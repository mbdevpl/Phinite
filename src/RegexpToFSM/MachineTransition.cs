using System;
using System.Collections.Generic;
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

	}

	//public class MachineTransition : Tuple<RegularExpression, List<string>, RegularExpression>
	//{
	//	/// <summary>
	//	/// Creates a new finite-state machine transition.
	//	/// </summary>
	//	/// <param name="initialState">initial state</param>
	//	/// <param name="letter">a letter</param>
	//	/// <param name="resultingState">resulting state</param>
	//	public MachineTransition(RegularExpression initialState, string letter,
	//		RegularExpression resultingState)
	//		: base(initialState, new List<string>(), resultingState)
	//	{
	//		AddLetter(letter);
	//	}

	//	/// <summary>
	//	/// Creates a new finite-state machine transition.
	//	/// </summary>
	//	/// <param name="initialState">initial state</param>
	//	/// <param name="letters">a list of letters</param>
	//	/// <param name="resultingState">resulting state</param>
	//	public MachineTransition(RegularExpression initialState, IEnumerable<string> letters,
	//		RegularExpression resultingState)
	//		: base(initialState, new List<string>(), resultingState)
	//	{
	//		AddAllLetters(letters);
	//	}

	//	/// <summary>
	//	/// Adds a letter to this transition.
	//	/// </summary>
	//	/// <param name="letter">a letter</param>
	//	public void AddLetter(string letter)
	//	{
	//		Item2.Add(letter);
	//	}

	//	/// <summary>
	//	/// Adds a list of letters to this transition.
	//	/// </summary>
	//	/// <param name="letters">a list of letters</param>
	//	public void AddAllLetters(IEnumerable<string> letters)
	//	{
	//		Item2.AddRange(letters);
	//	}

	//}
}
