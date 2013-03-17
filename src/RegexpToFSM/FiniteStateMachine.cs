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
		/// Initial state of the finite-state machine.
		/// </summary>
		public RegularExpression InitialState { get { return initialState; } }
		private RegularExpression initialState;

		/// <summary>
		/// List of lately encountered expressions that are not yet labeled.
		/// </summary>
		private List<RegularExpression> notLabeled;

		/// <summary>
		/// List of all expressions that need to be derived.
		/// </summary>
		//private List<RegularExpression> notDerived;
		private List<int> notDerivedIds;

		/// <summary>
		/// A read-only collection of states of this machine.
		/// </summary>
		public ReadOnlyCollection<RegularExpression> States
		{
			get
			{
				//return states == null ? null : new ReadOnlyCollection<RegularExpression>(states);
				if (equivalentStatesGroups == null)
					return null;
				var states = new List<RegularExpression>();
				foreach (var stateGroup in equivalentStatesGroups)
				{
					if (stateGroup.Value.Count == 0)
						throw new ArgumentException("an equivalent state group must have at least member state");
					// TODO: use state with shortest string representation instead of the 1st state
					states.Add(stateGroup.Value[0]);
				}
				return new ReadOnlyCollection<RegularExpression>(states);
			}
		}
		//private List<RegularExpression> states;

		/// <summary>
		/// This list groups together states that were found to be equivalent.
		/// 
		/// The equivalence was determined either by program or by the user.
		/// </summary>
		private List<KeyValuePair<int, List<RegularExpression>>> equivalentStatesGroups;

		private List<Tuple<int, string, RegularExpression>> notOptimizedTransitions;

		/// <summary>
		/// A read-only collection of transitions of the machine.
		/// </summary>
		public ReadOnlyCollection<MachineTransition> Transitions
		{
			get
			{
				return transitions == null
					? null : new ReadOnlyCollection<MachineTransition>(transitions);
			}
		}
		private List<MachineTransition> transitions;

		/// <summary>
		/// A read-only collection of final states of the machine.
		/// </summary>
		public ReadOnlyCollection<RegularExpression> FinalStates
		{
			get
			{
				//return finalStates == null ? null : new ReadOnlyCollection<RegularExpression>(finalStates);
				if (finalStatesIds == null)
					return null;

				var finalStates = new List<RegularExpression>();
				foreach (var id in finalStatesIds)
				{
					var stateGroup = equivalentStatesGroups[id].Value;
					if (stateGroup.Count == 0)
						throw new ArgumentException("an equivalent state group must have at least member state");
					// TODO: use state with shortest string representation instead of the 1st state
					finalStates.Add(stateGroup[0]);
				}
				return new ReadOnlyCollection<RegularExpression>(finalStates);
			}
		}
		//private List<RegularExpression> finalStates;
		private List<int> finalStatesIds;

		/// <summary>
		/// Creates a new finite-state machine from a given regular expression.
		/// </summary>
		/// <param name="input">any regular expression</param>
		/// <param name="constructImmediately">if false, the machine will be constructed
		/// only after the method Construct() is invoked</param>
		public FiniteStateMachine(RegularExpression input, bool constructImmediately = false)
		{
			this.input = input;

			InitializeEvaluation();
			if (constructImmediately)
				Construct(0);
		}

		/// <summary>
		/// The method performs at most chosen number of steps of finite-state machine construction.
		/// 
		/// The method may end prematurely when the machine is already constructed.
		/// </summary>
		/// <param name="numberOfSteps">maximum number of steps that will be taken, zero for complete construction</param>
		public void Construct(int numberOfSteps)
		{
			//FindTransitions();
			//FindFinalStates();
			while (numberOfSteps > 0 && !IsConstructionFinished())
			{
				if (!LabelNextExpression())
				{
					// automatically handle uncertain cases
				}
				if (!DeriveNextExpression())
				{
					// automatically handle uncertain cases
				}
				if (numberOfSteps > 0)
					--numberOfSteps;
			}
		}

		private void InitializeEvaluation()
		{
			notLabeled = new List<RegularExpression>();
			notLabeled.Add(input);

			//notDerived = new List<RegularExpression>();
			notDerivedIds = new List<int>();

			//states = new List<RegularExpression>();
			equivalentStatesGroups = new List<KeyValuePair<int, List<RegularExpression>>>();

			notOptimizedTransitions = new List<Tuple<int, string, RegularExpression>>();

			transitions = new List<MachineTransition>();

			finalStatesIds = new List<int>();
		}

		public bool IsConstructionFinished()
		{
			return notLabeled.Count == 0 && notDerivedIds.Count == 0 && notOptimizedTransitions.Count == 0;
		}

		public bool LabelNextExpression()
		{
			if (notLabeled.Count == 0)
				return false;

			var labeled = notLabeled[0];
			int labeledId = -1;
			var foundNew = false;

			if (initialState == null)
				initialState = labeled; // first state becomes the initial state

			if (equivalentStatesGroups.Count == 0)
			{
				foundNew = true;
				labeledId = 0;
			}
			else
			{
				foreach (var stateGroup in equivalentStatesGroups)
				{
					if (stateGroup.Value.Any(x => x.Equals(labeled)))
					{
						// found an exact duplicate, returning
						notLabeled.RemoveAt(0);
						return true;
						//foundNew = true;
						//break;
					}
				}
				//TODO: handle the unsure situation here!

				// this looks like a new state, but it may as well be not clearly but still
				// equivalent of one existing state

				foundNew = true;
			}

			if (foundNew)
			{
				labeledId = equivalentStatesGroups.Count;
				notLabeled.RemoveAt(0);
				equivalentStatesGroups.Add(new KeyValuePair<int, List<RegularExpression>>(labeledId, new List<RegularExpression>()));
				equivalentStatesGroups[labeledId].Value.Add(labeled);

				if (labeled.GeneratesEmptyWord())
					finalStatesIds.Add(labeledId);

				notDerivedIds.Add(labeledId);

				if (notOptimizedTransitions.Count > 0)
				{
					var transitionsForOptimization = notOptimizedTransitions.FindAll(x => x.Item3.Equals(labeled));

					transitionsForOptimization.ForEach(x => transitions.Add(new MachineTransition(x.Item1, x.Item2, labeledId)));
					transitionsForOptimization.ForEach(x => notOptimizedTransitions.Remove(x));
				}
			}
			return foundNew;
		}

		public bool DeriveNextExpression()
		{
			if (notDerivedIds.Count == 0)
				return false;

			var currentId = notDerivedIds[0];
			var current = equivalentStatesGroups[currentId].Value[0]; // .First(x => x.Key == currentId)

			foreach (var letter in current.Alphabet)
			{
				var derived = current.Derive(letter);

				if (derived == null)
					continue;

				notLabeled.Add(derived);

				notOptimizedTransitions.Add(new Tuple<int, string, RegularExpression>(currentId, letter, derived));

				//if (states.Any(x => x.IsEqual(current)))
				//	continue;
			}

			notDerivedIds.RemoveAt(0);
			return true;
		}

		//private void FindTransitions()
		//{
		//	notDerived.Add(input);
		//	while (notDerived.Count > 0)
		//	{
		//		var current = notDerived[0];
		//		notDerived.RemoveAt(0);
		//		if (states.Any(x => x.IsEquivalent(current)))
		//			continue;

		//		states.Add(current);
		//		List<RegularExpression> newDerivations = new List<RegularExpression>();
		//		foreach (var letter in current.Alphabet)
		//		{
		//			var derived = current.Derive(letter);
		//			if (derived == null)
		//				continue;
		//			int stateIndex = states.FindIndex(x => x.Equals(derived));
		//			if (stateIndex < 0)
		//				stateIndex = states.FindIndex(x => x.IsEquivalent(derived));
		//			if (stateIndex >= 0)
		//			{
		//				derived = states[stateIndex];
		//			}
		//			else
		//				newDerivations.Add(derived);
		//			int transitionIndex = transitions.FindIndex(x => x.Item1.Equals(current) && x.Item3.Equals(derived));
		//			if (transitionIndex >= 0)
		//			{
		//				transitions[transitionIndex].Item2.Add(letter);
		//				//string letters = new StringBuilder().Append(transitions[transitionIndex].Item2)
		//				//	.Append(", ").Append(letter).ToString();
		//				//transitions[transitionIndex]
		//				//	= new MachineTransition(current, letters, derived);
		//			}
		//			else
		//				transitions.Add(new MachineTransition(current, letter, derived));
		//		}
		//		notDerived.AddRange(newDerivations);
		//	}
		//}

		//private void FindFinalStates()
		//{
		//	finalStates = new List<RegularExpression>();

		//	foreach (var state in states)
		//	{
		//		if (state.GeneratesEmptyWord())
		//			finalStates.Add(state);
		//	}
		//}

	}
}
