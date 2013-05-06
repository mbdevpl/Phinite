using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GraphSharp.Algorithms.Layout;
using GraphSharp.Algorithms.Layout.Compound;
using GraphSharp.Algorithms.Layout.Compound.FDP;
using GraphSharp.Algorithms.Layout.Contextual;
using GraphSharp.Algorithms.Layout.Simple.FDP;
using GraphSharp.Algorithms.Layout.Simple.Tree;
using GraphSharp.Algorithms.OverlapRemoval;
using Petzold.Media2D;
using QuickGraph;
using QuickGraph.Algorithms;

namespace Phinite
{
	public class FiniteStateMachineLayout
	{
		#region style settings

		private static readonly double TextBlockHeight = 18;

		public static readonly double StateEllipseDiameter = 32;

		public static readonly double MinimumStateDistance = 3 * StateEllipseDiameter;

		private static readonly double LoopHeight = StateEllipseDiameter * 4.0 / 3;

		private static readonly double LoopHalfWidth = StateEllipseDiameter * 1.0 / 3;

		private static readonly double LayoutOffset = StateEllipseDiameter / 2 + LoopHeight + TextBlockHeight / 2;

		private static readonly Brush VertexBrush = Brushes.Black;

		private static readonly Brush VertexBackgroundBrush = Brushes.White;

		private static readonly Brush VertexLabelBrush = Brushes.Black;

		private static readonly Brush HighlightedVertexBrush = Brushes.DarkSlateBlue;

		private static readonly Brush HighlightedVertexBackgroundBrush = Brushes.PowderBlue;

		private static readonly Brush HighlightedVertexLabelBrush = Brushes.Black;

		private static readonly Brush WordAcceptedStateBackgroundBrush = Brushes.LawnGreen;

		private static readonly Brush WordRejectedStateBackgroundBrush = Brushes.Orange;

		private static readonly Brush EdgeBrush = Brushes.Black;

		private static readonly Brush EdgeBorderBrush = Brushes.Transparent;

		private static readonly Brush EdgeLabelBrush = Brushes.Black;

		private static readonly Brush HighlightedEdgeBrush = Brushes.DarkSlateBlue;

		private static readonly Brush HighlightedEdgeBorderBrush = Brushes.PowderBlue;

		private static readonly Brush HighlightedEdgeLabelBrush = Brushes.Black;

		#endregion

		#region fsm and graph data

		private ReadOnlyCollection<RegularExpression> states;

		private ReadOnlyCollection<RegularExpression> acceptingStates;

		private Dictionary<int, Point> vertices;

		private ReadOnlyCollection<MachineTransition> transitions;

		private Dictionary<MachineTransition, Tuple<int, int>> edges;

		private FiniteStateMachineLayoutScore layoutScore;

		#endregion

		#region GraphSharp fields

		private BidirectionalGraph<string, Edge<string>> graph;

		private string[] vertexLabels;

		private IDictionary<string, Point> vertexPositions;

		private Dictionary<string, Size> vertexSizes;

		private Dictionary<string, Thickness> vertexBorders;

		#endregion

		public FiniteStateMachineLayout(FiniteStateMachine fsm)
		{
			states = fsm.States;
			acceptingStates = fsm.AcceptingStates;
			transitions = fsm.Transitions;

			#region GraphSharp graph construction

			graph = new BidirectionalGraph<string, Edge<string>>();
			vertexLabels = new string[states.Count];
			vertexPositions = new Dictionary<string, Point>();
			vertexSizes = new Dictionary<string, Size>();
			vertexBorders = new Dictionary<string, Thickness>();

			for (int i = 0; i < states.Count; ++i)
			{
				vertexLabels[i] = i.ToString();
				vertexPositions.Add(vertexLabels[i], new Point(i * MinimumStateDistance, i * MinimumStateDistance));
				vertexSizes.Add(vertexLabels[i], new Size(StateEllipseDiameter, StateEllipseDiameter));
				vertexBorders.Add(vertexLabels[i], new Thickness(MinimumStateDistance));
			}
			graph.AddVertexRange(vertexLabels);
			foreach (var transition in transitions)
				if (transition.InitialStateId != transition.ResultingStateId)
					graph.AddEdge(new Edge<string>(vertexLabels[transition.InitialStateId], vertexLabels[transition.ResultingStateId]));

			#endregion

		}

