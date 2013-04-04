using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;

using QuickGraph;
using QuickGraph.Algorithms;
//using GraphSharp.Algorithms.Layout.Simple.Tree;
//using GraphSharp.Algorithms.Layout.Simple.FDP;
using GraphSharp.Algorithms.Layout.Compound;
using GraphSharp.Algorithms.Layout.Compound.FDP;
using System.Threading;
using System.Text;

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
				if (transitions == null)
					//return new ReadOnlyCollection<MachineTransition>(new List<MachineTransition>());
					return null;

				return new ReadOnlyCollection<MachineTransition>(transitions);
			}
		}
		private List<MachineTransition> transitions;

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
						throw new ArgumentException("an equivalent state group must have at least member state");
					// TODO: use state with shortest string representation instead of the 1st state
					acceptingStates.Add(stateGroup[0]);
				}
				return new ReadOnlyCollection<RegularExpression>(acceptingStates);
			}
		}
		private List<int> acceptingStatesIds;

		public int CurrentState { get { return currentState; } }
		private int currentState;

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

		private void InitializeConstruction()
		{
			notLabeled = new List<RegularExpression>();
			notLabeled.Add(input);

			//notDerived = new List<RegularExpression>();
			notDerivedIds = new List<int>();

			//states = new List<RegularExpression>();
			equivalentStatesGroups = new List<KeyValuePair<int, List<RegularExpression>>>();

			notOptimizedTransitions = new List<Tuple<int, string, RegularExpression>>();

			transitions = new List<MachineTransition>();

			acceptingStatesIds = new List<int>();
		}

		public bool IsConstructionFinished()
		{
			return notLabeled.Count == 0 && notDerivedIds.Count == 0 && notOptimizedTransitions.Count == 0;
		}

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
						//foundNew = false;
						//break;
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
							transitions.Add(new MachineTransition(x.Item1, x.Item2, labeledId));
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

				var derivedId = equivalentStatesGroups.FindIndex(x => x.Value.Any(y => y.Equals(derived)));

				if (derivedId >= 0)
				{
					var foundTransition = transitions.FindIndex(x => x.Item1 == currentId && x.Item3 == derivedId);
					if (foundTransition >= 0)
						transitions[foundTransition].AddLetter(letter);
					else
						transitions.Add(new MachineTransition(currentId, letter, derivedId));
				}
				else
				{
					notLabeled.Add(derived);
					notOptimizedTransitions.Add(new Tuple<int, string, RegularExpression>(currentId, letter, derived));
				}

				//if (states.Any(x => x.IsEqual(current)))
				//	continue;
			}

			notDerivedIds.RemoveAt(0);
			return true;
		}

		private void InitializeEvaluation()
		{
			currentState = -1;
			evaluatedWordInput = null;
			evaluatedWordRemainingFragment = null;
		}

		public void BeginEvaluation(string word)
		{
			if (!IsConstructionFinished())
				throw new InvalidOperationException("cannot evaluate word when the fsm is not yet completely constructed");

			if (!IsEvaluationFinished())
			{
				// overwrite old eval variables
			}

			currentState = 0;
			evaluatedWordInput = word;
			evaluatedWordProcessedFragment = String.Empty;
			evaluatedWordRemainingFragment = word;
		}

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
					// silent catch
				}

				evaluatedWordProcessedFragment = new StringBuilder(evaluatedWordProcessedFragment)
					.Append(letter).ToString();

				evaluatedWordRemainingFragment = evaluatedWordRemainingFragment.Substring(1);

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

		public bool IsAccepting(int stateIndex)
		{
			return acceptingStatesIds.Any(x => x == stateIndex);
		}

		private Dictionary<int, double> GetStatesTooCloseToEdge(Dictionary<int, Point> layout, int stateId1, int stateId2, double threshold)
		{
			if (stateId1 == stateId2)
				return null;

			if (!transitions.Any(x => x.Item1 == stateId1 && x.Item3 == stateId2))
				return null;

			var p1 = layout[stateId1];
			var p2 = layout[stateId2];

			Dictionary<int, double> results = null;
			foreach (var key in layout.Keys)
			{
				if (key == stateId1 || key == stateId2)
					continue;

				double dist = layout[key].DistanceToLine(p1, p2);

				if (dist < threshold)
				{
					if (results == null)
						results = new Dictionary<int, double>();
					results.Add(key, dist);
				}
			}
			return results;
		}

		/// <summary>
		/// Returns a score of a given layout.
		/// 
		/// This score is relative, and unless it is zero (perfect score),
		/// there needs to be another layout for comparison in order for any conclusions to be drawn.
		/// Zero means that if the machine is drawn using this layout, it will be very easy for human eye
		/// to analyse it, and that humans in generally will think of it as a clear and understandable drawing.
		/// For example, you can receive -1000, and think this is bad, but new layout may receive -10^6.
		/// Also if you receive -0.001 yo may think this is good, but new layout can still receive -10^-6.
		/// 
		/// Score is based on number of intersecting edges, number of nodes that lay on some edge,
		/// number of nodes that are very close to each other, etc.
		/// </summary>
		/// <returns>zero if layout is perfect, negative value if it is not</returns>
		private double CalculateLayoutScore(Dictionary<int, Point> layout)
		{
			if (layout == null || layout.Count <= 1)
				return 0;

			double nodesDistanceScore = 0;
			double nodesOnEdgesScore = 0;
			double edgeIntersectionsScore = 0;

			// check if point are too close to each other
			foreach (var key1 in layout.Keys)
				foreach (var key2 in layout.Keys)
				{
					if (key1 == key2)
						continue;

					var p1 = layout[key1];
					var p2 = layout[key2];

					double dist = p1.Distance(p2);
					if (dist < 100)
						nodesDistanceScore -= Math.Sqrt(100 - dist) + 1;
				}

			// check if edges between connected points are obstructed by any state
			foreach (var key1 in layout.Keys)
				foreach (var key2 in layout.Keys)
				{
					if (key1 == key2)
						continue;
					if (!transitions.Any(x => x.Item1 == key1 && x.Item3 == key2))
						continue;

					var p1 = layout[key1];
					var p2 = layout[key2];

					foreach (var key in layout.Keys)
					{
						if (key == key1 || key == key2)
							continue;

						double dist = layout[key].DistanceToLine(p1, p2);

						if (dist < 30)
							nodesOnEdgesScore -= Math.Sqrt(30 - dist) * 2 + 1;
					}

				}

			// check if any edges intersect
			foreach (var key1 in layout.Keys)
				foreach (var key2 in layout.Keys)
				{
					if (key1 == key2)
						continue;
					if (!transitions.Any(x => x.Item1 == key1 && x.Item3 == key2))
						continue;

					var p1 = layout[key1];
					var p2 = layout[key2];
				}

			return nodesDistanceScore + nodesOnEdgesScore + edgeIntersectionsScore;
		}

		public Dictionary<int, Point> LayOut()
		{
			var layouts = new List<KeyValuePair<Dictionary<int, Point>, double>>();
			int bestLayout = 0;
			//layout = new Dictionary<int, Point>();

			//Random rand = new Random();

			//double x = 20, y = 100;
			//foreach (var stateGroup in equivalentStatesGroups)
			//{
			//	layout.Add(stateGroup.Key, new Point(x, y));

			//	var list = transitions.FindAll(t => t.Item3 == stateGroup.Key && t.Item1 != t.Item3 && t.Item1 < stateGroup.Key);
			//	foreach (var elem in list)
			//	{
			//		var bad = GetStatesTooCloseToEdge(layout, elem.Item1, stateGroup.Key, 30);
			//		if (bad == null)
			//			continue;
			//		layout[stateGroup.Key] = new Point(x, y + 100 * (rand.Next() % 2 == 0 ? 1 : -1));
			//		break;
			//	}

			//	//list = transitions.FindAll(t => t.Item1 != t.Item3 && t.Item1 == stateGroup.Key);
			//	//foreach (var elem in list)
			//	//{
			//	//	var bad = GetStatesTooCloseToEdge(layout, stateGroup.Key, elem.Item1, 30);
			//	//	if (bad == null)
			//	//		continue;
			//	//	layout[stateGroup.Key] = new Point(x, y - 50);
			//	//	break;
			//	//}

			//	x += 100;
			//	y += 0;
			//}

			#region GraphSharp graph construction

			var graph = new BidirectionalGraph<string, Edge<string>>();
			string[] vertices = new string[equivalentStatesGroups.Count];
			//Dictionary<string, Point> vertexPositions = new Dictionary<string, Point>();
			Dictionary<string, Size> vertexSizes = new Dictionary<string, Size>();
			Dictionary<string, Thickness> vertexBorders = new Dictionary<string, Thickness>();

			//int i = 0;
			for (int i = 0; i < equivalentStatesGroups.Count; ++i)
			//foreach (var stateGroup in equivalentStatesGroups)
			{
				vertices[i] = i/*stateGroup.Key*/.ToString();
				//vertexPositions.Add(vertices[i], new Point(i * 100, i * 100));
				vertexSizes.Add(vertices[i], new Size(32, 32));
				vertexBorders.Add(vertices[i], new Thickness(50, 50, 50, 50));
				//i++;
			}
			graph.AddVertexRange(vertices);
			foreach (var transition in transitions)
				if (transition.Item1 != transition.Item3)
					graph.AddEdge(new Edge<string>(vertices[transition.Item1], vertices[transition.Item3]));

			#endregion

			//double lastScore = 1;
			for (int i = 0; i < 50; ++i)
			{
				#region running GraphSharp algorithm

				var algo4 = new CompoundFDPLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>>(
						graph, vertexSizes, vertexBorders, new Dictionary<string, CompoundVertexInnerLayoutType>());
				algo4.Compute();
				while (algo4.State != ComputationState.Finished)
					Thread.Sleep(250);

				#endregion

				var layout = new Dictionary<int, Point>();

				double minX = 1000, minY = 1000;
				foreach (var pos in algo4.VertexPositions)
				{
					var location = pos.Value;
					if (location.X < minX) minX = location.X;
					if (location.Y < minY) minY = location.Y;
					layout.Add(int.Parse(pos.Key), new Point(location.X, location.Y));
				}

				minX -= 60;
				minY -= 60;

				for (int key = 0; key < layout.Count; ++key)
				{
					layout[key] = new Point(layout[key].X - minX, layout[key].Y - minY);
				}

				double score = CalculateLayoutScore(layout);
				layouts.Add(new KeyValuePair<Dictionary<int, Point>, double>(layout, score));

				if (score == 0)
					return layouts[layouts.Count - 1].Key;

				if (score > layouts[bestLayout].Value)
					bestLayout = layouts.Count - 1;

				//if (lastScore > 0)
				//	lastScore = score;
			}


			return layouts[bestLayout].Key;

			//var results = new Dictionary<RegularExpression, Point>();
			//foreach (var pair in dictionary)
			//	results.Add(equivalentStatesGroups[pair.Key].Value.First(), pair.Value);
			//return results;
		}

	}
}
