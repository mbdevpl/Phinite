
namespace Phinite
{
	/// <summary>
	/// List of roles of parts of a regular expression.
	/// </summary>
	public enum PartialExpressionRole
	{
		/// <summary>
		/// The role of this part is not yet determined, it can assume any role.
		/// </summary>
		Undetermined = 0,

		/// <summary>
		/// This partial expression cannot have any sub-parts.
		/// </summary>
		Leaf = 1 << 10,
		/// <summary>
		/// Empty word.
		/// </summary>
		EmptyWord = Leaf | 1,
		/// <summary>
		/// Letter.
		/// </summary>
		Letter = Leaf | 1 << 1,

		/// <summary>
		/// This expression has sub-parts.
		/// </summary>
		InternalNode = 1 << 11,
		/// <summary>
		/// A concatenation.
		/// </summary>
		Concatenation = InternalNode | 1,
		/// <summary>
		/// A union.
		/// </summary>
		Union = InternalNode | 1 << 1,

		/// <summary>
		/// This partial expression is invalid.
		/// </summary>
		Invalid = 1 << 20
	}
}