		public void Create()
		{
			int workGroupSize = 8;

			var layoutsAll = new List<KeyValuePair<Dictionary<int, Point>, FiniteStateMachineLayoutScore>>();
			int bestLayout = 0;

			double defaultDistanceRatio = MinimumStateDistance / 72;

			for (int i = 0; i < 32; ++i)
			{
				var layoutsScores = new FiniteStateMachineLayoutScore[workGroupSize];
				var layouts = new Dictionary<int, Point>[workGroupSize];

				try
				{
					// try several layouts in parallel
					Parallel.For(0, workGroupSize, (int n) =>
					{
						IDictionary<string, Point> createdVertices = null;

						//if (n % 2 == 0)
						createdVertices = Create4();
						//else
						//	createdVertices = Create6();

						var layout = new Dictionary<int, Point>();

						var firstCreatedVertex = createdVertices[vertexLabels[0]];

						double minX = firstCreatedVertex.X, minY = firstCreatedVertex.Y;
						foreach (var pos in createdVertices)
						{
							var location = pos.Value;
							double x = location.X * defaultDistanceRatio;
							double y = location.Y * defaultDistanceRatio;
							if (x < minX) minX = x;
							if (y < minY) minY = y;
							layout.Add(int.Parse(pos.Key), new Point(x, y));
						}

						minX -= LayoutOffset + 1;
						minY -= LayoutOffset + 1;

						//bool invalid = false;
						for (int key = 0; key < layout.Count; ++key)
						{
							layout[key] = new Point(layout[key].X - minX, layout[key].Y - minY);
							//if (layout[key].X < LayoutOffset || layout[key].Y < LayoutOffset) invalid = true;
						}

						var score = CalculateScore(layout);

						score.AnalyzeData();

						//if (invalid)
						//	score = FiniteStateMachineLayoutScore.Perfect;
						//else
						//	score = FiniteStateMachineLayoutScore.Worst;

						layoutsScores[n] = score;
						layouts[n] = layout;

					});
				}
				catch (AggregateException e)
				{
					foreach (var exception in e.InnerExceptions)
					{
						System.Console.Out.WriteLine("one of the layout jobs failed:");
						System.Console.Out.WriteLine(exception.ToString());
					}
				}

				int newBestLayout = 0;
				for (int j = 1; j < workGroupSize; ++j)
				{
					if (layoutsScores[j] == FiniteStateMachineLayoutScore.Perfect)
					{
						newBestLayout = j;
						break;
					}
					if (layoutsScores[j].CompareTo(layoutsScores[newBestLayout]) > 0)
						newBestLayout = j;
				}

				layoutsAll.Add(new KeyValuePair<Dictionary<int, Point>, FiniteStateMachineLayoutScore>(
					layouts[newBestLayout], layoutsScores[newBestLayout]));

				if (layoutsScores[newBestLayout] == FiniteStateMachineLayoutScore.Perfect)
				{
					bestLayout = layoutsAll.Count - 1;
					break;
				}

				if (layoutsScores[newBestLayout].CompareTo(layoutsAll[bestLayout].Value) > 0)
					bestLayout = layoutsAll.Count - 1;
			}

			vertices = layoutsAll[bestLayout].Key;
			layoutScore = layoutsAll[bestLayout].Value;
			CreateEdges();
			return;
		}

		//private IDictionary<string, Point> Create1()
		//{
		//	// 1st algorithm
		//	var algo1 = new SourceFirstTopologicalSortAlgorithm<string, Edge<string>>(graph);
		//	algo1.Compute();
		//	while (algo1.State != ComputationState.Finished)
		//		Thread.Sleep(100);

		//	return algo1.SortedVertices;
		//}

		/// <summary>
		/// Nodes are too close to each other.
		/// </summary>
		/// <returns></returns>
		private IDictionary<string, Point> Create2()
		{
			// 2nd algorithm
			ISOMLayoutParameters params2 = null;//new ISOMLayoutParameters();
			var algo2 = new ISOMLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>>(graph, params2);
			algo2.Compute();
			while (algo2.State != ComputationState.Finished)
				Thread.Sleep(100);

			return algo2.VertexPositions;
		}

		/// <summary>
		/// Crazy distances between nodes, and yet often nodes overlap the edges and edges intersect each other...
		/// </summary>
		/// <returns></returns>
		private IDictionary<string, Point> Create3()
		{
			var params3 = new BalloonTreeLayoutParameters();
			params3.MinRadius = (int)MinimumStateDistance;
			params3.Border = (int)MinimumStateDistance;
			var algo3 = new BalloonTreeLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>>(
				graph, vertexPositions, vertexSizes, params3, vertexLabels[0]);
			algo3.Compute();
			//while (algo3.State != ComputationState.Finished)
			//	Thread.Sleep(100);

			return algo3.VertexPositions;
		}

		/// <summary>
		/// Uses GraphSharp CompoundFDPLayoutAlgorithm, i.e. a force-based algorithm.
		/// 
		/// Pretty effective - for not too complicated graphs it is capable of providing flawless solution.
		/// </summary>
		/// <returns>raw set of coordinates of vertices</returns>
		private IDictionary<string, Point> Create4()
		{
			var algo4 = new CompoundFDPLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>>(
					graph, vertexSizes, vertexBorders, new Dictionary<string, CompoundVertexInnerLayoutType>());
			algo4.Compute();
			//while (algo4.State != ComputationState.Finished)
			//	Thread.Sleep(250);
			return algo4.VertexPositions;
		}

		/// <summary>
		/// Nodes frequently overlap edges. Pretty useless.
		/// </summary>
		/// <returns></returns>
		private IDictionary<string, Point> Create5()
		{
			DoubleTreeLayoutParameters params5;
			DoubleTreeLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>> algo5;

			params5 = new DoubleTreeLayoutParameters();
			params5.Direction = LayoutDirection.LeftToRight;
			params5.LayerGap = MinimumStateDistance;
			params5.PrioritizedTreeSide = DoubleTreeSides.Side2;
			params5.VertexGap = MinimumStateDistance;
			algo5 = new DoubleTreeLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>>(
					graph, Create4(), vertexSizes, params5, vertexLabels[0]);
			algo5.Compute();
			return algo5.VertexPositions;
		}

