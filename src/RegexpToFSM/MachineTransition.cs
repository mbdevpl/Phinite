using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Phinite
{
	/// <summary>
	/// A pair of states that are directly connected together by (possibly multiple) character(s).
	/// </summary>
	public class MachineTransition : Tuple<int, List<string>, int>
	{
		/// <summary>
		/// Index of initial state, in the States array.
		/// </summary>
		public int InitialStateId { get { return Item1; } }

		/// <summary>
		/// List of letters involved in the transition.
		/// </summary>
		public ReadOnlyCollection<string> Letters
		{
			get
			{
				if (Item2 == null)
					return null;
				return new ReadOnlyCollection<string>(Item2);
			}
		}

		/// <summary>
		/// Index of resulting index, in the States array.
		/// </summary>
		public int ResultingStateId { get { return Item3; } }

		/// <summary>
		/// Creates a new finite-state machine transition.
		/// </summary>
		/// <param name="initialStateId">initial state id</param>
		/// <param name="letter">a letter</param>
		/// <param name="resultingStateId">resulting state id</param>
		public MachineTransition(int initialStateId, string letter,
			int resultingStateId)
			: base(initialStateId, new List<string>(), resultingStateId)
		{
			AddLetter(letter);
		}

		///// <summary>
		///// Creates a new finite-state machine transition.
		///// </summary>
		///// <param name="initialState">initial state</param>
		///// <param name="letters">a list of letters</param>
		///// <param name="resultingState">resulting state</param>
		//public MachineTransition(RegularExpression initialState, IEnumerable<string> letters,
		//	RegularExpression resultingState)
		//	: base(initialState, new List<string>(), resultingState)
		//{
		//	AddAllLetters(letters);
		//}

		/// <summary>
		/// Adds a letter to this transition.
		/// </summary>
		/// <param name="letter">a letter</param>
		public void AddLetter(string letter)
		{
			Item2.Add(letter);
		}

		/// <summary>
		/// Adds a list of letters to this transition.
		/// </summary>
		/// <param name="letters">a list of letters</param>
		public void AddAllLetters(IEnumerable<string> letters)
		{
			Item2.AddRange(letters);
		}

		/// <summary>
		/// Checks if this transition contains a specified letter.
		/// </summary>
		/// <param name="letter"></param>
		/// <returns></returns>
		public bool ContainsLetter(string letter)
		{
			if (Item2 == null)
				return false;

			return Item2.Any(x => x.Equals(letter));
		}

		/// <summary>
		/// Converts this transition into a simple string in form of "q#---(letters)-->q#".
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("q{0}---({1})-->q{2}", Item1, String.Join(",", Item2), Item3);
		}

	}
}
