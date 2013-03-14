using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phinite
{
	/// <summary>
	/// List of unary operators encountered in regular expressions.
	/// </summary>
	public enum UnaryOperator
	{
		None = 0,
		/// <summary>
		/// Not for tagging input, only for use in comparisons.
		/// </summary>
		Existing = 2,
		KleeneStar = 4 + 2,
		KleenePlus = 8 + 2
	}
}