		/// <summary>
		/// It seems it does not do anything...
		/// </summary>
		/// <returns></returns>
		private IDictionary<string, Point> Create6()
		{
			RadialTreeLayoutParameters params6;
			RadialTreeLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>> algo6;

			params6 = new RadialTreeLayoutParameters(); // no actual params here
			algo6 = new RadialTreeLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>>(
					graph, Create4(), params6, vertexLabels[0]);
			// selected vertex is 'root' or something of this kind

			algo6.Compute();
			return algo6.VertexPositions;
		}

		private object RemoveOverlaps1()
		{
			OneWayFSAParameters params1 = new OneWayFSAParameters();
			params1.HorizontalGap = 40;
			params1.VerticalGap = 40;

			OneWayFSAAlgorithm<string> algo1;
			algo1 = new OneWayFSAAlgorithm<string>(null, params1);
			algo1.Compute();
			while (algo1.State != ComputationState.Finished)
				Thread.Sleep(250);

			return null;
		}

		private object RemoveOverlaps2()
		{
			OverlapRemovalParameters params2 = new OverlapRemovalParameters();
			params2.HorizontalGap = 40;
			params2.VerticalGap = 40;

			FSAAlgorithm<string> algo2;
			algo2 = new FSAAlgorithm<string>(null, params2);
			algo2.Compute();
			while (algo2.State != ComputationState.Finished)
				Thread.Sleep(250);

			return null;
		}

		private void CreateEdges()
		{
			edges = new Dictionary<MachineTransition, Tuple<int, int>>();
			bool loopsExist = false;

			foreach (var transition in transitions)
			{
				int idStart = transition.InitialStateId;
				int idEnd = transition.ResultingStateId;

				if (idStart == idEnd)
				{
					loopsExist = true;
					continue;
				}

				var start = vertices[idStart];
				var end = vertices[idEnd];

				int startAngle = 0;
				int endAngle = 0;

				startAngle = (int)start.Angle(end);
				endAngle = startAngle + (startAngle >= 180 ? -180 : +180);

				if (transitions.Any(x => x.InitialStateId == idEnd
					&& x.ResultingStateId == idStart))
				{
					startAngle += 10;
					endAngle -= 10;
				}

				var angles = new Tuple<int, int>(startAngle, endAngle);
				edges.Add(transition, angles);
			}

			if (!loopsExist)
				return;

			foreach (var transition in transitions)
			{
				int idStart = transition.InitialStateId;
				int idEnd = transition.ResultingStateId;

				if (idStart != idEnd)
					continue;

				int angle = (int)FindMostFreeAngle(idStart, true, true, 0);

				var angles = new Tuple<int, int>(angle, angle);
				edges.Add(transition, angles);

			}
		}

		/*
		private Dictionary<int, double> GetStatesTooCloseToEdge(Dictionary<int, Point> layout,
			int stateId1, int stateId2, double threshold)
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
		 */

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
		private FiniteStateMachineLayoutScore CalculateScore(Dictionary<int, Point> vertices)
		{
			FiniteStateMachineLayoutScore score = null;

			if (vertices == null || vertices.Count <= 1)
				return FiniteStateMachineLayoutScore.Perfect;

			foreach (var key1 in vertices.Keys)
				foreach (var key2 in vertices.Keys)
				{
					if (key1 == key2)
						continue;

					var p1 = vertices[key1];
					var p2 = vertices[key2];

					#region state on state
					// check if point are too close to each other
					if (key1 < key2)
					{
						double dist = p1.Distance(p2);
						if (dist < MinimumStateDistance)
						{
							//nodesDistanceScore -= Math.Sqrt(50 - dist);
							if (score == null)
								score = new FiniteStateMachineLayoutScore();
							score.VerticesOnVertices.Add(new Tuple<int, int, double>(key1, key2, dist));
						}
					}
					#endregion

					// check connected vertices
					int transitionIndex = transitions.IndexOf(x => x.InitialStateId == key1 && x.ResultingStateId == key2);
					//try
					//{
					//	transitionIndex = transitions.IndexOf(
					//		transitions.First(x => x.InitialStateId == key1 && x.ResultingStateId == key2)
					//		);
					//}
					//catch (InvalidOperationException)
					//{
					//	// silent catch
					//}
					if (transitionIndex == -1)
						continue;

					// old implementation of checking if connection exists:
					//if (!transitions.Any(x => x.InitialStateId == key1 && x.ResultingStateId == key2))
					//	continue;

					#region state on edge
					// check if any state is obstructed by the edge
					foreach (var key in vertices.Keys)
					{
						if (key == key1 || key == key2)
							continue;

						double dist = vertices[key].DistanceToLine(p1, p2);

						if (dist < 30)
						{
							if (score == null)
								score = new FiniteStateMachineLayoutScore();
							score.VerticesOnEdges.Add(new Tuple<int, int, double>(key, transitionIndex, dist));
						}
					}
					#endregion

					// check if the edge intersects with any other edge
					var line = new LineGeometry(p1, p2);
					foreach (var keyOther1 in vertices.Keys)
						foreach (var keyOther2 in vertices.Keys)
						{
							if (keyOther1 == keyOther2)
								continue;
							if (keyOther1 == key1 || keyOther2 == key2 || keyOther1 == key2 || keyOther2 == key1)
								continue;
							int transitionIndexOther = transitions.IndexOf(
								x => x.InitialStateId == keyOther1 && x.ResultingStateId == keyOther2);
							if (transitionIndexOther == -1)
								continue;

							var pOther1 = vertices[keyOther1];
							var pOther2 = vertices[keyOther2];

							if (p1.Intersects(p2, pOther1, pOther2))
							{
								if (score == null)
									score = new FiniteStateMachineLayoutScore();
								score.IntersectingEdges.Add(new Tuple<int, int>(transitionIndex, transitionIndexOther));
							}
						}
				}

			return score == null ? FiniteStateMachineLayoutScore.Perfect : score;
		}

