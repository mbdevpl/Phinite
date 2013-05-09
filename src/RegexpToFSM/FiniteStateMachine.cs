using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Phinite
{
	/// <summary>
	/// Stores all information about a finite-state machine: states and transitions, also about
	/// initial and accepting states.
	/// </summary>
	public class FiniteStateMachine
	{
		#region auto-resolver constants

		/// <summary>
		/// Minimum number of steps taken by auto-resolver.
		/// </summary>
		public static readonly int MinimumSimilaritiesRefinementSteps = 4;

		/// <summary>
		/// Must be between 0 and 1.
		/// </summary>
		private static readonly double NeutralSimilarity = 0.5;

		/// <summary>
		/// Similarity must be at least at this level in order for two states to be considered as equivalent.
		/// </summary>
		public static readonly double SimilarityThresholdToInferEquivalence = 0.95;

		private static readonly double SimilarityPenaltyForStatesCount = 0.25;

		private static readonly double SimilarityPenaltyForTransitionsCount = 0.2;

		private static readonly double SimilarityPenaltyForTransitions = 0.15;

		#endregion

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
		/// The expression that should be labeled in the next step of construction,
		/// it is null if there are no expressions to label.
		/// </summary>
		public RegularExpression NextNotLabeledState
		{ get { return (notLabeled == null || notLabeled.Count == 0) ? null : notLabeled[0]; } }

		/// <summary>
		/// Estimates probabilities of all labeled states being equivalent
		/// to the first of not labeled states.
		/// </summary>
		public ReadOnlyCollection<double> NextNotLabeledStateSimilarities
		{ get { return nextNotLabeledStateSimilarities == null ? null : new ReadOnlyCollection<double>(nextNotLabeledStateSimilarities); } }
		private Double[] nextNotLabeledStateSimilarities;

		/// <summary>
		/// Number of states that cannot be displayed yet.
		/// </summary>
		public int RemainingStatesCount { get { return notLabeled == null ? 0 : notLabeled.Count; } }

		/// <summary>
		/// List of all expressions that need to be derived.
		/// </summary>
		private List<int> notDerivedIds;

		/// <summary>
		/// A read-only collection of states of this machine.
		/// </summary>
		public ReadOnlyCollection<RegularExpression> States
		{
			get
			{
				if (equivalentStatesGroups == null)
					return null;

				var states = new List<RegularExpression>();
				foreach (var stateGroup in equivalentStatesGroups)
				{
					if (stateGroup.Value.Count == 0)
						states.Add(null);
					states.Add(stateGroup.Value[0]);
				}
				return new ReadOnlyCollection<RegularExpression>(states);
			}
		}

		/// <summary>
		/// Collection of states added since last reading of this property,
		/// useful only during construction phase.
		/// </summary>
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

		/// <summary>
		/// Current total count of labeled states.
		/// </summary>
		public int LabeledStatesCount { get { return equivalentStatesGroups.Count; } }

		/// <summary>
		/// Transitions that are not attached to labeled states, because at least one of the states
		/// that they belong to is not yet labeled.
		/// </summary>
		private List<Tuple<int, string, RegularExpression>> notOptimizedTransitions;

		/// <summary>
		/// Number of transactions that cannot be displayed yet.
		/// </summary>
		public int RemainingTransitionsCount
		{
			get
			{
				return (notDerivedIds == null ? 0 : notDerivedIds.Count)
					+ (notOptimizedTransitions == null ? 0 : notOptimizedTransitions.Count);
			}
		}

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

		/// <summary>
		/// Current total count of labeled transitions.
		/// </summary>
		public int LabeledTransitionsCount { get { return transitions.Count; } }

		/// <summary>
		/// Collection of transitions added since last reading of this property,
		/// useful only during construction phase.
		/// </summary>
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
					return null;

				var acceptingStates = new List<RegularExpression>();
				foreach (var id in acceptingStatesIds)
				{
					var stateGroup = equivalentStatesGroups[id].Value;
					if (stateGroup.Count == 0)
						acceptingStates.Add(null);
					acceptingStates.Add(stateGroup[0]);
				}
				return new ReadOnlyCollection<RegularExpression>(acceptingStates);
			}
		}
		private List<int> acceptingStatesIds;

		/// <summary>
		/// Number of steps forward that auto-resolver takes to see if given states are equivalent.
		/// </summary>
		private int similaritiesRefinementSteps;

		/// <summary>
		/// Current state of fsm, valid only during word evaluation phase.
		/// </summary>
		public int CurrentState { get { return currentState; } }
		private int currentState;

		/// <summary>
		/// Previous state of fsm, valid only during word evaluation phase.
		/// </summary>
		public int PreviousState { get { return previousState; } }
		private int previousState;

		/// <summary>
		/// Whole evaluated input word, valid only during word evaluation phase.
		/// </summary>
		public string EvaluatedWordInput { get { return evaluatedWordInput; } }
		private string evaluatedWordInput;

		/// <summary>
		/// Processed part of input word, valid only during word evaluation phase.
		/// </summary>
		public string EvaluatedWordProcessedFragment { get { return evaluatedWordProcessedFragment; } }
		private string evaluatedWordProcessedFragment;

		/// <summary>
		/// Part of input word that was not processed yet, valid only during word evaluation phase.
		/// </summary>
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
				Construct(0, false);

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

			similaritiesRefinementSteps = FindOptimalNumberOfRefinementSteps();
		}

		private int FindOptimalNumberOfRefinementSteps()
		{
			var reducedInput = input.ParseTree;
			reducedInput.Reduce();

			if (reducedInput.GeneratesEmptyWord() || reducedInput.Role == PartialExpressionRole.Letter)
				return MinimumSimilaritiesRefinementSteps;

			var parts = reducedInput.Parts;
			var distinctParts = new List<PartialExpression>();
			distinctParts.Add(parts[0]);

			for (int i = 1; i < parts.Count; ++i)
			{
				if (distinctParts.Any(x => x.Value.Equals(parts[i].Value)))
					continue;
				distinctParts.Add(parts[i]);
			}

			if (distinctParts.Count == 1)
				return Math.Max(MinimumSimilaritiesRefinementSteps, parts.Count);

			var repetitions = new int[distinctParts.Count];
			Parallel.For(0, distinctParts.Count, (int n) =>
			{
				repetitions[n] = parts.Count(x => x.Value.Equals(distinctParts[n].Value));
			});
			return Math.Max(MinimumSimilaritiesRefinementSteps, repetitions.Max());
		}

		/// <summary>
		/// The method performs at most chosen number of steps of finite-state machine construction.
		/// 
		/// The method may end prematurely when the machine is already constructed
		/// or if an uncertain case arises (the latter only if breakIfUncertain is set to true).
		/// </summary>
		/// <param name="numberOfSteps">maximum number of steps that will be taken,
		/// when set to zero or a negative number, the construction is performed until it has completed
		/// or interrupted because of uncertainty</param>
		/// <param name="breakIfUncertain">if false, the algorithm delegates all uncertain cases
		/// to auto-resolver, and always finishes after designated number of steps
		/// or when machine construction is complete</param>
		/// <returns>false if construction was interrupted, true otherwise</returns>
		public bool Construct(int numberOfSteps, bool breakIfUncertain)
		{
			if (numberOfSteps <= 0)
				numberOfSteps = -1;
			while (numberOfSteps != 0 && !IsConstructionFinished())
			{
				if (notLabeled.Count > 0 && !LabelNextExpression())
				{
					// automatically handle some uncertain cases
					if (breakIfUncertain)
					{
						return false;
					}
					else if (nextNotLabeledStateSimilarities != null)
					{
						RefineSimilarities();

						// analyze refined similarities
						int iMax = -1;
						for (int i = 0; i < nextNotLabeledStateSimilarities.Length; ++i)
						{
							if (nextNotLabeledStateSimilarities[i] < SimilarityThresholdToInferEquivalence)
								continue;

							if (iMax == -1 || nextNotLabeledStateSimilarities[iMax] < nextNotLabeledStateSimilarities[i])
								iMax = i;
						}

						// label expression accordingly
						nextNotLabeledStateSimilarities = null;
						if (iMax == -1)
							ManuallyLabelNextExpression(null);
						else
							ManuallyLabelNextExpression(equivalentStatesGroups[iMax].Value[0]);
					}
					else
						throw new NotImplementedException("labeling failed to auto-resolve uncertainty");
				}
				if (notDerivedIds.Count > 0 && !DeriveNextExpression())
				{
					// automatically handle uncertain cases
					throw new NotImplementedException("derivation failed while there is something to derive");
				}
				if (numberOfSteps > 0)
					--numberOfSteps;
			}
			return true;
		}

		/// <summary>
		/// The method performs at most chosen number of steps of finite-state machine construction.
		/// 
		/// The method may end prematurely when the machine is already constructed.
		/// </summary>
		/// <param name="numberOfSteps">maximum number of steps that will be taken,
		/// when set to zero or a negative number, the construction is performed until it has completed</param>
		/// <param name="equivalentToExpressionIfUncertain">algoritm will treat all uncertain cases of not labeled states
		/// as equivalent to given expression, or as new if this argument is null</param>
		/// <param name="assumeNotLabeled">if true, algorithm will not even try to label new states, it will immediately
		/// treat them as equivalent to given value of equivalentToExpressionIfUncertain parameter</param>
		/// <returns></returns>
		public bool Construct(int numberOfSteps, RegularExpression equivalentToExpressionIfUncertain, bool assumeNotLabeled)
		{
			if (numberOfSteps <= 0)
				numberOfSteps = -1;
			while (numberOfSteps != 0 && !IsConstructionFinished())
			{
				if (assumeNotLabeled || (notLabeled.Count > 0 && !LabelNextExpression()))
				{
					// automatically handle some uncertain cases
					nextNotLabeledStateSimilarities = null;
					ManuallyLabelNextExpression(equivalentToExpressionIfUncertain);
				}
				if (notDerivedIds.Count > 0 && !DeriveNextExpression())
				{
					// automatically handle uncertain cases
					throw new NotImplementedException("derivation failed while there is something to derive");
				}
				if (numberOfSteps > 0)
					--numberOfSteps;
			}
			return true;
		}

		/// <summary>
		/// Initially calculated probability values for similarities (public field NextNotLabeledStateSimilarities)
		/// are refined because each case is examined more thoroughly, which gives more precise results,
		/// but is very expensive in terms of computation time.
		/// 
		/// Auto-resolver launches this method automatically to ensure good approximation
		/// of equivalence estimation.
		/// </summary>
		public void RefineSimilarities()
		{
			if (nextNotLabeledStateSimilarities == null)
				return;

			FiniteStateMachine machine1 = new FiniteStateMachine(notLabeled[0], false);
			machine1.Construct(similaritiesRefinementSteps, null, false);

			// refine each similarity that can theoretically yield an equivalent state
			Parallel.For(0, nextNotLabeledStateSimilarities.Length, (int n) =>
				{
					if (nextNotLabeledStateSimilarities[n] < NeutralSimilarity)
						return; // it is unlikely the equivalent expression

					if (equivalentStatesGroups[n].Value.Count > 1)
					{
						double[] localSimilarities = new double[equivalentStatesGroups[n].Value.Count];
						Parallel.For(0, equivalentStatesGroups[n].Value.Count, (int k) =>
							{
								localSimilarities[k] = nextNotLabeledStateSimilarities[n];

								FiniteStateMachine machine2 = new FiniteStateMachine(equivalentStatesGroups[n].Value[k], false);
								machine2.Construct(similaritiesRefinementSteps, null, false);

								while (machine2.equivalentStatesGroups.Count < similaritiesRefinementSteps
									&& machine2.notLabeled.Count > 0)
									machine2.Construct(1, null, false);

								if (machine1.equivalentStatesGroups.Count != machine2.equivalentStatesGroups.Count)
									localSimilarities[k] -= SimilarityPenaltyForStatesCount;

								if (machine1.transitions.Count != machine2.transitions.Count)
									localSimilarities[k] -= SimilarityPenaltyForTransitionsCount;

								if (localSimilarities[k] < NeutralSimilarity)
									return;

								double ratio = NeutralSimilarity / machine1.transitions.Count;

								for (int i = 0; i < machine1.transitions.Count; ++i)
								{
									var t1 = machine1.transitions[i];
									var t2 = machine2.transitions[i];

									if (t1.InitialStateId == t2.InitialStateId && t1.ResultingStateId == t2.ResultingStateId
										&& t1.Letters.Count == t2.Letters.Count
										&& machine1.equivalentStatesGroups[t1.InitialStateId].Value[0]
											.Similarity(machine2.equivalentStatesGroups[t2.InitialStateId].Value[0]) >= NeutralSimilarity
										)
										localSimilarities[k] += ratio;
									else
									{
										localSimilarities[k] = NeutralSimilarity - SimilarityPenaltyForTransitions;
										return;
									}
								}
							});

						nextNotLabeledStateSimilarities[n] = localSimilarities.Min();
					}
					else
					{
						FiniteStateMachine machine2 = new FiniteStateMachine(equivalentStatesGroups[n].Value[0], false);
						machine2.Construct(similaritiesRefinementSteps, null, false);

						while (machine2.equivalentStatesGroups.Count < similaritiesRefinementSteps
							&& machine2.notLabeled.Count > 0)
							machine2.Construct(1, null, false);

						if (machine1.equivalentStatesGroups.Count != machine2.equivalentStatesGroups.Count)
							nextNotLabeledStateSimilarities[n] -= SimilarityPenaltyForStatesCount;

						if (machine1.transitions.Count != machine2.transitions.Count)
							nextNotLabeledStateSimilarities[n] -= SimilarityPenaltyForTransitionsCount;

						if (nextNotLabeledStateSimilarities[n] < NeutralSimilarity)
							return;

						double ratio = NeutralSimilarity / machine1.transitions.Count;

						for (int i = 0; i < machine1.transitions.Count; ++i)
						{
							var t1 = machine1.transitions[i];
							var t2 = machine2.transitions[i];

							if (t1.InitialStateId == t2.InitialStateId && t1.ResultingStateId == t2.ResultingStateId
								&& t1.Letters.Count == t2.Letters.Count
								&& t1.Letters.SequenceEqual(t2.Letters)
								&& machine1.equivalentStatesGroups[t1.InitialStateId].Value[0]
									.Similarity(machine2.equivalentStatesGroups[t2.InitialStateId].Value[0]) >= NeutralSimilarity
								)
								nextNotLabeledStateSimilarities[n] += ratio;
							else
							{
								nextNotLabeledStateSimilarities[n] = NeutralSimilarity - SimilarityPenaltyForTransitions;
								return;
							}
						}
					}

				});
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
		/// <returns>true if an expression was labeled during execution of this method,
		/// or if an expression was evaluated and it was determined with 100% certainty
		/// that it is equivalent to some already labeled expression</returns>
		private bool LabelNextExpression()
		{
			if (notLabeled.Count == 0)
				return false;

			var labeled = notLabeled[0];
			var foundNew = false;

			if (initialState == null)
				initialState = labeled; // first state becomes the initial state

			int count = equivalentStatesGroups.Count;

			if (count == 0)
			{
				foundNew = true;
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
				// this looks like a new state, but it may as well be still equivalent of one existing state
				// because equivalence checking is not perfect

				double[] similarities = new double[count];
				bool foundUncertainty = false;
				if (count > 1)
				{
					Parallel.For(0, count, (int n) =>
					{
						similarities[n] = notLabeled[0].Similarity(equivalentStatesGroups[n].Value[0]);
					});
					foundUncertainty = similarities.Max() > 0;
				}
				else
				{
					// count == 1
					similarities[0] = notLabeled[0].Similarity(equivalentStatesGroups[0].Value[0]);
					foundUncertainty = similarities[0] > 0;
				}

				if (foundUncertainty)
				{
					nextNotLabeledStateSimilarities = similarities;
					return false;
				}

				nextNotLabeledStateSimilarities = null;
				foundNew = true;
			}

			if (foundNew)
			{
				ManuallyLabelNextExpression(null);
			}
			return foundNew;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="equivalentToExpression"></param>
		private void ManuallyLabelNextExpression(RegularExpression equivalentToExpression)
		{
			RegularExpression labeled = notLabeled[0];
			int labeledId = -1;

			if (equivalentToExpression == null)
			{
				labeledId = equivalentStatesGroups.Count;
				notLabeled.RemoveAt(0);
				equivalentStatesGroups.Add(new KeyValuePair<int, List<RegularExpression>>(labeledId, new List<RegularExpression>()));
				equivalentStatesGroups[labeledId].Value.Add(labeled);

				latestStates.Add(labeled);

				if (labeled.GeneratesEmptyWord())
					acceptingStatesIds.Add(labeledId);

				notDerivedIds.Add(labeledId);
			}
			else
			{
				foreach (var group in equivalentStatesGroups)
				{
					if (!group.Value.Any(x => x.Equals(equivalentToExpression)))
						continue;
					labeledId = group.Key;
					break;
				}

				if (labeledId < 0 && labeledId >= equivalentStatesGroups.Count)
					throw new ArgumentException("must be either null or an expression that is already labeled", "equivalentToExpression");

				equivalentStatesGroups[labeledId].Value.Add(labeled);
			}

			if (notOptimizedTransitions == null || notOptimizedTransitions.Count == 0)
				return;

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

		/// <summary>
		/// Derives next expression from the not-derived-expressions queue.
		/// </summary>
		/// <returns>true if any expression was derived during execution of this method</returns>
		private bool DeriveNextExpression()
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
				if (count > 1)
					Parallel.For(0, count, (int n) =>
					{
						derivations[n] = current.Derive(alphabet[n]);
					});
				else
					derivations[0] = current.Derive(alphabet[0]);

				// analyze results in sequence
				for (int i = 0; i < count; ++i)
				{
					var derived = derivations[i];

					if (derived == null)
						continue;

					var letter = alphabet[i];

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
		/// <param name="numberOfSteps">if zero, the evaluation continues until it is complete</param>
		public void Evaluate(int numberOfSteps)
		{
			if (!IsConstructionFinished())
				throw new InvalidOperationException("cannot evaluate word when the fsm is not yet completely constructed");

			if (numberOfSteps <= 0)
				numberOfSteps = -1;
			while (numberOfSteps != 0 && !IsEvaluationFinished())
			{
				// remove single letter
				var letter = evaluatedWordRemainingFragment[0].ToString();

				MachineTransition transition = transitions.FirstOrNull(
					x => x.InitialStateId == currentState && x.ContainsLetter(letter));
				//try
				//{
				//	transition = transitions.First(x => x.InitialStateId == currentState && x.ContainsLetter(letter));
				//}
				//catch (InvalidOperationException)
				//{
				//	// silent catch, matching transition was not found
				//}

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

		/// <summary>
		/// Converts this object into a single line of text that describes its current state.
		/// </summary>
		/// <returns>a description of current state of this instance</returns>
		public override string ToString()
		{
			return String.Format("States:{0} Accepting:{1} Transitions:{2} notLabeled:{3} notDerived:{4} CurrentState:{5}",
				equivalentStatesGroups.Count, acceptingStatesIds.Count, transitions.Count,
				notLabeled.Count, notDerivedIds.Count,
				currentState);
		}

	}
}
