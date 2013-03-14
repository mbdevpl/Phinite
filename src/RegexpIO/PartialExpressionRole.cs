using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phinite
{
	/// <summary>
	/// List of roles of parts of a regular expression.
	/// </summary>
	public enum PartialExpressionRole
	{
		EmptyWord = 2 + 1,
		Letter = 4 + 1,
		Concatenation = 16 + 8,
		Union = 32 + 8,

		InternalNode = 8,
		Leaf = 1,
		Undetermined = 0,
		Invalid = 1024
	}
}
