using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Phinite
{
	public class PartialExpression
	{
		//public static readonly PartialExpression EmptyWord = new PartialExpression(null, ".");

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

		///// <summary>
		///// Sub-expressions that are in union with each other.
		///// </summary>
		//public ReadOnlyCollection<PartialExpression> Union
		//{
		//	get
		//	{
		//		if (union == null)
		//			return null;
		//		return new ReadOnlyCollection<PartialExpression>(union);
		//	}
		//}
		///// <summary>
		///// Sub-expressions that are concatenated with each other
		///// </summary>
		//public ReadOnlyCollection<PartialExpression> ConcatenatedSymbols
		//{
		//	get
		//	{
		//		if (concatenatedSymbols == null)
		//			return null;
		//		return new ReadOnlyCollection<PartialExpression>(concatenatedSymbols);
		//	}
		//}
		//private List<PartialExpression> union;
		//private List<PartialExpression> concatenatedSymbols;

		///// <summary>
		///// Returns the last symbol that is in this part's concatenation.
		///// </summary>
		//public PartialExpression LastConcatenatedSymbol
		//{
		//	get
		//	{
		//		int index = concatenatedSymbols.Count - 1;
		//		if (index < 0)
		//			throw new ArgumentException("cannot get a non-existing last concatenated symbol of the expression");
		//		return concatenatedSymbols[index];
		//	}
		//}

		/// <summary>
		/// Value of this expression, provided that it is a single character.
		/// </summary>
		public string Value
		{
			get { return _value; }
			set
			{
				//if (union != null || concatenatedSymbols != null)
				if (!role.Equals(PartialExpressionRole.Letter))
					throw new ArgumentException();
				if (_value != null)
					throw new ArgumentException("cannot change value of expression's part if it is already set");
				_value = value;
			}
		}
		private string _value;

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

		public PartialExpression(PartialExpressionRole role, PartialExpression root)
		{
			this.role = PartialExpressionRole.Undetermined;
			this.role = role;
			this.root = root;

			this.parts = null;
			this._value = null;
			this._operator = UnaryOperator.None;
		}

		public PartialExpression(PartialExpression root, List<PartialExpression> parts, bool concatenation)
			: this(concatenation ? PartialExpressionRole.Concatenation : PartialExpressionRole.Union, root)
		{
			this.parts = parts;
		}

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

		public void AddToUnion(PartialExpression exp)
		{
			//if (concatenatedSymbols != null || _value != null)
			if (!this.role.Equals(PartialExpressionRole.Union))
				throw new ArgumentException("cannot add symbol to a partial expression that does not store union");
			//if (union == null)
			//	union = new List<PartialExpression>();
			if (parts == null)
				parts = new List<PartialExpression>();
			//this.union.Add(exp);
			parts.Add(exp);
		}

		public void AddToConcatenation(PartialExpression exp)
		{
			//if (role.Equals(PartialExpressionRole.Undetermined))
			//	role = PartialExpressionRole.Concatenation;
			//if (union != null || _value != null)
			//else 
			if (!role.Equals(PartialExpressionRole.Concatenation))
				throw new ArgumentException("cannot concatenate symbol with a partial expression that does not store concatenation");
			//if (concatenatedSymbols == null)
			//	concatenatedSymbols = new List<PartialExpression>();
			if (parts == null)
				parts = new List<PartialExpression>();
			//this.concatenatedSymbols.Add(exp);
			parts.Add(exp);
		}

		public void Optimize()
		{
			if ((role.Equals(PartialExpressionRole.Concatenation)
				|| role.Equals(PartialExpressionRole.Union))
				&& parts.Count == 1)
			{
				// this is a concatenation or union of single element, meaning that
				// in fact it is not a union or a concatenation, but rather
				// just a single element

				var part = parts[0];

				if (part.role.Equals(PartialExpressionRole.Concatenation)
					|| part.role.Equals(PartialExpressionRole.Union))
				{
					foreach (var p in part.parts)
						p.root = this;
				}
				if (_operator.Equals(UnaryOperator.None))
					_operator = part._operator;
				else if (part._operator.Equals(UnaryOperator.None))
				{
					// nothing needed here, the operator stays as it was
				}
				else if (_operator.Equals(UnaryOperator.KleeneStar)
					|| part._operator.Equals(UnaryOperator.KleeneStar))
					_operator = UnaryOperator.KleeneStar;
				else if (_operator.Equals(UnaryOperator.KleenePlus)
					&& part._operator.Equals(UnaryOperator.KleenePlus))
					_operator = UnaryOperator.KleenePlus;
				else
					throw new NotImplementedException(
						"there is some not implemented case in operator pair handling in PartialExpression.Optimize()");

				_value = part._value;
				parts = part.parts;
				role = part.role;
				Optimize(); // optmize once again
			}
			else if (role.Equals(PartialExpressionRole.Concatenation))
			{
				for (int i = parts.Count - 1; i >= 0; --i)
				{
					var p = parts[i];
					if (p.role.Equals(PartialExpressionRole.EmptyWord))
						parts.RemoveAt(i);
					else
						p.Optimize();
				}

				if (parts.Count == 1)
					Optimize();
				else if (parts.Count == 0)
				{
					parts = null;
					role = PartialExpressionRole.EmptyWord;
				}
			}
			else if (role.Equals(PartialExpressionRole.Union))
			{
				foreach (var p in parts)
					p.Optimize();
				for (int i = parts.Count - 1; i >= 0; --i)
				{
					var p = parts[i];
					if (parts.Count(x => x.Equals(p)) > 1)
					{
						// there are duplicates in this union
						parts.RemoveAt(i);
					}
				}
				if (parts.Count == 1)
					Optimize();
				else if (parts.Count == 0)
				{
					parts = null;
					role = PartialExpressionRole.EmptyWord;
				}
			}
		}

		//public bool IsEmpty()
		//{
		//	return _value == null && concatenatedSymbols == null && union == null;
		//}

		public void Derive(string removedLetter)
		{
			// to properly derive expressions with unary operators, they have to be transformed
			if (_operator.Equals(UnaryOperator.KleeneStar))
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
			}
			else if (_operator.Equals(UnaryOperator.KleenePlus))
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
			}

			if (role.Equals(PartialExpressionRole.EmptyWord))
			{
				//if(root != null && root.role.Equals(PartialExpressionRole.Union))
				role = PartialExpressionRole.Invalid;
			}
			else if (role.Equals(PartialExpressionRole.Letter))
			{
				if (_value.Equals(removedLetter))
					role = PartialExpressionRole.EmptyWord;
				else
					role = PartialExpressionRole.Invalid;
				_value = null;
			}
			else if (role.Equals(PartialExpressionRole.Concatenation))
			{
				var firstPart = parts[0];
				if (firstPart.role.Equals(PartialExpressionRole.Union)
					&& firstPart.parts.Any(x => x.role.Equals(PartialExpressionRole.EmptyWord)))
				{
					// this union contains an empty word, so the derivation can be split in two variants

					PartialExpression new1firstPart = new PartialExpression(firstPart);
					new1firstPart.parts.RemoveAll(x => x.role.Equals(PartialExpressionRole.EmptyWord));

					var new1 = new PartialExpression(this);
					new1.parts.RemoveAt(0);
					new1.parts.Insert(0, new1firstPart);
					new1firstPart.root = new1;
					new1.root = this;

					var new2 = new PartialExpression(this);
					new2.parts.RemoveAt(0);
					//TODO: handle if now there is only 1 part
					new2.Optimize();
					new2.root = this;

					role = PartialExpressionRole.Union;
					parts = new List<PartialExpression> { new1, new2 };

					Derive(removedLetter);
				}
				else if (firstPart._operator.Equals(UnaryOperator.KleeneStar))
				{
					PartialExpression new1firstPart = new PartialExpression(firstPart);
					new1firstPart._operator = UnaryOperator.None;
					//new1firstPart.parts.RemoveAll(x => x.role.Equals(PartialExpressionRole.EmptyWord));

					var new1 = new PartialExpression(this);
					new1.parts.RemoveAt(0);
					//TODO: handle if now there is only 1 part
					new1.Optimize();
					//new1.parts.Insert(0, new1firstPart);
					//new1firstPart.root = new1;
					new1.root = this;

					var new2 = new PartialExpression(this);
					new2.parts.Insert(0, new1firstPart);
					new1firstPart.root = new2;
					//new2.parts.RemoveAt(0);
					new2.root = this;

					role = PartialExpressionRole.Union;
					parts = new List<PartialExpression> { new1, new2 };

					Derive(removedLetter);
				}
				else
				{
					parts[0].Derive(removedLetter);
					if (parts[0].Role.Equals(PartialExpressionRole.Invalid))
					{
						parts.Clear();
						parts = null;
						role = PartialExpressionRole.Invalid;
					}
					else if (parts.Count > 1)
					{
						if (
							//parts[0].Role.Equals(PartialExpressionRole.Invalid)
							//|| 
							parts[0].Role.Equals(PartialExpressionRole.EmptyWord))
						{
							parts.RemoveAt(0);
							//TODO: optimize if now only a single element remains
							Optimize();
						}
					}
					else
					{
						//if (parts[0].Role.Equals(PartialExpressionRole.Invalid))
						//{
						//	role = PartialExpressionRole.Invalid;
						//}
						//else 
						if (parts[0].Role.Equals(PartialExpressionRole.EmptyWord))
						{
							parts.RemoveAt(0);
							parts = null;
							role = PartialExpressionRole.EmptyWord;
						}
						//concatenatedSymbols.RemoveAt(0);
						//if (concatenatedSymbols.Count == 0)
						//	concatenatedSymbols = null;
					}
				}
			}
			else if (role.Equals(PartialExpressionRole.Union))
			{
				foreach (var part in parts)
					part.Derive(removedLetter);
				for (int i = parts.Count - 1; i >= 0; --i)
				{
					if (parts[i].role.Equals(PartialExpressionRole.Invalid))
						parts.RemoveAt(i);
				}
				if (parts.Count == 0)
				{
					parts = null;
					role = PartialExpressionRole.Invalid;
				}
			}
			else
				throw new ArgumentException("encountered partial expression with role that is not allowed");
		}

		public bool GeneratesEmptyWord()
		{
			if (role.Equals(PartialExpressionRole.EmptyWord))
				return true;

			if (_operator.Equals(UnaryOperator.KleeneStar))
				return true;

			if (role.Equals(PartialExpressionRole.Union)
				&& parts.Any(x => x.GeneratesEmptyWord()))
				return true;

			if (role.Equals(PartialExpressionRole.Concatenation)
				&& parts.All(x => x.GeneratesEmptyWord()))
				return true;

			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (obj is PartialExpression == false)
				return false;

			var part = (PartialExpression)obj;

			if (this == part)
				return true;

			if (!role.Equals(part.role))
				return false;

			if (!_operator.Equals(part._operator))
				return false;

			if (role.Equals(PartialExpressionRole.EmptyWord))
				return true;
			else if (role.Equals(PartialExpressionRole.Letter))
				return _value.Equals(part._value);
			else if (role.Equals(PartialExpressionRole.Concatenation))
			{
				if (parts.Count != part.parts.Count)
					return false;

				for (int i = 0; i < parts.Count; ++i)
					if (!parts[i].Equals(part.parts[i]))
						return false;

				return true;
			}
			else if (role.Equals(PartialExpressionRole.Union))
			{
				foreach (var p in parts)
					if (part.parts.Count(x => x.Equals(p)) == 0)
						return false;
				return true;
			}
			else if (role.Equals(PartialExpressionRole.Undetermined) || role.Equals(PartialExpressionRole.Invalid))
				return true;

			return base.Equals(obj);
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

			if (role.Equals(PartialExpressionRole.Union) && parts.Count > 0)
			{
				// this part represents union of several expressions
				if ((parts.Count > 1 && root != null) || _operator != UnaryOperator.None)
					s.Append(parOpen);
				s.Append(String.Join<PartialExpression>(RegularExpression.TagsStrings[InputSymbolTag.Union], parts));
				if ((parts.Count > 1 && root != null) || _operator != UnaryOperator.None)
					s.Append(parClose);
			}
			else if (role.Equals(PartialExpressionRole.Concatenation) && parts.Count > 0)
			{
				// this part represents concatenation of several expressions
				if ((parts.Count > 1 && root != null) || _operator != UnaryOperator.None)
					s.Append(parOpen);
				foreach (PartialExpression partexp in parts)
					s.Append(partexp);
				if ((parts.Count > 1 && root != null) || _operator != UnaryOperator.None)
					s.Append(parClose);
			}
			else if (role.Equals(PartialExpressionRole.Letter) && _value != null && _value.Length > 0)
			{
				// this part represents a single letter
				s.Append(_value);
			}
			else if (role.Equals(PartialExpressionRole.EmptyWord))
			{
				// this part represents an empty word
				s.Append(RegularExpression.TagsStrings[InputSymbolTag.EmptyWord]);
			}
			else
				throw new ArgumentException("invalid partial expression, it contains no data");

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
