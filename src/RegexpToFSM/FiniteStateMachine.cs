using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;

//using QuickGraph;
//using QuickGraph.Algorithms;
//using GraphSharp.Algorithms.Layout.Simple.Tree;
//using GraphSharp.Algorithms.Layout.Simple.FDP;
//using GraphSharp.Algorithms.Layout.Compound;
//using GraphSharp.Algorithms.Layout.Compound.FDP;
using System.Threading;
using System.Text;
using System.Threading.Tasks;

namespace Phinite
{
	/// <summary>
	/// Stores all information about a finite-state machine: states and transitions, also about
	/// initial and accepting states.
	/// </summary>
	public class FiniteStateMachine
	{
		/// <summary>
		/// Original input expression that was used to generate this machine.
		/// </summary>
		public RegularExpression Input { get { return input; } }
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
				if (equivalentStatesGroups == null)
					//return new ReadOnlyCollection<RegularExpression>(new List<RegularExpression>());
					return null;
				var states = new List<RegularExpression>();
				foreach (var stateGroup in equivalentStatesGroups)
				{
					if (stateGroup.Value.Count == 0)
						states.Add(null); //throw new ArgumentException("an equivalent state group must have at least member state");
					// TODO: use state with shortest string representation instead of the 1st state
					states.Add(stateGroup.Value[0]);
				}
				return new ReadOnlyCollection<RegularExpression>(states);
			}
		}

		public ReadOnlyCollection<RegularExpression> LatestStates
		{
			get
			{
				if (latestStates == null)
					return null;
				var array = latestStates.ToArray();
				latestStates.Clear();
				return new ReadOnlyCollection<RegularExpression>(array);
			}
		}
		private List<RegularExpression> latestStates;

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
				if (transitions == null)
					return null;
				return new ReadOnlyCollection<MachineTransition>(transitions.ToArray());
			}
		}
		private List<MachineTransition> transitions;

		public ReadOnlyCollection<MachineTransition> LatestTransitions
		{
			get
			{
				if (latestTransitions == null)
					return null;
				var array = latestTransitions.ToArray();
				latestTransitions.Clear();
				return new ReadOnlyCollection<MachineTransition>(array);
			}
		}
		private List<MachineTransition> latestTransitions;

		/// <summary>
		/// A read-only collection of accepting states of the machine.
		/// </summary>
		public ReadOnlyCollection<RegularExpression> AcceptingStates
		{
			get
			{
				if (acceptingStatesIds == null)
					//return new ReadOnlyCollection<RegularExpression>(new List<RegularExpression>());
					return null;

				var acceptingStates = new List<RegularExpression>();
				foreach (var id in acceptingStatesIds)
				{
					var stateGroup = equivalentStatesGroups[id].Value;
					if (stateGroup.Count == 0)
						acceptingStates.Add(null); //throw new ArgumentException("an equivalent state group must have at least member state");
					// TODO: use state with shortest string representation instead of the 1st state
					acceptingStates.Add(stateGroup[0]);
				}
				return new ReadOnlyCollection<RegularExpression>(acceptingStates);
			}
		}
		private List<int> acceptingStatesIds;

		public int CurrentState { get { return currentState; } }
		private int currentState;

		public int PreviousState { get { return previousState; } }
		private int previousState;

		public string EvaluatedWordInput { get { return evaluatedWordInput; } }
		private string evaluatedWordInput;

		public string EvaluatedWordProcessedFragment { get { return evaluatedWordProcessedFragment; } }
		private string evaluatedWordProcessedFragment;

		public string EvaluatedWordRemainingFragment { get { return evaluatedWordRemainingFragment; } }
		private string evaluatedWordRemainingFragment;

		/// <summary>
		/// Creates a new finite-state machine from a given regular expression.
		/// </summary>
		/// <param name="input">any regular expression</param>
		/// <param name="constructImmediately">if false, the machine will be constructed
		/// only after the method Construct() is invoked</param>
		public FiniteStateMachine(RegularExpression input, bool constructImmediately = false)
		{
			this.input = input;

			InitializeConstruction();
			if (constructImmediately)
				Construct(0);

			InitializeEvaluation();
		}

		private void InitializeConstruction()
		{
			notLabeled = new List<RegularExpression>();
			notLabeled.Add(input);

			notDerivedIds = new List<int>();

			latestStates = new List<RegularExpression>();
			latestTransitions = new List<MachineTransition>();

			equivalentStatesGroups = new List<KeyValuePair<int, List<RegularExpression>>>();

			notOptimizedTransitions = new List<Tuple<int, string, RegularExpression>>();

			transitions = new List<MachineTransition>();

			acceptingStatesIds = new List<int>();
		}

		/// <summary>
		/// The method performs at most chosen number of steps of finite-state machine construction.
		/// 
		/// The method may end prematurely when the machine is already constructed.
		/// </summary>
		/// <param name="numberOfSteps">maximum number of steps that will be taken,
		/// when set to zero or a negative number, the construction is performed until it has completed</param>
		public void Construct(int numberOfSteps = 0)
		{
			if (numberOfSteps <= 0)
				numberOfSteps = -1;
			while (numberOfSteps != 0 && !IsConstructionFinished())
			{
				if (!LabelNextExpression() && notLabeled.Count > 0)
				{
					// automatically handle uncertain cases
				}
				if (!DeriveNextExpression() && notDerivedIds.Count > 0)
				{
					// automatically handle uncertain cases
				}
				if (numberOfSteps > 0)
					--numberOfSteps;
			}
		}

		/// <summary>
		/// Checks if this finite-state machine is fully constructed.
		/// </summary>
		/// <returns></returns>
		public bool IsConstructionFinished()
		{
			return notLabeled.Count == 0 && notDerivedIds.Count == 0 && notOptimizedTransitions.Count == 0;
		}

		/// <summary>
		/// Labels the next expression from the not-labeled-expressions queue.
		/// </summary>
		/// <param name="breakOnNotFound"></param>
		/// <returns>true if an expression was labeled during execution of this method,
		/// or if an expression was evaluated and it was determined with 100% certainty
		/// that it is equivalent to some already labeled expression</returns>
		public bool LabelNextExpression(bool breakOnNotFound = false)
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
					}
				}
				//TODO: handle the unsure situation here!
				if (breakOnNotFound)
				{
					// this looks like a new state, but it may as well be still equivalent of one existing state
					// because equivalence checking is not perfect
					return false;
				}

				foundNew = true;
			}

			if (foundNew)
			{
				labeledId = equivalentStatesGroups.Count;
				notLabeled.RemoveAt(0);
				equivalentStatesGroups.Add(new KeyValuePair<int, List<RegularExpression>>(labeledId, new List<RegularExpression>()));
				equivalentStatesGroups[labeledId].Value.Add(labeled);

				latestStates.Add(labeled);

				if (labeled.GeneratesEmptyWord())
					acceptingStatesIds.Add(labeledId);

				notDerivedIds.Add(labeledId);

				if (notOptimizedTransitions.Count > 0)
				{
					var transitionsForOptimization = notOptimizedTransitions.FindAll(x => x.Item3.Equals(labeled));

					transitionsForOptimization.ForEach(x =>
					{
						var foundTransition = transitions.FindIndex(y => y.Item1 == x.Item1 && y.Item3 == labeledId);
						if (foundTransition >= 0)
							// this can happen if more than one transition is optimized in this step
							transitions[foundTransition].AddLetter(x.Item2);
						else
						{
							MachineTransition transition = new MachineTransition(x.Item1, x.Item2, labeledId);
							transitions.Add(transition);
							latestTransitions.Add(transition);
						}
					});
					transitionsForOptimization.ForEach(x => notOptimizedTransitions.Remove(x));
				}
			}
			return foundNew;
		}

		public void ManuallyLabelNextExpression(RegularExpression equivalentToExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Derives next expression from the not-derived-expressions queue.
		/// </summary>
		/// <returns>true if any expression was derived during execution of this method</returns>
		public bool DeriveNextExpression()
		{
			if (notDerivedIds.Count == 0)
				return false;

			var currentId = notDerivedIds[0];
			var current = equivalentStatesGroups[currentId].Value[0]; // .First(x => x.Key == currentId)

			var alphabet = current.Alphabet;
			int count = alphabet.Count;

			if (count > 0)
			{
				Thread[] derivationThreads = new Thread[count];
				RegularExpression[] derivations = new RegularExpression[count];

				#region obsolete manual parallelization
				/****

				int processorsCount = count; // TODO: get total number of logical processing units
				Semaphore semaphore = new Semaphore(processorsCount, processorsCount);

				// derive in parallel
				for (int i = 0; i < count; ++i)
				{
					Thread t = new Thread(new ParameterizedThreadStart((object obj) =>
					{
						int n = (int)obj;
						semaphore.WaitOne();
						derivations[n] = current.Derive(alphabet[n]);
						semaphore.Release();
					}));
					t.Name = "DerivationThread";
					t.Priority = ThreadPriority.Lowest;
					t.SetApartmentState(ApartmentState.STA);
					t.Start(i);
					derivationThreads[i] = t;
				}

				// sync
				for (int i = 0; i < count; ++i)
					derivationThreads[i].Join();

				****/
				
				#endregion

				// derive in parallel
				Parallel.For(0, count, (int n) =>
				{
					derivations[n] = current.Derive(alphabet[n]);
				});

				// analyze results in sequence
				for (int i = 0; i < count; ++i)
				{
					var letter = alphabet[i];
					var derived = derivations[i];

					if (derived == null)
						continue;

					var derivedId = equivalentStatesGroups.FindIndex(x => x.Value.Any(y => y.Equals(derived)));

					if (derivedId >= 0)
					{
						var foundTransition = transitions.FindIndex(x => x.Item1 == currentId && x.Item3 == derivedId);
						if (foundTransition >= 0)
						{
							transitions[foundTransition].AddLetter(letter);
							latestTransitions.Add(transitions[foundTransition]);
						}
						else
						{
							var mt = new MachineTransition(currentId, letter, derivedId);
							transitions.Add(mt);
							latestTransitions.Add(mt);
						}
					}
					else
					{
						notLabeled.Add(derived);
						notOptimizedTransitions.Add(new Tuple<int, string, RegularExpression>(currentId, letter, derived));
					}
				}
			}

			//foreach (var letter in current.Alphabet)
			//{
			//	//if (states.Any(x => x.IsEqual(current)))
			//	//	continue;
			//}

			notDerivedIds.RemoveAt(0);
			return true;
		}

		private void InitializeEvaluation()
		{
			previousState = -1;
			currentState = -1;
			evaluatedWordInput = null;
			evaluatedWordRemainingFragment = null;
		}

		/// <summary>
		/// Begins evaluation of a given word on this machine.
		/// </summary>
		/// <param name="word"></param>
		public void BeginEvaluation(string word)
		{
			if (!IsConstructionFinished())
				throw new InvalidOperationException("cannot evaluate word when the fsm is not yet completely constructed");

			if (!IsEvaluationFinished())
			{
				// overwrite old eval variables
			}

			previousState = -1;
			currentState = 0;
			evaluatedWordInput = word;
			evaluatedWordProcessedFragment = String.Empty;
			evaluatedWordRemainingFragment = word;
		}

		/// <summary>
		/// Tries to perform at most given number of steps of word evaluation.
		/// </summary>
		/// <param name="numberOfSteps"></param>
		public void Evaluate(int numberOfSteps = 0)
		{
			if (!IsConstructionFinished())
				throw new InvalidOperationException("cannot evaluate word when the fsm is not yet completely constructed");

			if (numberOfSteps <= 0)
				numberOfSteps = -1;
			while (numberOfSteps != 0 && !IsEvaluationFinished())
			{
				// remove single letter
				var letter = evaluatedWordRemainingFragment[0].ToString();

				MachineTransition transition = null;
				try
				{
					transition = transitions.First(x => x.InitialStateId == currentState && x.ContainsLetter(letter));
				}
				catch (InvalidOperationException)
				{
					// silent catch, matching transition was not found
				}

				evaluatedWordProcessedFragment = new StringBuilder(evaluatedWordProcessedFragment)
					.Append(letter).ToString();

				evaluatedWordRemainingFragment = evaluatedWordRemainingFragment.Substring(1);

				previousState = currentState;
				if (transition == null)
				{
					currentState = -1;
					break;
				}
				currentState = transition.ResultingStateId;

				if (numberOfSteps > 0)
					--numberOfSteps;
			}
		}

		/// <summary>
		/// Checks if this machine is in accepting state and there is nothing left to evaluate,
		/// or it may be in rejecting state.
		/// </summary>
		/// <returns></returns>
		public bool IsEvaluationFinished()
		{
			if (!IsConstructionFinished())
				throw new InvalidOperationException("cannot check if evaluation finished, because fsm is not yet completely constructed");

			if (evaluatedWordInput == null)
				return true;

			if (evaluatedWordRemainingFragment == null)
				return true;

			return evaluatedWordRemainingFragment.Length == 0 || currentState < 0;
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

		/// <summary>
		/// Checks if a state with given index is an accepting one.
		/// </summary>
		/// <param name="stateIndex"></param>
		/// <returns>guess what: true if it is an accepting state and false otherwise</returns>
		public bool IsAccepting(int stateIndex)
		{
			return acceptingStatesIds.Any(x => x == stateIndex);
		}

		public override string ToString()
		{
			return String.Format("States:{0} Accepting:{1} Transitions:{2} notLabeled:{3} notDerived:{4} CurrentState:{5}",
				equivalentStatesGroups.Count, acceptingStatesIds.Count, transitions.Count,
				notLabeled.Count, notDerivedIds.Count,
				currentState);
		}

	}
}
