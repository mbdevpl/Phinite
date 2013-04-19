using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phinite
{
	/// <summary>
	/// List of semantic elements of regular expression. It is used to tag the input,
	/// i.e. recognize its semantic structure.
	/// </summary>
	public enum InputSymbolTag
	{
		/// <summary>
		/// Any symbol that is not special/reserved, or ignored, is a letter.
		/// </summary>
		Letter = 0,

		/// <summary>
		/// This sybmol denotes the union.
		/// </summary>
		Union = 1,

		/// <summary>
		/// Not for tagging input, only for use in comparisons.
		/// </summary>
		UnaryOperator = 2,
		/// <summary>
		/// This is a Kleene star.
		/// </summary>
		KleeneStar = 4 + 2,
		/// <summary>
		/// This is a Kleene plus.
		/// </summary>
		KleenePlus = 8 + 2,

		/// <summary>
		/// Not for tagging input, only for use in comparisons.
		/// </summary>
		Parenthesis = 16,
		/// <summary>
		/// This is an opening parenthesis.
		/// </summary>
		OpeningParenthesis = 32 + 16,
		/// <summary>
		/// This is a closing parenthesis.
		/// </summary>
		ClosingParenthesis = 64 + 16,

		/// <summary>
		/// This is an empty word symbol.
		/// </summary>
		EmptyWord = 128
	}
}
