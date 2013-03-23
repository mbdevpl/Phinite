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
		Undetermined = 0,

		Leaf = 1 << 10,
		EmptyWord = Leaf | 1,
		Letter = Leaf | 1 << 1,

		InternalNode = 1 << 11,
		Concatenation = InternalNode | 1,
		Union = InternalNode | 1 << 1,

		Invalid = 1 << 20
	}
}
