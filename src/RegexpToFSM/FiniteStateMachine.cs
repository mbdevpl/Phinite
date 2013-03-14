using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phinite
{
	/// <summary>
	/// Stores all information about a finite-state machine: states and transitions, also about
	/// initial and final states. 
	/// </summary>
	public class FiniteStateMachine
	{
		/// <summary>
		/// Original input expression that was used to generate this machine.
		/// </summary>
		public RegularExpression Input { get { return Input; } }
		private RegularExpression input;

		/// <summary>
		/// A read-only collection of states of this machine.
		/// </summary>
		public ReadOnlyCollection<RegularExpression> States
		{
			get { return states == null ? null : new ReadOnlyCollection<RegularExpression>(states); }
		}
		private List<RegularExpression> states;

		/// <summary>
		/// A read-only collection of transitions of the machine.
		/// </summary>
		public ReadOnlyCollection<Tuple<RegularExpression, string, RegularExpression>> Transitions
		{
			get
			{
				return transitions == null
					? null : new ReadOnlyCollection<Tuple<RegularExpression, string, RegularExpression>>(transitions);
			}
		}
		private List<Tuple<RegularExpression, string, RegularExpression>> transitions;

		/// <summary>
		/// Initial state of the finite-state machine.
		/// </summary>
		public RegularExpression InitialState
		{
			get { return (states == null || states.Count == 0) ? null : states[0]; }
		}

		/// <summary>
		/// A read-only collection of final states of the machine.
		/// </summary>
		public ReadOnlyCollection<RegularExpression> FinalStates
		{
			get { return finalStates == null ? null : new ReadOnlyCollection<RegularExpression>(finalStates); }
		}
		private List<RegularExpression> finalStates;

		/// <summary>
		/// Creates a new finite-state machine from a given regular expression.
		/// </summary>
		/// <param name="input">any regular expression</param>
		/// <param name="evaluateImmediately">if false, the input will be evaluated
		/// only after the method EvaluateInput() is invoked</param>
		public FiniteStateMachine(RegularExpression input, bool evaluateImmediately = false)
		{
			this.input = input;
			if (evaluateImmediately)
				EvaluateInput();
		}

		public void EvaluateInput()
		{
			FindTransitions();
			FindFinalStates();
		}

		private void FindTransitions()
		{
			states = new List<RegularExpression>();
			transitions = new List<Tuple<RegularExpression, string, RegularExpression>>();

			List<RegularExpression> notEvaluated = new List<RegularExpression>();
			notEvaluated.Add(input);
			while (notEvaluated.Count > 0)
			{
				var current = notEvaluated[0];
				notEvaluated.RemoveAt(0);
				if (states.Count(x => x.Equals(current)) > 0)
					continue;

				states.Add(current);
				List<RegularExpression> newDerivations = new List<RegularExpression>();
				foreach (var letter in current.Alphabet)
				{
					var derived = current.Derive(letter);
					if (derived == null)
						continue;
					int stateIndex = states.FindIndex(x => x.Equals(derived));
					if (stateIndex >= 0)
					{
						derived = states[stateIndex];
					}
					else
						newDerivations.Add(derived);
					int transitionIndex = transitions.FindIndex(x => x.Item1.Equals(current) && x.Item3.Equals(derived));
					if (transitionIndex >= 0)
					{
						string letters = new StringBuilder().Append(transitions[transitionIndex].Item2)
							.Append(", ").Append(letter).ToString();
						transitions[transitionIndex]
							= new Tuple<RegularExpression, string, RegularExpression>(current, letters, derived);
					}
					else
						transitions.Add(new Tuple<RegularExpression, string, RegularExpression>(current, letter, derived));
				}
				notEvaluated.AddRange(newDerivations);
			}
		}

		private void FindFinalStates()
		{
			finalStates = new List<RegularExpression>();

			foreach (var state in states)
			{
				if (state.GeneratesEmptyWord())
					finalStates.Add(state);
			}
		}

	}
}