		public void Draw(Canvas canvas, bool constructionMode, IList<RegularExpression> highlightedStates,
			IList<MachineTransition> highlightedTransitions, bool evaluationMode, int previousState,
			int currentState, bool evaluationEnded)
		{
			var canvasContent = canvas.Children;
			canvasContent.Clear();

			double maxX = 0;
			double maxY = 0;

			//IEnumerable<MachineTransition> transitionsFromInitialState = transitions.Where(x => x.InitialStateId == 0);

			{
				double startAngle = FindMostFreeAngle(0, true, false, 315);

				if (constructionMode && highlightedStates.Any(x => states.IndexOf(x) == 0))
					DrawStartArrow(canvas, HighlightedEdgeBrush, HighlightedEdgeBorderBrush, vertices[0], startAngle);
				else
					DrawStartArrow(canvas, EdgeBrush, EdgeBorderBrush, vertices[0], startAngle);
			}

			string text = String.Empty;

			for (int i = 0; i < vertices.Count; ++i)
			{
				var location = vertices[i];

				int transitionIndex = -1;
				foreach (var transition in transitions)
				{
					++transitionIndex;
					if (transition.InitialStateId != i)
						continue;

					var letters = new TextBlock();
					letters.Height = TextBlockHeight;
					//letters.Width = double.NaN; // Auto, goes to 100%
					letters.Width = transition.Item2.Count * TextBlockHeight;
					letters.Text = String.Join(" ", transition.Item2);
					letters.TextAlignment = TextAlignment.Center;

					Brush edgeBrush = null;
					Brush edgeBorderBrush = null;
					Brush edgeLabelBrush = null;
					if (constructionMode && highlightedTransitions.Any(x => x.InitialStateId == transition.InitialStateId
							&& x.ResultingStateId == transition.ResultingStateId))
					{
						edgeBrush = HighlightedEdgeBrush;
						edgeBorderBrush = HighlightedEdgeBorderBrush;
						edgeLabelBrush = HighlightedEdgeLabelBrush;
					}
					else if (evaluationMode && transition.InitialStateId == previousState
						&& transition.ResultingStateId == currentState)
					{
						edgeBrush = HighlightedEdgeBrush;
						edgeBorderBrush = HighlightedEdgeBorderBrush;
						edgeLabelBrush = HighlightedEdgeLabelBrush;
					}
					else
					{
						edgeBrush = EdgeBrush;
						edgeBorderBrush = EdgeBorderBrush;
						edgeLabelBrush = EdgeLabelBrush;
					}

					letters.Foreground = edgeLabelBrush;

					if (transition.Item3 == i)
					{
						// loop, i.e. directed edge to self

						double angle = edges[transition].Item1;

						DrawLoop(canvas, edgeBrush, edgeBorderBrush, edgeLabelBrush, location, angle);

						Point translatePoint = new Point().MoveTo(angle, LoopHeight);

						if (angle > 90 && angle <= 270)
							angle -= 180;

						var rotateTransform = new RotateTransform(angle, letters.Width / 2, TextBlockHeight / 2);
						var translateTransform = new TranslateTransform(translatePoint.X, translatePoint.Y);
						var transforms = new TransformGroup();
						transforms.Children.Add(rotateTransform);
						transforms.Children.Add(translateTransform);
						letters.RenderTransform = transforms;

						Canvas.SetLeft(letters, location.X - letters.Width / 2);
						Canvas.SetTop(letters, location.Y - TextBlockHeight / 2);
					}
					else
					{
						// normal edge
						Point endpoint = vertices[transition.Item3];

						double angle = location.Angle(endpoint) - 90;
						Point translatePoint;

						Point middle = new Point((location.X + endpoint.X) / 2, (location.Y + endpoint.Y) / 2);

						List<Point> intersections = new List<Point>();
						if (layoutScore.IntersectingEdges.Count > 0)
						{

							// detect intersections with other edges to move label to the longest uninterrupted segment
							foreach (var intr in layoutScore.IntersectingEdges)
							{
								if (intr.Item1 != transitionIndex)
									continue;
								MachineTransition intersectingTransition = transitions[intr.Item2];
								Point p1 = vertices[intersectingTransition.InitialStateId];
								Point p2 = vertices[intersectingTransition.ResultingStateId];

								Point intersection = location.FindIntersection(endpoint, p1, p2, true);

								intersections.Add(intersection);
							}

							// detect overlapping vertices to move label away from them
							foreach (var intr in layoutScore.VerticesOnEdges)
							{
								// the vertex must be near currently evaluated transition
								if (intr.Item2 != transitionIndex)
									continue;

								//double angle = location.Angle(endpoint); // already got it

								Point intersection = vertices[intr.Item1].Copy().MoveTo(angle, intr.Item3);

								if (intersection.DistanceToLine(location, endpoint) > intr.Item3)
									intersection = vertices[intr.Item1].Copy().MoveTo(angle + 180, intr.Item3);

								intersections.Add(intersection);
							}

							if (intersections.Count > 0)
							{
								intersections.Add(location.MoveTo(endpoint, StateEllipseDiameter / 2));
								intersections.Add(endpoint.MoveTo(location, StateEllipseDiameter / 2));
								intersections.Sort((a, b) => a.X.CompareTo(b.X));
							}
						}
						if (intersections.Count > 0)
						{
							var intersectionDistances = intersections.Zip(intersections.Skip(1), (x, y) => y.Distance(x)).ToArray();
							Console.Out.WriteLine(String.Join("; ", intersectionDistances));

							// find the best position for the label
							// i.e. the longest fragment without intersections
							double max = intersectionDistances.Max();
							int index = intersectionDistances.IndexOfMax();
							middle = intersections[index].MoveTo(intersections[index + 1], max / 2);

							//DrawDot(canvas, Brushes.Red, middle);
						}

						//TODO: take bent edges into account (i.e. case of two transitions: q1->q2 and q2->q1)
						// because in such cases labels are sometimes too close to the edges they belong to

						if (transitions.Any(x => x.InitialStateId == transition.ResultingStateId
								&& x.ResultingStateId == transition.InitialStateId))
						{
							if (angle > 90 && angle <= 270)
							{
								angle -= 180;
								translatePoint = new Point().MoveTo(angle, TextBlockHeight / 2 + 4);
							}
							else
								translatePoint = new Point().MoveTo(angle, -TextBlockHeight / 2 - 4);

							DrawEdge(canvas, edgeBrush, edgeBorderBrush, edgeLabelBrush,
								location, edges[transition].Item1, endpoint, edges[transition].Item2);
						}
						else
						{
							DrawEdge(canvas, edgeBrush, edgeBorderBrush, edgeLabelBrush, location, endpoint);

							if (angle > 90 && angle <= 270)
								angle -= 180;
							translatePoint = new Point().MoveTo(angle, TextBlockHeight / 2 - 2);
						}

						var rotateTransform = new RotateTransform(angle, letters.Width / 2, TextBlockHeight / 2);
						var translateTransform = new TranslateTransform(translatePoint.X, translatePoint.Y);
						var transforms = new TransformGroup();
						transforms.Children.Add(rotateTransform);
						transforms.Children.Add(translateTransform);
						letters.RenderTransform = transforms;

						Canvas.SetLeft(letters, middle.X - letters.Width / 2);
						Canvas.SetTop(letters, middle.Y - TextBlockHeight / 2);
					}
					canvasContent.Add(letters);
					Canvas.SetZIndex(letters, 0);
				}

				Brush vertexBrush = null;
				Brush vertexBackgroundBrush = null;
				Brush vertexLabelBrush = null;
				if (constructionMode && highlightedStates.Any(x => states.IndexOf(x) == i))
				{
					vertexBrush = HighlightedVertexBrush;
					vertexBackgroundBrush = HighlightedVertexBackgroundBrush;
					vertexLabelBrush = HighlightedVertexLabelBrush;
				}
				else if (evaluationMode)
				{
					if (currentState == i)
					{
						if (evaluationEnded)
						{
							vertexBrush = VertexBrush;
							vertexBackgroundBrush = WordAcceptedStateBackgroundBrush;
							vertexLabelBrush = VertexLabelBrush;
						}
						else
						{
							vertexBrush = HighlightedVertexBrush;
							vertexBackgroundBrush = HighlightedVertexBackgroundBrush;
							vertexLabelBrush = HighlightedVertexLabelBrush;
						}
					}
					else if (previousState == i && currentState == -1)
					{
						vertexBrush = VertexBrush;
						vertexBackgroundBrush = WordRejectedStateBackgroundBrush;
						vertexLabelBrush = VertexLabelBrush;
					}
					else
					{
						vertexBrush = VertexBrush;
						vertexBackgroundBrush = VertexBackgroundBrush;
						vertexLabelBrush = VertexLabelBrush;
					}
				}
				else
				{
					vertexBrush = VertexBrush;
					vertexBackgroundBrush = VertexBackgroundBrush;
					vertexLabelBrush = VertexLabelBrush;
				}

				DrawState(canvas, vertexBrush, vertexBackgroundBrush, vertexLabelBrush, location,
					String.Format("q{0}", i), acceptingStates.Any(x => x == states[i]));

				if (location.X > maxX) maxX = location.X;
				if (location.Y > maxY) maxY = location.Y;

				//++i;
			}
			canvas.Width = maxX + LayoutOffset;
			canvas.Height = maxY + LayoutOffset;
		}

