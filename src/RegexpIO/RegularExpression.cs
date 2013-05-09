using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Phinite
{
	/// <summary>
	/// This class stores all information about a regular expression and is responsible
	/// for transforming plain text into a syntactic structure that represents a regular expression.
	/// </summary>
	public class RegularExpression
	{

		private static readonly string errorAtChar = "error at character {0} of input, just after \"{1}\": ";
		private static readonly string errorAtStart = "error at beginning of the input: ";

		/// <summary>
		/// A set of rules that apply to text input for RegularExpression constructor.
		/// </summary>
		public static readonly string[] Rules
			= new string[]
			{
				"union, kleene star and kleene plus symbol cannot occur after opening parenthis or union symbol",
				"kleene star and kleene plus symbols cannot be stacked",
				"it is illegal to use an empty pair of perentheses",
				"union, kleene star and kleene plus symbol cannot be applied to empty pair of parenthes",
				"the expression can start only with letter or opening parenthesis",
				"input must not contain only whitespace characters",
				"regular expression cannot start with union operator",
				"regular expression cannot start with unary operator or a closing parenthesis",
				"regular expression cannot end with union operator or an opening parenthesis"
			};

		/// <summary>
		/// List of symbols that have special meaning.
		/// </summary>
		public static readonly KeyValuePair<string, InputSymbolTag>[] ReservedSymbols;

		/// <summary>
		/// Maximum lenght of any of ReservedSymbols.
		/// </summary>
		public static readonly int ReservedSymbolMaxLength;

		/// <summary>
		/// Literals that represent each of the tags.
		/// 
		/// Conceptually, it is an exact reverse of ReservedSymbols list.
		/// </summary>
		public static readonly Dictionary<InputSymbolTag, string> TagsStrings;

		/// <summary>
		/// Those symbols are skipped when they are encountered in the input.
		/// </summary>
		public static readonly string[] IgnoredSymbols;

		/// <summary>
		/// Maximum lenght of any of IgnoredSymbols.
		/// </summary>
		public static readonly int IgnoredSymbolMaxLength;

		/// <summary>
		/// Those symbols cannot be used alone in the input. Sometimes they may be still allowed as parts
		/// of special symbols - possibly longer, multi-character ones.
		/// </summary>
		public static readonly string[] ForbiddenSymbols;

		/// <summary>
		/// Maximum lenght of any of ForbiddenSymbols.
		/// </summary>
		public static readonly int ForbiddenSymbolMaxLength;

		static RegularExpression()
		{
			ReservedSymbols = new KeyValuePair<string, InputSymbolTag>[]
				{
					new KeyValuePair<string, InputSymbolTag>("+", InputSymbolTag.Union),
					new KeyValuePair<string, InputSymbolTag>("^*", InputSymbolTag.KleeneStar),
					new KeyValuePair<string, InputSymbolTag>("^+", InputSymbolTag.KleenePlus),
					new KeyValuePair<string, InputSymbolTag>(".", InputSymbolTag.EmptyWord),
					new KeyValuePair<string, InputSymbolTag>("(", InputSymbolTag.OpeningParenthesis),
					new KeyValuePair<string, InputSymbolTag>(")", InputSymbolTag.ClosingParenthesis)
				};
			ReservedSymbolMaxLength = 2;

			var reversedReservedSymbols = new Dictionary<InputSymbolTag, string>();
			foreach (var pair in ReservedSymbols)
			{
				if (reversedReservedSymbols.ContainsKey(pair.Value))
					continue;
				reversedReservedSymbols.Add(pair.Value, pair.Key);
			}
			TagsStrings = reversedReservedSymbols;

			IgnoredSymbols = new string[] { " ", "\t", "\n" };
			IgnoredSymbolMaxLength = 1;

			ForbiddenSymbols = new string[] { "^", "*", "+", ".", "(", ")", "ε" };
			ForbiddenSymbolMaxLength = 1;
		}

		/// <summary>
		/// Original input given to the constructor of this object.
		/// 
		/// May sometimes differ from the result of ToString() method, but will never differ
		/// semantically and in meaning.
		/// </summary>
		public string Input { get { return input; } }
		private string input;

		/// <summary>
		/// Set of non-special characters that were encountered in the input.
		/// </summary>
		public ReadOnlyCollection<string> Alphabet { get { return new ReadOnlyCollection<string>(alphabet); } }
		private List<string> alphabet;

		/// <summary>
		/// List of input characters with tags that denote their meaning.
		/// 
		/// This variable is only used in validation and parsing process.
		/// </summary>
		private List<KeyValuePair<string, InputSymbolTag>> taggedInput;

		/// <summary>
		/// A dictionary that stores how many times each tag was encountered in the input.
		/// 
		/// This variable is only used in validation process.
		/// </summary>
		private Dictionary<InputSymbolTag, uint> tagCount;

		/// <summary>
		/// A tree structure that has semantic structure (and meaning) equivalent to that of a given input.
		/// 
		/// This variable is a copy of the result of parsing process.
		/// </summary>
		public PartialExpression ParseTree { get { return new PartialExpression(parsedInput); } }
		private PartialExpression parsedInput;

		/// <summary>
		/// Creates a regular expression from a given
		/// </summary>
		/// <param name="input"></param>
		/// <param name="evaluateImmediately"></param>
		public RegularExpression(string input, bool evaluateImmediately = false)
		{
			if (input == null)
				throw new ArgumentNullException("input", "input is null");
			if (input.Length == 0)
				throw new ArgumentException("expression is empty");

			this.input = input;

			if (evaluateImmediately)
				EvaluateInput();
		}

		private RegularExpression(RegularExpression origin)
		{
			input = origin.input;
			alphabet = new List<string>(origin.alphabet);
			taggedInput = new List<KeyValuePair<string, InputSymbolTag>>(origin.taggedInput);
			tagCount = new Dictionary<InputSymbolTag, uint>(origin.tagCount);
			parsedInput = new PartialExpression(origin.parsedInput);
		}

		private RegularExpression(PartialExpression part)
		{
			input = part.ToString();
			parsedInput = part;
			TagInput();
			CountTags();
		}

		/// <summary>
		/// Evaluates the input string again. Can be used multiple times, but the result will always be
		/// the same.
		/// </summary>
		public void EvaluateInput()
		{
			TagInput();
			CountTags();
			ParseInput();
			Optimize();
		}

		private void TagInput()
		{
			alphabet = new List<string>();
			taggedInput = new List<KeyValuePair<string, InputSymbolTag>>();
			for (int i = 0; i < input.Length; ++i)
			{
				bool tagged = false;
				for (int n = 0; n < IgnoredSymbols.Length; ++n)
					if (input.IndexOf(IgnoredSymbols[n], i, Math.Min(IgnoredSymbolMaxLength, input.Length - i)) == i)
					{
						tagged = true; // in this case it means: "not to be tagged"
						break;
					}

				if (tagged)
					continue;

				int iNew = CheckReservedSymbolsAt(i);
				if (iNew >= 0)
				{
					tagged = true;
					i = iNew;
				}
				if (tagged)
					continue;

				CheckForbiddenSymbolsAt(i);

				var letter = input[i].ToString();
				if (!alphabet.Contains(letter))
					alphabet.Add(letter);
				taggedInput.Add(new KeyValuePair<string, InputSymbolTag>(letter, InputSymbolTag.Letter));
			}

			if (taggedInput.Count == 0)
				throw new ArgumentException(Rules[5]);

			if (taggedInput[0].Value == InputSymbolTag.Union)
				throw new ArgumentException(Rules[6]);

			if ((taggedInput[0].Value & InputSymbolTag.UnaryOperator) > 0
					|| taggedInput[0].Value == InputSymbolTag.ClosingParenthesis)
				throw new ArgumentException(Rules[7]);

			if (taggedInput.Count > 1 && (
					taggedInput[taggedInput.Count - 1].Value == InputSymbolTag.Union
					|| taggedInput[taggedInput.Count - 1].Value == InputSymbolTag.OpeningParenthesis
				))
				throw new ArgumentException(Rules[8]);
		}

		private int CheckReservedSymbolsAt(int i)
		{
			int count = taggedInput.Count;
			bool success = false;

			InputSymbolTag previous = default(InputSymbolTag);
			if (count > 0)
				previous = taggedInput[count - 1].Value;
			InputSymbolTag previous2 = default(InputSymbolTag);
			if (count > 1)
				previous2 = taggedInput[count - 2].Value;

			// at most one of parallel threads reaches the last lines of action body
			try
			{
				Parallel.For(0, ReservedSymbols.Length, (int n) =>
					{
						if (input.IndexOf(ReservedSymbols[n].Key, i, Math.Min(ReservedSymbolMaxLength, input.Length - i)) != i)
							return;

						var current = ReservedSymbols[n].Value;
						if (count > 0)
						{
							CheckIfTagSequenceValid(i, previous, current);

							if (count > 1)
								CheckIfTagSequenceValid(i, previous2, previous, current);
						}
						else if (current == InputSymbolTag.Union || current == InputSymbolTag.KleeneStar
								|| current == InputSymbolTag.KleenePlus || current == InputSymbolTag.ClosingParenthesis)
							throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
								errorAtStart + Rules[4]));

						taggedInput.Add(ReservedSymbols[n]);
						if (ReservedSymbols[n].Key.Length > 1)
							i += ReservedSymbols[n].Key.Length - 1;
						success = true;
					});
			}
			catch (AggregateException e)
			{
				int exceptionsCount = e.InnerExceptions.Count;
				if (exceptionsCount > 1)
					throw new ArgumentException(String.Format("there are {0} errors, including an {1}",
						exceptionsCount, e.InnerException.Message));
				throw e.InnerException;
			}

			if (success)
				return i;
			return -1;
		}

		private string GetTagsSample()
		{
			return String.Join("", taggedInput
				.GetRange(Math.Max(taggedInput.Count - 5, 0), Math.Min(5, taggedInput.Count))
				.Select((pair, i) => pair.Key)
				);
		}

		private void CheckIfTagSequenceValid(int index, params InputSymbolTag[] tags)
		{
			switch (tags.Length)
			{
				case 2:
					{
						InputSymbolTag previous = tags[0];
						InputSymbolTag current = tags[1];

						if ((previous == InputSymbolTag.Union || previous == InputSymbolTag.OpeningParenthesis)
							&& (
								current == InputSymbolTag.Union
								|| current == InputSymbolTag.KleeneStar
								|| current == InputSymbolTag.KleenePlus
							))
							throw new ArgumentException(index == 0 ? errorAtStart + Rules[0] : String.Format(
								errorAtChar + Rules[0], index, GetTagsSample()));

						if ((previous == InputSymbolTag.KleeneStar || previous == InputSymbolTag.KleenePlus)
							&& (current == InputSymbolTag.KleeneStar || current == InputSymbolTag.KleenePlus))
							throw new ArgumentException(index == 0 ? errorAtStart + Rules[1] : String.Format(
								errorAtChar + Rules[1], index, GetTagsSample()));

						if (previous == InputSymbolTag.OpeningParenthesis
							&& current == InputSymbolTag.ClosingParenthesis)
							throw new ArgumentException(index == 0 ? errorAtStart + Rules[2] : String.Format(
								errorAtChar + Rules[2], index, GetTagsSample()));
					} break;
				case 3:
					{
						InputSymbolTag previous2 = tags[0];
						InputSymbolTag previous = tags[1];
						InputSymbolTag current = tags[2];

						if ((previous2 == InputSymbolTag.OpeningParenthesis && previous == InputSymbolTag.ClosingParenthesis)
							&& (
								current == InputSymbolTag.Union
								|| current == InputSymbolTag.KleeneStar
								|| current == InputSymbolTag.KleenePlus
							))
							throw new ArgumentException(index == 0 ? errorAtStart + Rules[3] : String.Format(
								errorAtChar + Rules[3], index));
					} break;
			}
		}

		private void CheckForbiddenSymbolsAt(int i)
		{
			try
			{
				Parallel.For(0, ForbiddenSymbols.Length, (int n) =>
					{
						if (input.IndexOf(ForbiddenSymbols[n], i, Math.Min(ForbiddenSymbolMaxLength, input.Length - i)) != i)
							return;

						if (i == 0)
							throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
								errorAtStart + "use of the symbol \"{0}\" is forbidden here", ForbiddenSymbols[n]));

						throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
							errorAtChar + "use of the symbol \"{2}\" is forbidden here", i, GetTagsSample(), ForbiddenSymbols[n]));
					});
			}
			catch (AggregateException e)
			{
				int exceptionsCount = e.InnerExceptions.Count;
				if (exceptionsCount > 1)
					throw new ArgumentException(String.Format("there are {0} errors, including an {1}",
						exceptionsCount, e.InnerException.Message));
				throw e.InnerException;
			}
		}

		private void CountTags()
		{
			tagCount = new Dictionary<InputSymbolTag, uint>();
			for (int i = 0; i < taggedInput.Count; ++i)
			{
				uint count = 0;
				if (tagCount.TryGetValue(taggedInput[i].Value, out count))
					tagCount[taggedInput[i].Value] = count + 1;
				else
					tagCount.Add(taggedInput[i].Value, 1);
			}

			uint openCount = 0, closeCount = 0;
			tagCount.TryGetValue(InputSymbolTag.OpeningParenthesis, out openCount);
			tagCount.TryGetValue(InputSymbolTag.ClosingParenthesis, out closeCount);
			if (openCount != closeCount)
				throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
					"parentheses count in the expression does not match,"
					+ " there are {0} opening, but {1} closing parentheses", openCount, closeCount));
		}

		private void ParseInput()
		{
			int lastSymbol;
			parsedInput = ParseSubExpression(0, out lastSymbol);
			if (lastSymbol < taggedInput.Count - 1)
				throw new ArgumentException("parsing is incomplete");
		}

		private PartialExpression ParseSubExpression(int startingIndex, out int lastIndex)
		{
			var returned = new PartialExpression(PartialExpressionRole.Undetermined, null);
			lastIndex = startingIndex;

			PartialExpression current = returned;
			for (int i = startingIndex; i < taggedInput.Count; ++i)
			{
				var tagPair = taggedInput[i];

				switch (tagPair.Value)
				{
					case InputSymbolTag.Letter:
						{
							current.Role = PartialExpressionRole.Concatenation;
							current.AddToConcatenation(new PartialExpression(current, tagPair.Key));
						} break;
					case InputSymbolTag.EmptyWord:
						{
							current.Role = PartialExpressionRole.Concatenation;
							current.AddToConcatenation(new PartialExpression(PartialExpressionRole.EmptyWord, current));
						} break;
					case InputSymbolTag.Union:
						{
							if (current.Root == null)
							{
								var newCurrent = new PartialExpression(PartialExpressionRole.Undetermined, null);
								//var newRoot = new PartialExpression(null, new List<PartialExpression> { current, newCurrent }, false);
								var newRoot = new PartialExpression(PartialExpressionRole.Union, null);
								newRoot.AddToUnion(current);
								newRoot.AddToUnion(newCurrent);

								current.Root = newRoot;
								newCurrent.Root = newRoot;

								if (returned == current)
									returned = newRoot;
								current = newCurrent;
							}
							else if (current.Root.Role.Equals(PartialExpressionRole.Union)
								&& current.Root.Parts.Contains(current))
							{
								var newCurrent = new PartialExpression(PartialExpressionRole.Undetermined, current.Root);
								current.Root.AddToUnion(newCurrent);
								current = newCurrent;
							}
							else
								throw new NotImplementedException("more handling code needed");
						} break;
					case InputSymbolTag.OpeningParenthesis:
						{
							int last = 0;
							var subExpr = ParseSubExpression(i + 1, out last);
							current.Role = PartialExpressionRole.Concatenation;
							current.AddToConcatenation(subExpr);
							subExpr.Root = current;
							i = last;
						} break;
					case InputSymbolTag.ClosingParenthesis:
						{
							lastIndex = i;
							return returned;
						}
					case InputSymbolTag.KleeneStar:
					case InputSymbolTag.KleenePlus:
						{
							current.Parts[current.Parts.Count - 1].Operator = (UnaryOperator)tagPair.Value;
							//current.LastConcatenatedSymbol.Operator = (UnaryOperator)input.Value;
						} break;
					default:
						{
							throw new NotImplementedException("more handling code needed");
						}
				}

				lastIndex = i;
			}
			return returned;
		}

		/// <summary>
		/// Removes useless nodes from the parse tree.
		/// </summary>
		public void Optimize()
		{
			//string test = ToString();
			parsedInput.Optimize();
			//if (!ToString().Equals(test))
			//	throw new NotImplementedException("optimizations are not implemented properly");
		}

		/// <summary>
		/// Derives a new expression by removing a given letter from the beginning
		/// of this regular expression.
		/// </summary>
		/// <param name="removedLetter">a one-letter string, it must belong to the alphabet of the input</param>
		/// <returns>a derived expression, or null if no valid expression could be derived</returns>
		public RegularExpression Derive(string removedLetter)
		{
			if (removedLetter == null)
				throw new ArgumentNullException("removedLetter", "cannot remove non-existing letter");
			if (removedLetter.Length != 1)
				throw new ArgumentException("a single letter must be given", "removedLetter");

			if (!alphabet.Contains(removedLetter))
				//throw new ArgumentException("this letter does not belong to the alphabet", "removedLetter");
				return null; // this can be simply handled, no need for exception

			//if (taggedInput == null || tagCount == null || parsedInput == null)
			//	throw new ArgumentNullException("parsedInput"); //EvaluateInput();

			//var copy = new RegularExpression(this.input, true);
			//copy.parsedInput.Derive(removedLetter);
			//if (copy.parsedInput.Role.Equals(PartialExpressionRole.Invalid))
			//	return null;
			//copy.input = copy.ToString();
			//copy.EvaluateInput();
			//return copy;

			var parseTreeCopy = new PartialExpression(parsedInput);
			parseTreeCopy.Derive(removedLetter);
			if (parseTreeCopy.Role.Equals(PartialExpressionRole.Invalid))
				return null;

			return new RegularExpression(parseTreeCopy);
		}

		/// <summary>
		/// Returns true if this expression generates an empty word, and false otherwise.
		/// </summary>
		/// <returns>true if this expression generates an empty word, false otherwise</returns>
		public bool GeneratesEmptyWord()
		{
			return parsedInput.GeneratesEmptyWord();
		}

		/****
		/// <summary>
		/// Checks if given expression is equivalent to this one.
		/// </summary>
		/// <param name="regexp"></param>
		/// <returns></returns>
		public bool IsEquivalent(RegularExpression regexp)
		{
			if (Equals(regexp))
				return true;

			//RegularExpression copy = new RegularExpression(this);
			//RegularExpression regexpCopy = new RegularExpression(regexp);

			List<string> commonAlphabet = new List<string>(this.alphabet);
			foreach (string letter in regexp.alphabet)
			{
				if (commonAlphabet.Any(x => x.Equals(letter)))
					continue;
				commonAlphabet.Add(letter);
			}

			List<RegularExpression> thisDerivations = new List<RegularExpression>();
			List<RegularExpression> regexpDerivations = new List<RegularExpression>();
			foreach (string letter in commonAlphabet)
			{
				var r1 = this.Derive(letter);
				var r2 = regexp.Derive(letter);

				thisDerivations.Add(r1);
				regexpDerivations.Add(r2);

				if (r1 == null && r2 == null)
					continue;
				if ((r1 == null && r2 != null) || (r1 != null && r2 == null))
					return false;
				if (this.Equals(r1) && regexp.Equals(r2))
					continue;
				//if (!r1.IsEquivalent(r2))
				return false;
			}

			return true;
		}
		 ****/

		/// <summary>
		/// Estimates similarity between two regular expressions, on a scale from 0 to 1.
		/// </summary>
		/// <param name="regexp"></param>
		/// <returns>value from 0 to 1</returns>
		public double Similarity(RegularExpression regexp)
		{
			if (regexp == null)
				return 0.0;

			if (Equals(regexp))
				return 1.0;

			if (alphabet.Count != regexp.alphabet.Count)
				return 0.0; // alphabet lengths differ

			if (GeneratesEmptyWord() != regexp.GeneratesEmptyWord())
				return 0.0; // empty word generation properties differ

			if (alphabet.Intersect(regexp.alphabet).Count() != alphabet.Count)
				return 0.0; // alphabet contents differ

			RegularExpression[] derived = new RegularExpression[2 * alphabet.Count];
			Parallel.For(0, 2 * alphabet.Count, (int n) =>
				{
					if(n < alphabet.Count)
						derived[n] = Derive(alphabet[n / 2]);
					else
						derived[n] = regexp.Derive(alphabet[n / 2]);
				});

			double[] similarities = new double[alphabet.Count];
			Parallel.For(0, alphabet.Count, (int n) =>
				{
					int n2 = n + alphabet.Count;

					similarities[n] = 0.5;

					if (ReferenceEquals(derived[n], null) && ReferenceEquals(derived[n2], null))
						return;

					similarities[n] = 0.0;

					if ((derived[n] == null && derived[n2] != null) || (derived[n] != null && derived[n2] == null))
						return; // outgoing transitions differ

					if (derived[n].alphabet.Count != derived[n2].alphabet.Count)
						return; // alphabet lengths differ

					if (derived[n].GeneratesEmptyWord() != derived[n2].GeneratesEmptyWord())
						return; // empty word generation properties differ

					similarities[n] = 0.5;
				});

			return similarities.Min();
		}

		/// <summary>
		/// Checks for equality between this RegularExpression and another object.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (obj is RegularExpression == false)
				return false;

			if (this == obj)
				return true;

			var regexp = (RegularExpression)obj;

			if (input.Equals(regexp.input))
				return true;

			if (alphabet.Count != regexp.alphabet.Count)
				return false;

			if (alphabet.Count != alphabet.Intersect(regexp.alphabet).Count())
				return false;

			return parsedInput.Equals(regexp.parsedInput);
		}

		/// <summary>
		/// Returns the hash code for the value of this instance.
		/// </summary>
		/// <returns>a 32-bit signed integer hash code</returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// Returns a string representation of parse tree of this regular expression.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return parsedInput.ToString();
		}

	}
}
