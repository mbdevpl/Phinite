using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phinite
{
	/// <summary>
	/// Part of the expression that serves as a single node of a parse tree of a complete RegularExpression.
	/// </summary>
	public class PartialExpression
	{
		/// <summary>
		/// Role of the expression part, it has large impact on its behaviour.
		/// </summary>
		public PartialExpressionRole Role
		{
			get { return role; }
			set
			{
				if (role.Equals(value))
					return;
				if (!role.Equals(PartialExpressionRole.Undetermined))
					throw new ArgumentException("cannot change the role of partial expression if it is already determined");
				role = value;
			}
		}
		private PartialExpressionRole role;

		/// <summary>
		/// Root of this part of the expression.
		/// </summary>
		public PartialExpression Root
		{
			get { return root; }
			set
			{
				if (root == value)
					return;
				if (root != null)
					throw new ArgumentException("cannot change root of expression's part if it is already set");
				root = value;
			}
		}
		private PartialExpression root;

		/// <summary>
		/// Contains list of sub-parts of this partial expression, if it has any.
		/// 
		/// Applicable only to expressions that have role equal to PartialExpressionRole.Concatenation
		/// or PartialExpressionRole.Union. Always null in other cases.
		/// </summary>
		public ReadOnlyCollection<PartialExpression> Parts
		{
			get
			{
				if ((role & PartialExpressionRole.InternalNode) == 0)
					return null;
				if (parts == null)
					return null;
				return new ReadOnlyCollection<PartialExpression>(parts);
			}
		}
		private List<PartialExpression> parts;

		/// <summary>
		/// Value of this expression, provided that it is a single character.
		/// 
		/// Applicable only to expressions that have role equal to PartialExpressionRole.Letter.
		/// Null in all other cases.
		/// </summary>
		public string Value
		{
			get { return _value; }
			set
			{
				if (role != PartialExpressionRole.Letter)
					throw new ArgumentException("only letter can have an actual value");
				if (_value != null)
					throw new ArgumentException("cannot change value of expression's part if it is already set");
				_value = value;
			}
		}
		private string _value;

		/// <summary>
		/// Unary operator associated with this part.
		/// </summary>
		public UnaryOperator Operator
		{
			get { return _operator; }
			set
			{
				if (_operator != UnaryOperator.None)
					throw new ArgumentException("cannot change operator of expression's part if it is already set");
				_operator = value;
			}
		}
		private UnaryOperator _operator;

		/// <summary>
		/// Copy constructor. Constructs a complete copy of all subexpressions.
		/// </summary>
		/// <param name="origin">original partial expression</param>
		public PartialExpression(PartialExpression origin)
			: this(origin.role, origin.root)
		{
			_value = origin._value;
			_operator = origin._operator;

			if (role.Equals(PartialExpressionRole.Union)
				|| role.Equals(PartialExpressionRole.Concatenation))
			{
				parts = new List<PartialExpression>();
				foreach (var part in origin.parts)
				{
					parts.Add(new PartialExpression(part));
					part.root = this;
				}
			}
		}

		/// <summary>
		/// Constructs a new regular expression part.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="root"></param>
		public PartialExpression(PartialExpressionRole role, PartialExpression root)
		{
			//this.role = PartialExpressionRole.Undetermined;
			this.role = role;
			this.root = root;

			this.parts = null;
			this._value = null;
			this._operator = UnaryOperator.None;
		}

		/// <summary>
		/// Constructs a new regular expression part.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="parts"></param>
		/// <param name="concatenation"></param>
		private PartialExpression(PartialExpression root, List<PartialExpression> parts, bool concatenation)
			: this(concatenation ? PartialExpressionRole.Concatenation : PartialExpressionRole.Union, root)
		{
			this.parts = parts;
		}

		/// <summary>
		/// Constructs a new regular expression part.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="value"></param>
		public PartialExpression(PartialExpression root, string value)
			: this(PartialExpressionRole.Letter, root)
		{
			this._value = value;
		}

		//public PartialExpression(PartialExpression root)
		//	: this()
		//{
		//	this.root = root;
		//}

		//public PartialExpression(PartialExpression root, string value)
		//	: this(root)
		//{
		//	this._value = value;
		//}

		/// <summary>
		/// Adds a new part to list of sub-parts that are in alternative with each other.
		/// </summary>
		/// <param name="exp"></param>
		public void AddToUnion(PartialExpression exp)
		{
			if (role != PartialExpressionRole.Union)
				throw new ArgumentException("cannot add symbol to a partial expression that does not store union");
			if (parts == null)
				parts = new List<PartialExpression>();
			parts.Add(exp);
		}

		/// <summary>
		/// Adds a new part to list of sub-parts that are concatenated with each other.
		/// </summary>
		/// <param name="exp"></param>
		public void AddToConcatenation(PartialExpression exp)
		{
			if (role != PartialExpressionRole.Concatenation)
				throw new ArgumentException("cannot concatenate symbol with a partial expression that does not store concatenation");
			if (parts == null)
				parts = new List<PartialExpression>();
			parts.Add(exp);
		}

		/// <summary>
		/// Performs several optimizations of this parse tree. All of them are described in technical analysis.
		/// </summary>
		public void Optimize()
		{
			if ((role == PartialExpressionRole.Concatenation
				|| role == PartialExpressionRole.Union)
				&& parts.Count == 1)
			{
				// this is a concatenation or union of one element, meaning that
				// in fact it is not a union or a concatenation, but rather
				// just this single element

				#region Optimization 1.
				var part = parts[0];

				if (part.role == PartialExpressionRole.Concatenation
					|| part.role == PartialExpressionRole.Union)
				{
					foreach (var p in part.parts)
						p.root = this;
				}
				if (_operator == UnaryOperator.None)
					_operator = part._operator;
				else if (part._operator == UnaryOperator.None)
				{
					// nothing needed here, the operator stays as it was
				}
				else if (_operator == UnaryOperator.KleeneStar
					|| part._operator == UnaryOperator.KleeneStar)
					_operator = UnaryOperator.KleeneStar;
				else if (_operator == UnaryOperator.KleenePlus
					&& part._operator == UnaryOperator.KleenePlus)
					_operator = UnaryOperator.KleenePlus;
				else
					throw new NotImplementedException(
						"not implemented case in operator pair handling in partial expression optimization algorithm");

				_value = part._value;
				parts = part.parts;
				role = part.role;
				Optimize(); // optmize once again
				#endregion

				return;
			}

			switch (role)
			{
				case PartialExpressionRole.Concatenation:
					OptimizeConcatenation();
					break;
				case PartialExpressionRole.Union:
					OptimizeUnion();
					break;
			}
		}

		private void OptimizeConcatenation()
		{
			for (int i = parts.Count - 1; i >= 0; --i)
			{
				if (parts.Count == 1)
					break;
				var p = parts[i];

				#region Optimization 2.
				if (p.role == PartialExpressionRole.EmptyWord)
				{
					parts.RemoveAt(i);
					continue;
				}
				else
					p.Optimize();
				#endregion

				#region Optimization 6.
				if (_operator == UnaryOperator.None
					&& p.role == PartialExpressionRole.Concatenation && p._operator == UnaryOperator.None)
				{
					var subParts = p.parts;
					parts.RemoveAt(i);
					for (int si = subParts.Count - 1; si >= 0; --si)
					{
						subParts[si].root = this;
						parts.Insert(i, subParts[si]);
					}
					// optimize this partial expression once again due to changes in the structure
					Optimize();
					continue;
				}
				#endregion

				#region Optimization 4.
				if (i == parts.Count - 1)
					continue;
				var x = parts[i + 1];
				if (x._operator != UnaryOperator.None && p._operator != UnaryOperator.None
					&& x.ContentEquals(p))
				{
					if (x._operator == UnaryOperator.KleeneStar && p._operator == UnaryOperator.KleenePlus)
						x._operator = UnaryOperator.KleenePlus;
					parts.RemoveAt(i);
				}
				#endregion
			}

			if (parts.Count == 1)
				Optimize();
			else if (parts.Count == 0)
				throw new ArgumentException("zero parts reached while optimizing a concatenation");
		}

		private void OptimizeUnion()
		{
			#region Optimization 8.
			bool optimization8 = (_operator == UnaryOperator.KleeneStar);
			for (int i = parts.Count - 1; i >= 0; --i)
			{
				var p = parts[i];
				p.Optimize();
				if (optimization8 && p.role == PartialExpressionRole.EmptyWord)
				{
					// empty word will be generated by this expression anyway
					parts.RemoveAt(i);
				}
			}
			if (optimization8 && parts.Count == 1)
			{
				Optimize();
				return;
			}
			#endregion

			//foreach (var p in parts)
			//	p.Optimize();
			for (int i = parts.Count - 1; i >= 0; --i)
			{
				var p = parts[i];

				#region Optimization 3.
				if (parts.Any(x => (x != p && x.ContentEquals(p))))
				{
					// there are duplicates in this union
					parts.RemoveAt(i);
					continue;
				}
				#endregion

				#region Optimization 7.
				if (_operator == UnaryOperator.None
					&& p.role == PartialExpressionRole.Union && p._operator == UnaryOperator.None)
				{
					var subParts = p.parts;
					parts.RemoveAt(i);
					for (int si = subParts.Count - 1; si >= 0; --si)
					{
						subParts[si].root = this;
						parts.Insert(i, subParts[si]);
					}
					// optimize this partial expression once again due to changes in the structure
					Optimize();
					continue;
				}
				#endregion

				#region Optimization 5.
				var equal = parts.FindAll(x => (x != p && x.ContentEquals(p)));
				foreach (var x in equal)
				{
					if (x._operator == UnaryOperator.KleenePlus && p._operator == UnaryOperator.None)
						parts.RemoveAt(i);
					else if (x._operator == UnaryOperator.None && p._operator == UnaryOperator.KleenePlus)
					{
						x._operator = UnaryOperator.KleenePlus;
						parts.RemoveAt(i);
					}
					else if (x._operator == UnaryOperator.KleeneStar && p._operator == UnaryOperator.KleenePlus)
						parts.RemoveAt(i);
					else if (x._operator == UnaryOperator.KleenePlus && p._operator == UnaryOperator.KleeneStar)
					{
						x._operator = UnaryOperator.KleeneStar;
						parts.RemoveAt(i);
					}
					else
						continue;
					break;
				}
				#endregion
			}
			if (parts.Count == 1)
				Optimize();
			else if (parts.Count == 0)
				throw new ArgumentException("zero parts reached while optimizing a union");
		}

		/// <summary>
		/// The derivation is performed in-place, therefore it is good to make a backup of instance
		/// if the original is expected to be of some use later.
		/// </summary>
		/// <param name="removedLetter">string containing a single letter</param>
		public void Derive(string removedLetter)
		{
			// to properly derive expressions with unary operators, they have to be transformed
			switch (_operator)
			{
				case UnaryOperator.KleeneStar:
					{
						// (a)* becomes .+a(a)* because those expressions are equivalent

						var extracted = new PartialExpression(this);
						extracted._operator = UnaryOperator.None;

						var remaining = new PartialExpression(this);

						var concat = new PartialExpression(this, new List<PartialExpression> { extracted, remaining }, true);
						extracted.root = concat;
						remaining.root = concat;

						role = PartialExpressionRole.Union;
						parts = new List<PartialExpression> { new PartialExpression(PartialExpressionRole.EmptyWord, this), concat };
						_operator = UnaryOperator.None;
					} break;
				case UnaryOperator.KleenePlus:
					{
						// (a)^+ becomes a(a)^* because those expressions are equivalent

						var extracted = new PartialExpression(this);
						extracted._operator = UnaryOperator.None;
						extracted.root = this;

						var remaining = new PartialExpression(this);
						remaining._operator = UnaryOperator.KleeneStar;
						remaining.root = this;

						role = PartialExpressionRole.Concatenation;
						parts = new List<PartialExpression> { extracted, remaining };
						_operator = UnaryOperator.None;
					} break;
			}

			switch (role)
			{
				case PartialExpressionRole.EmptyWord:
					{
						//if(root != null && root.role.Equals(PartialExpressionRole.Union))
						role = PartialExpressionRole.Invalid;
					} break;
				case PartialExpressionRole.Letter:
					{
						if (_value.Equals(removedLetter))
							role = PartialExpressionRole.EmptyWord;
						else
							role = PartialExpressionRole.Invalid;
						_value = null;
					} break;
				case PartialExpressionRole.Concatenation:
					{
						var firstPart = parts[0];
						if (firstPart._operator == UnaryOperator.KleeneStar)
						{
							// here also derivation can split into two variants

							PartialExpression new1firstPart = new PartialExpression(firstPart);
							new1firstPart._operator = UnaryOperator.None;
							//new1firstPart.parts.RemoveAll(x => x.role.Equals(PartialExpressionRole.EmptyWord));

							var new1 = new PartialExpression(this);
							new1.parts.RemoveAt(0);
							new1.Optimize();
							new1.root = this;

							var new2 = new PartialExpression(this);
							new2.parts.Insert(0, new1firstPart);
							new1firstPart.root = new2;
							new2.root = this;

							role = PartialExpressionRole.Union;
							parts = new List<PartialExpression> { new1, new2 };

							Derive(removedLetter);
						}
						else if (firstPart.role == PartialExpressionRole.Union
							&& firstPart.parts.Any(x => x.role == PartialExpressionRole.EmptyWord))
						{
							// this union contains an empty word, so the derivation can be split in two variants

							PartialExpression new1firstPart = new PartialExpression(firstPart);
							new1firstPart.parts.RemoveAll(x => x.role == PartialExpressionRole.EmptyWord);

							var new1 = new PartialExpression(this);
							new1.parts.RemoveAt(0);
							new1.parts.Insert(0, new1firstPart);
							new1firstPart.root = new1;
							new1.root = this;

							var new2 = new PartialExpression(this);
							new2.parts.RemoveAt(0);
							new2.Optimize();
							new2.root = this;

							role = PartialExpressionRole.Union;
							parts = new List<PartialExpression> { new1, new2 };

							Derive(removedLetter);
						}
						else
						{
							parts[0].Derive(removedLetter);
							if (parts[0].role == PartialExpressionRole.Invalid)
							{
								parts.Clear();
								parts = null;
								role = PartialExpressionRole.Invalid;
							}
							else if (parts.Count > 1)
							{
								if (parts[0].role == PartialExpressionRole.EmptyWord)
								{
									parts.RemoveAt(0);
									if (parts.Count == 1)
										Optimize();
								}
							}
							else
							{
								if (parts[0].role == PartialExpressionRole.EmptyWord)
								{
									parts.RemoveAt(0);
									parts = null;
									role = PartialExpressionRole.EmptyWord;
								}
							}
						}
					} break;
				case PartialExpressionRole.Union:
					{
						Parallel.For(0, parts.Count, (int n) =>
							{
								parts[n].Derive(removedLetter);
							});
						//foreach (var part in parts)
						//	part.Derive(removedLetter);
						for (int i = parts.Count - 1; i >= 0; --i)
						{
							if (parts[i].role == PartialExpressionRole.Invalid)
								parts.RemoveAt(i);
						}
						if (parts.Count == 1)
							Optimize();
						else if (parts.Count == 0)
						{
							parts = null;
							role = PartialExpressionRole.Invalid;
						}
					} break;
				default:
					throw new ArgumentException("encountered partial expression with role that is not allowed");
			}
		}

		/// <summary>
		/// Checks if this expression can generate an empty word.
		/// </summary>
		/// <returns></returns>
		public bool GeneratesEmptyWord()
		{
			//if (role.Equals(PartialExpressionRole.EmptyWord))
			//	return true;

			if (_operator.Equals(UnaryOperator.KleeneStar))
				return true;

			switch (role)
			{
				case PartialExpressionRole.EmptyWord:
					return true;
				case PartialExpressionRole.Union:
					if (parts.Any(x => x.GeneratesEmptyWord()))
						return true;
					break;
				case PartialExpressionRole.Concatenation:
					if (parts.All(x => x.GeneratesEmptyWord()))
						return true;
					break;
			}

			//if (role.Equals(PartialExpressionRole.Union)
			//	&& parts.Any(x => x.GeneratesEmptyWord()))
			//	return true;

			//if (role.Equals(PartialExpressionRole.Concatenation)
			//	&& parts.All(x => x.GeneratesEmptyWord()))
			//	return true;

			return false;
		}

		/// <summary>
		/// Calculates the width of this parse tree. Width is understood as the number of the leaves.
		/// </summary>
		/// <returns>Positive number if this is a valid partial expression,
		/// zero if the role of this expression is not determined,
		/// -1 otherwise.</returns>
		public int CalculateTreeWidth()
		{
			if (role == PartialExpressionRole.Undetermined)
				return 0;
			if ((role & PartialExpressionRole.Leaf) > 0)
				return 1;

			if ((role & PartialExpressionRole.InternalNode) > 0)
				return parts.Sum(x => x.CalculateTreeWidth());

			return -1;
		}

		/// <summary>
		/// Calculates the height of this parse tree. Height is understood as the maximum distance
		/// from root to any of the leaves, measured by counting nodes, including both root and the leaf.
		/// </summary>
		/// <returns>Positive number if this is a valid partial expression,
		/// zero if the role of this expression is not determined,
		/// -1 otherwise.</returns>
		public int CalculateTreeHeight()
		{
			if (role == PartialExpressionRole.Undetermined)
				return 0;
			if ((role & PartialExpressionRole.Leaf) > 0)
				return 1;

			if ((role & PartialExpressionRole.InternalNode) > 0)
				return parts.Max(x => x.CalculateTreeHeight()) + 1;

			return -1;
		}

		public bool ContentEquals(object obj, bool compareOperators = false)
		{
			if (obj == null)
				return false;
			if (obj is PartialExpression == false)
				return false;

			var part = (PartialExpression)obj;

			if (this == part)
				return true;

			if (role != part.role)
				return false;

			if (compareOperators)
			{
				if (_operator != part._operator)
					return false;
			}

			switch (role)
			{
				case PartialExpressionRole.EmptyWord:
					return true;
				case PartialExpressionRole.Letter:
					return _value.Equals(part._value);
				case PartialExpressionRole.Concatenation:
					{
						if (parts.Count != part.parts.Count)
							return false;

						for (int i = 0; i < parts.Count; ++i)
							if (!parts[i].Equals(part.parts[i]))
								return false;

						return true;
					}
				case PartialExpressionRole.Union:
					{
						foreach (var p in parts)
							if (part.parts.Count(x => x.Equals(p)) == 0)
								return false;
						foreach (var p in part.parts)
							if (parts.Count(x => x.Equals(p)) == 0)
								return false;

						return true;
					}
				case PartialExpressionRole.Undetermined:
				case PartialExpressionRole.Invalid:
				default:
					return true;
			}
			// return base.Equals(obj);
		}

		public override bool Equals(object obj)
		{
			return ContentEquals(obj, true);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString()
		{
			StringBuilder s = new StringBuilder();

			string parOpen = RegularExpression.TagsStrings[InputSymbolTag.OpeningParenthesis];
			string parClose = RegularExpression.TagsStrings[InputSymbolTag.ClosingParenthesis];

			if (_operator != UnaryOperator.None)
			{
				if (root != null)
					s.Append(parOpen); // outer parenthesis
				if (parts == null || parts.Count <= 1)
					s.Append(parOpen);
			}

			if ((role & PartialExpressionRole.InternalNode) > 0 && parts.Count > 0)
			{
				bool extraParentheses = (parts.Count > 1 && root != null) || _operator != UnaryOperator.None;

				if (extraParentheses)
					s.Append(parOpen);
				switch (role)
				{
					case PartialExpressionRole.Union:
						// this part represents union of several expressions
						s.Append(String.Join<PartialExpression>(RegularExpression.TagsStrings[InputSymbolTag.Union], parts));
						break;
					case PartialExpressionRole.Concatenation:
						// this part represents concatenation of several expressions
						foreach (PartialExpression partexp in parts)
							s.Append(partexp);
						break;
				}
				if (extraParentheses)
					s.Append(parClose);
			}
			else if (role == PartialExpressionRole.Letter && _value != null && _value.Length > 0)
			{
				// this part represents a single letter
				s.Append(_value);
			}
			else if (role == PartialExpressionRole.EmptyWord)
			{
				// this part represents an empty word
				s.Append(RegularExpression.TagsStrings[InputSymbolTag.EmptyWord]);
			}
			else
				return null; //throw new ArgumentException("invalid partial expression, it contains no data");

			if (_operator != UnaryOperator.None)
			{
				if (parts == null || parts.Count <= 1)
					s.Append(parClose);
				s.Append(RegularExpression.TagsStrings[(InputSymbolTag)_operator]);
				if (root != null)
					s.Append(parClose); // outer parenthesis
			}

			return s.ToString();
		}

	}
}