		private double FindMostFreeAngle(int stateId, bool checkOutboundEdges, bool checkInboundEdges, int defaultAngle)
		{
			var edgesFromInitialState = new List<KeyValuePair<MachineTransition, Tuple<int, int>>>();
			if (checkOutboundEdges)
				edgesFromInitialState.AddRange(edges.Where(x => x.Key.InitialStateId == stateId));

			var edgesToInitialState = new List<KeyValuePair<MachineTransition, Tuple<int, int>>>();
			if (checkInboundEdges)
				edgesToInitialState.AddRange(edges.Where(x => x.Key.ResultingStateId == stateId));

			int totalCount = edgesFromInitialState.Count + edgesToInitialState.Count;

			if (totalCount == 0)
				return defaultAngle;

			if (totalCount == 1)
			{
				if (edgesFromInitialState.Count == 0)
					return (edgesToInitialState[0].Value.Item2 + 180) % 360;
				return (edgesFromInitialState[0].Value.Item1 + 180) % 360;
			}

			double angle = defaultAngle;

			List<double> angles = new List<double>();
			foreach (var edge in edgesFromInitialState)
				angles.Add(edge.Value.Item1);
			foreach (var edge in edgesToInitialState)
				angles.Add(edge.Value.Item2);
			angles.Sort();

			var anglesDiffs = new List<double>(angles.Zip(angles.Skip(1), (x, y) => y - x));
			anglesDiffs.Add(angles.First() + 360 - angles.Last());

			int index = anglesDiffs.IndexOf(anglesDiffs.Max());
			if (index == anglesDiffs.Count - 1)
			{
				angle = (angles[index] - 360 + angles.First()) / 2;
				if (angle < 0)
					angle += 360;
			}
			else
				angle = (angles[index] + angles[index + 1]) / 2;

			return angle;
		}

