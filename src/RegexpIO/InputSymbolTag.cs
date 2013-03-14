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
		Union = 1,
		/// <summary>
		/// Not for tagging input, only for use in comparisons.
		/// </summary>
		UnaryOperator = 2,
		KleeneStar = 4 + 2,
		KleenePlus = 8 + 2,
		/// <summary>
		/// Not for tagging input, only for use in comparisons.
		/// </summary>
		Parenthesis = 16,
		OpeningParenthesis = 32 + 16,
		ClosingParenthesis = 64 + 16,
		EmptyWord = 128
	}
}
