﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phinite
{
	/// <summary>
	/// This class stores all information about a regular expression and is responsible
	/// for transforming plain text into a syntactic structure that represents a regular expression.
	/// </summary>
	public class RegularExpression
	{
		/// <summary>
		/// List of symbols that have special meaning.
		/// </summary>
		public static readonly KeyValuePair<string, InputSymbolTag>[] ReservedSymbols
			= new KeyValuePair<string, InputSymbolTag>[]
			{
				new KeyValuePair<string, InputSymbolTag>("+", InputSymbolTag.Union),
				new KeyValuePair<string, InputSymbolTag>("^*", InputSymbolTag.KleeneStar),
				new KeyValuePair<string, InputSymbolTag>("^+", InputSymbolTag.KleenePlus),
				new KeyValuePair<string, InputSymbolTag>(".", InputSymbolTag.EmptyWord),
				new KeyValuePair<string, InputSymbolTag>("(", InputSymbolTag.OpeningParenthesis),
				new KeyValuePair<string, InputSymbolTag>(")", InputSymbolTag.ClosingParenthesis)
			};
		/// <summary>
		/// Maximum lenght of any of ReservedSymbols.
		/// </summary>
		public static readonly int ReservedSymbolMaxLength = 2;

		/// <summary>
		/// Literals that represent each of the tags.
		/// 
		/// Conceptually, it is an exact reverse of ReservedSymbols list.
		/// </summary>
		public static readonly Dictionary<InputSymbolTag, string> TagsStrings
			= new Dictionary<InputSymbolTag, string>
			{
				{InputSymbolTag.Union, "+"},
				{InputSymbolTag.KleeneStar, "^*"},
				{InputSymbolTag.KleenePlus, "^+"},
				{InputSymbolTag.EmptyWord, "."},
				{InputSymbolTag.OpeningParenthesis, "("},
				{InputSymbolTag.ClosingParenthesis, ")"}
			};

		/// <summary>
		/// Those symbols are skipped when they are encountered in the input.
		/// </summary>
		public static readonly string[] IgnoredSymbols = new string[] { " ", "\t" };
		public static readonly int IgnoredSymbolMaxLength = 1;

		/// <summary>
		/// Those symbols cannot be used alone in the input. Sometimes they may be still allowed as parts
		/// of longer, multi-character symbols.
		/// </summary>
		public static readonly string[] ForbiddenSymbols = new string[] { "*" };
		public static readonly int ForbiddenSymbolMaxLength = 1;

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
		/// This variable is a result of parsing process.
		/// </summary>
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
				for (int n = 0; n < ForbiddenSymbols.Length; ++n)
					if (input.IndexOf(ForbiddenSymbols[n], i, Math.Min(ForbiddenSymbolMaxLength, input.Length - i)) == i)
						throw new ArgumentException(String.Format("error at character {0} of input: "
							+ "use of the symbol \"{1}\" is forbidden here", i, ForbiddenSymbols[n]));

				bool tagged = false;
				for (int n = 0; n < IgnoredSymbols.Length; ++n)
					if (input.IndexOf(IgnoredSymbols[n], i, Math.Min(IgnoredSymbolMaxLength, input.Length - i)) == i)
					{
						tagged = true;
						break;
					}

				if (tagged)
					continue;

				for (int n = 0; n < ReservedSymbols.Length; ++n)
					if (input.IndexOf(ReservedSymbols[n].Key, i, Math.Min(ReservedSymbolMaxLength, input.Length - i)) == i)
					{
						var current = ReservedSymbols[n].Value;
						if (taggedInput.Count > 0)
						{
							var previous = taggedInput[taggedInput.Count - 1].Value;

							if ((
									previous.Equals(InputSymbolTag.Union)
									|| previous.Equals(InputSymbolTag.OpeningParenthesis)
									) && (
									current.Equals(InputSymbolTag.Union)
									|| current.Equals(InputSymbolTag.KleeneStar)
									|| current.Equals(InputSymbolTag.KleenePlus)
								))
								throw new ArgumentException(String.Format("error at character {0} of input: "
									+ "union, kleene star and kleene plus symbol cannot occur after opening parenthis or union symbol", i));

							if ((
									previous.Equals(InputSymbolTag.KleeneStar)
									|| previous.Equals(InputSymbolTag.KleenePlus)
									) && (
									current.Equals(InputSymbolTag.KleeneStar)
									|| current.Equals(InputSymbolTag.KleenePlus)
								))
								throw new ArgumentException(String.Format("error at character {0} of input: "
									+ "kleene star and kleene plus symbols cannot be stacked", i));

							if (previous.Equals(InputSymbolTag.OpeningParenthesis)
								&& current.Equals(InputSymbolTag.ClosingParenthesis))
								throw new ArgumentException(String.Format("error at character {0} of input: "
									+ "it is illegal to use an empty pair of perentheses", i));

							if (taggedInput.Count > 1)
							{
								var previous2 = taggedInput[taggedInput.Count - 2].Value;
								if ((
										previous2.Equals(InputSymbolTag.OpeningParenthesis)
										&& previous.Equals(InputSymbolTag.ClosingParenthesis)
										) && (
										current.Equals(InputSymbolTag.Union)
										|| current.Equals(InputSymbolTag.KleeneStar)
										|| current.Equals(InputSymbolTag.KleenePlus)
									))
									throw new ArgumentException(String.Format("error at character {0} of input: "
										+ "union, kleene star and kleene plus symbol cannot be applied to empty pair of parenthes", i));
							}
						}
						else
						{
							if (
									current.Equals(InputSymbolTag.Union)
									|| current.Equals(InputSymbolTag.KleeneStar)
									|| current.Equals(InputSymbolTag.KleenePlus)
									|| current.Equals(InputSymbolTag.ClosingParenthesis)
								)
								throw new ArgumentException(String.Format("error at character {0} of input: "
									+ "the expression can start only with letter or opening parenthesis", i));
						}

						taggedInput.Add(ReservedSymbols[n]);
						if (ReservedSymbols[n].Key.Length > 1)
							i += ReservedSymbols[n].Key.Length - 1;
						tagged = true;
						break;
					}
				if (tagged)
					continue;
				var letter = input[i].ToString();
				if (!alphabet.Contains(letter))
					alphabet.Add(letter);
				taggedInput.Add(new KeyValuePair<string, InputSymbolTag>(letter, InputSymbolTag.Letter));
			}

			if (taggedInput.Count == 0)
				throw new ArgumentException("input contains only whitespace characters");

			if (taggedInput[0].Value == InputSymbolTag.Union)
				throw new ArgumentException("regular expression cannot start with union operator");

			if ((taggedInput[0].Value & InputSymbolTag.UnaryOperator) > 0
					|| taggedInput[0].Value == InputSymbolTag.ClosingParenthesis)
				throw new ArgumentException("regular expression cannot start with unary operator or a closing parenthesis");

			if (taggedInput.Count > 1 && (
					taggedInput[taggedInput.Count - 1].Value == InputSymbolTag.Union
					|| taggedInput[taggedInput.Count - 1].Value == InputSymbolTag.OpeningParenthesis
				))
				throw new ArgumentException("regular expression cannot end with union operator or an opening parenthesis");
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
				throw new ArgumentException(String.Format("parentheses count in the expression does not match,"
					+ " there are {0} opening, but {1} closing parentheses", openCount, closeCount));
		}

		private PartialExpression ParseSubExpression(int startingIndex, out int lastIndex)
		{
			var returned = new PartialExpression(PartialExpressionRole.Undetermined, null);
			lastIndex = startingIndex;

			PartialExpression current = returned;
			for (int i = startingIndex; i < taggedInput.Count; ++i)
			{
				var input = taggedInput[i];

				if (input.Value.Equals(InputSymbolTag.Letter))
				{
					current.Role = PartialExpressionRole.Concatenation;
					current.AddToConcatenation(new PartialExpression(current, input.Key));
				}
				else if (input.Value.Equals(InputSymbolTag.EmptyWord))
				{
					current.Role = PartialExpressionRole.Concatenation;
					current.AddToConcatenation(new PartialExpression(PartialExpressionRole.EmptyWord, current));
				}
				else if (input.Value.Equals(InputSymbolTag.Union))
				{
					if (current.Root == null)
					{
						var newCurrent = new PartialExpression(PartialExpressionRole.Undetermined, null);
						var newRoot = new PartialExpression(null, new List<PartialExpression> { current, newCurrent }, false);

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
				}
				else if (input.Value.Equals(InputSymbolTag.OpeningParenthesis))
				{
					int last = 0;
					var subExpr = ParseSubExpression(i + 1, out last);
					current.Role = PartialExpressionRole.Concatenation;
					current.AddToConcatenation(subExpr);
					subExpr.Root = current;
					i = last;
				}
				else if (input.Value.Equals(InputSymbolTag.ClosingParenthesis))
				{
					lastIndex = i;
					return returned;
				}
				else if ((input.Value & InputSymbolTag.UnaryOperator) > 0)
				{
					current.Parts[current.Parts.Count - 1].Operator = (UnaryOperator)input.Value;
					//current.LastConcatenatedSymbol.Operator = (UnaryOperator)input.Value;
				}
				else
				{
					throw new NotImplementedException("more handling code needed");
				}

				lastIndex = i;
			}
			return returned;
		}

		private void ParseInput()
		{
			int lastSymbol;
			parsedInput = ParseSubExpression(0, out lastSymbol);
			if (lastSymbol < taggedInput.Count - 1)
				throw new ArgumentException("parsing is incomplete");
		}

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
				throw new ArgumentNullException("cannot remove non-existing letter", "removedLetter");
			if (removedLetter.Length != 1)
				throw new ArgumentException("a single letter must be given", "removedLetter");
			if (!alphabet.Contains(removedLetter))
				throw new ArgumentException("this letter does not belong to the alphabet", "removedLetter");

			if (taggedInput == null || tagCount == null || parsedInput == null)
				EvaluateInput();

			var copy = new RegularExpression(this.input, true);

			copy.parsedInput.Derive(removedLetter);
			if (copy.parsedInput.Role.Equals(PartialExpressionRole.Invalid))
				return null;
			copy.input = copy.ToString();
			copy.EvaluateInput();

			return copy;
		}

		/// <summary>
		/// Returns true if this expression generates an empty word, and false otherwise.
		/// </summary>
		/// <returns>true if this expression generates an empty word, false otherwise</returns>
		public bool GeneratesEmptyWord()
		{
			return parsedInput.GeneratesEmptyWord();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (obj is RegularExpression == false)
				return false;

			var regexp = (RegularExpression)obj;

			if (input.Equals(regexp.input))
				return true;

			if (parsedInput == null || regexp.parsedInput == null)
				throw new ArgumentException("cannot compare regular expressions that were not evaluated");

			return parsedInput.Equals(regexp.parsedInput);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// Returns a string representation of this regular expression.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return parsedInput.ToString();
		}

	}
}