		public void Draw(Canvas canvas, IList<RegularExpression> highlightedStates,
			IList<MachineTransition> highlightedTransitions)
		{
			Draw(canvas, true, highlightedStates, highlightedTransitions, false, -1, -1, false);
		}

		public void Draw(Canvas canvas, int previousState, int currentState, bool evaluationEnded)
		{
			Draw(canvas, false, null, null, true, previousState, currentState, evaluationEnded);
		}

		/// <summary>
		/// Draws the layout of the underlying machine without any highlights.
		/// </summary>
		/// <param name="canvas"></param>
		public void Draw(Canvas canvas)
		{
			Draw(canvas, false, null, null, false, -1, -1, false);
		}

		private void DrawDot(Canvas canvas, Brush brush, Point location)
		{
			var canvasContent = canvas.Children;

			var border = new Ellipse();
			border.Width = 6;
			border.Height = 6;

			border.StrokeThickness = 0;
			border.Fill = brush;

			canvasContent.Add(border);
			Canvas.SetLeft(border, location.X - border.Width / 2);
			Canvas.SetTop(border, location.Y - border.Height / 2);
			Canvas.SetZIndex(border, 0);
		}

		private void DrawStartArrow(Canvas canvas, Brush brush, Brush borderBrush,
			Point location, double angle)
		{
			var canvasContent = canvas.Children;

			Point start = new Point().MoveTo(angle, StateEllipseDiameter);
			Point end = new Point().MoveTo(angle, StateEllipseDiameter / 2);

			ArrowLine poly = MakeArrow(start, end, brush, 1);

			canvasContent.Add(poly);
			Canvas.SetLeft(poly, location.X);
			Canvas.SetTop(poly, location.Y);
			Canvas.SetZIndex(poly, -5);

			if (borderBrush.Equals(Brushes.Transparent))
				return;

			poly = MakeArrow(start, end, borderBrush, 5);

			canvasContent.Add(poly);
			Canvas.SetLeft(poly, location.X);
			Canvas.SetTop(poly, location.Y);
			Canvas.SetZIndex(poly, -9);
		}

		private void DrawState(Canvas canvas, Brush borderBrush, Brush backgroundBrush, Brush labelBrush,
			Point location, string label, bool isAccepting)
		{
			var canvasContent = canvas.Children;

			var border = new Ellipse();
			border.Width = StateEllipseDiameter;
			border.Height = StateEllipseDiameter;

			border.Stroke = borderBrush;
			border.StrokeThickness = 1;
			border.Fill = backgroundBrush;

			canvasContent.Add(border);
			Canvas.SetLeft(border, location.X - border.Width / 2);
			Canvas.SetTop(border, location.Y - border.Height / 2);
			Canvas.SetZIndex(border, -4);

			// extra ellipse for accepting state
			if (isAccepting)
			{
				var border2 = new Ellipse();
				border2.StrokeThickness = 1;
				border2.Width = StateEllipseDiameter - 6;
				border2.Height = StateEllipseDiameter - 6;

				border2.Stroke = borderBrush;
				border.StrokeThickness = 1;
				border2.Fill = backgroundBrush;

				canvasContent.Add(border2);
				Canvas.SetLeft(border2, location.X - border2.Width / 2);
				Canvas.SetTop(border2, location.Y - border2.Height / 2);
				Canvas.SetZIndex(border2, -3);
			}

			var elem = new TextBlock();
			elem.TextAlignment = TextAlignment.Center;
			elem.Width = StateEllipseDiameter;
			elem.Height = TextBlockHeight;
			elem.Text = label;

			elem.Foreground = labelBrush;

			canvasContent.Add(elem);
			Canvas.SetLeft(elem, location.X - elem.Width / 2);
			Canvas.SetTop(elem, location.Y - elem.Height / 2);
			Canvas.SetZIndex(elem, 0);
		}

		/// <summary>
		/// Draws straight edge from start point to end point.
		/// </summary>
		/// <param name="canvas"></param>
		/// <param name="brush"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		private void DrawEdge(Canvas canvas, Brush brush, Brush borderBrush, Brush labelBrush,
			Point start, Point end)
		{
			var canvasContent = canvas.Children;

			Point target = end.Copy().MoveTo(start, StateEllipseDiameter / 2);

			ArrowLine edge = new ArrowLine();
			edge.ArrowEnds = ArrowEnds.End;
			edge.ArrowLength = 10;
			edge.ArrowAngle = 60;
			edge.X1 = start.X;
			edge.Y1 = start.Y;
			edge.X2 = target.X;
			edge.Y2 = target.Y;

			edge.StrokeThickness = 1;
			edge.Stroke = brush;

			canvasContent.Add(edge);
			Canvas.SetLeft(edge, 0);
			Canvas.SetTop(edge, 0);
			Canvas.SetZIndex(edge, -5);

			if (borderBrush.Equals(Brushes.Transparent))
				return;

			edge = new ArrowLine();
			edge.ArrowEnds = ArrowEnds.End;
			edge.ArrowLength = 10;
			edge.ArrowAngle = 60;
			edge.X1 = start.X;
			edge.Y1 = start.Y;
			edge.X2 = target.X;
			edge.Y2 = target.Y;

			edge.StrokeThickness = 5;
			edge.Stroke = borderBrush;

			canvasContent.Add(edge);
			Canvas.SetLeft(edge, 0);
			Canvas.SetTop(edge, 0);
			Canvas.SetZIndex(edge, -9);
		}

		/// <summary>
		/// Draws a bent edge, with defined angles of exit and entry.
		/// </summary>
		/// <param name="canvas"></param>
		/// <param name="brush"></param>
		/// <param name="start"></param>
		/// <param name="startAngle"></param>
		/// <param name="end"></param>
		/// <param name="endAngle"></param>
		private void DrawEdge(Canvas canvas, Brush brush, Brush borderBrush, Brush labelBrush,
			Point start, double startAngle, Point end, double endAngle)
		{
			var canvasContent = canvas.Children;

			double dist = start.Distance(end) / 2;
			Point startCtrl = start.Copy().MoveTo(startAngle, dist);
			Point endCtrl = end.Copy().MoveTo(endAngle, dist);

			var ptArr = end.Copy().MoveTo(endCtrl, StateEllipseDiameter / 2);

			// edge without arrow
			var bezier = new BezierSegment();
			bezier.Point1 = startCtrl;
			bezier.Point2 = endCtrl;
			bezier.Point3 = end.Copy().MoveTo(endCtrl, StateEllipseDiameter / 2 + 1);

			var path = new PathFigure();
			path.StartPoint = start.Copy().MoveTo(startCtrl, StateEllipseDiameter / 2);
			path.Segments.Add(bezier);

			var geo = new PathGeometry();
			geo.Figures = new PathFigureCollection();
			geo.Figures.Add(path);

			var edge = new Path();
			edge.Data = geo;

			edge.Stroke = brush;
			edge.StrokeThickness = 1;

			canvasContent.Add(edge);
			Canvas.SetLeft(edge, 0);
			Canvas.SetTop(edge, 0);
			Canvas.SetZIndex(edge, -5);

			// arrow
			var arrow = new ArrowLine();
			arrow.ArrowEnds = ArrowEnds.End;
			arrow.ArrowLength = 10;
			arrow.ArrowAngle = 60;
			arrow.X1 = bezier.Point3.X;
			arrow.Y1 = bezier.Point3.Y;
			arrow.X2 = ptArr.X;
			arrow.Y2 = ptArr.Y;

			arrow.Stroke = brush;
			arrow.StrokeThickness = 1;

			canvasContent.Add(arrow);
			Canvas.SetLeft(arrow, 0);
			Canvas.SetTop(arrow, 0);
			Canvas.SetZIndex(arrow, -5);

			if (borderBrush.Equals(Brushes.Transparent))
				return;

			// edge without arrow
			bezier = new BezierSegment();
			bezier.Point1 = startCtrl;
			bezier.Point2 = endCtrl;
			bezier.Point3 = end.Copy().MoveTo(endCtrl, StateEllipseDiameter / 2 + 1);

			path = new PathFigure();
			path.StartPoint = start.Copy().MoveTo(startCtrl, StateEllipseDiameter / 2);
			path.Segments.Add(bezier);

			geo = new PathGeometry();
			geo.Figures = new PathFigureCollection();
			geo.Figures.Add(path);

			edge = new Path();
			edge.Data = geo;

			edge.Stroke = borderBrush;
			edge.StrokeThickness = 5;

			canvasContent.Add(edge);
			Canvas.SetLeft(edge, 0);
			Canvas.SetTop(edge, 0);
			Canvas.SetZIndex(edge, -9);

			// arrow
			arrow = new ArrowLine();
			arrow.ArrowEnds = ArrowEnds.End;
			arrow.ArrowLength = 10;
			arrow.ArrowAngle = 60;
			//var ptArr = end.Copy().MoveTo(endCtrl, StateEllipseDiameter / 2);
			arrow.X1 = bezier.Point3.X;
			arrow.Y1 = bezier.Point3.Y;
			arrow.X2 = ptArr.X;
			arrow.Y2 = ptArr.Y;

			arrow.Stroke = borderBrush;
			arrow.StrokeThickness = 5;

			canvasContent.Add(arrow);
			Canvas.SetLeft(arrow, 0);
			Canvas.SetTop(arrow, 0);
			Canvas.SetZIndex(arrow, -9);
		}

		private void DrawLoop(Canvas canvas, Brush brush, Brush borderBrush, Brush labelBrush,
			Point location, double angle)
		{
			var canvasContent = canvas.Children;

			var tempPt = location.Copy().MoveTo(angle, LoopHeight);
			Point pt1Ctrl = tempPt.Copy().MoveTo(angle - 90, LoopHalfWidth);
			Point pt2Ctrl = tempPt.Copy().MoveTo(angle + 90, LoopHalfWidth);

			var ptArr = location.Copy().MoveTo(pt2Ctrl, StateEllipseDiameter / 2);

			// loop without arrow
			var bezier = new BezierSegment();
			bezier.Point1 = pt1Ctrl;
			bezier.Point2 = pt2Ctrl;
			bezier.Point3 = location.Copy().MoveTo(pt2Ctrl, StateEllipseDiameter / 2 + 1);

			var path = new PathFigure();
			path.StartPoint = location.Copy().MoveTo(pt1Ctrl, StateEllipseDiameter / 2);
			path.Segments.Add(bezier);

			var geo = new PathGeometry();
			geo.Figures = new PathFigureCollection();
			geo.Figures.Add(path);

			var loop = new Path();
			loop.Data = geo;

			loop.Stroke = brush;
			loop.StrokeThickness = 1;

			canvasContent.Add(loop);
			Canvas.SetLeft(loop, 0);
			Canvas.SetTop(loop, 0);
			Canvas.SetZIndex(loop, -5);

			// arrow
			var arrow = new ArrowLine();
			arrow.ArrowEnds = ArrowEnds.End;
			arrow.ArrowLength = 10;
			arrow.ArrowAngle = 60;
			arrow.X1 = bezier.Point3.X;
			arrow.Y1 = bezier.Point3.Y;
			arrow.X2 = ptArr.X;
			arrow.Y2 = ptArr.Y;

			arrow.Stroke = brush;
			arrow.StrokeThickness = 1;

			canvasContent.Add(arrow);
			Canvas.SetLeft(arrow, 0);
			Canvas.SetTop(arrow, 0);
			Canvas.SetZIndex(arrow, -5);

			// loop without arrow
			bezier = new BezierSegment();
			bezier.Point1 = pt1Ctrl;
			bezier.Point2 = pt2Ctrl;
			bezier.Point3 = location.Copy().MoveTo(pt2Ctrl, StateEllipseDiameter / 2 + 1);

			path = new PathFigure();
			path.StartPoint = location.Copy().MoveTo(pt1Ctrl, StateEllipseDiameter / 2);
			path.Segments.Add(bezier);

			geo = new PathGeometry();
			geo.Figures = new PathFigureCollection();
			geo.Figures.Add(path);

			loop = new Path();
			loop.Data = geo;

			loop.Stroke = borderBrush;
			loop.StrokeThickness = 5;

			canvasContent.Add(loop);
			Canvas.SetLeft(loop, 0);
			Canvas.SetTop(loop, 0);
			Canvas.SetZIndex(loop, -9);

			// arrow
			arrow = new ArrowLine();
			arrow.ArrowEnds = ArrowEnds.End;
			arrow.ArrowLength = 10;
			arrow.ArrowAngle = 60;
			arrow.X1 = bezier.Point3.X;
			arrow.Y1 = bezier.Point3.Y;
			arrow.X2 = ptArr.X;
			arrow.Y2 = ptArr.Y;

			arrow.Stroke = borderBrush;
			arrow.StrokeThickness = 5;

			canvasContent.Add(arrow);
			Canvas.SetLeft(arrow, 0);
			Canvas.SetTop(arrow, 0);
			Canvas.SetZIndex(arrow, -9);
		}

		private static ArrowLine MakeArrow(Point start, Point end,
			Brush strokeBrush, double strokeThickness)
		{
			ArrowLine poly = new ArrowLine();
			poly.X1 = start.X;
			poly.Y1 = start.Y;
			poly.X2 = end.X;
			poly.Y2 = end.Y;

			poly.ArrowEnds = ArrowEnds.End;
			poly.ArrowLength = 10;
			poly.ArrowAngle = 60;

			poly.Stroke = strokeBrush;
			poly.StrokeThickness = strokeThickness;

			return poly;
		}

	}
}
