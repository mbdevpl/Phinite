using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphSharp.Algorithms.Layout.Compound.FDP;
using QuickGraph;

namespace Phinite
{

	public class MachineLayoutAlgorithm : CompoundFDPLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>>
	{

		public MachineLayoutAlgorithm()
			: base(null, null, null, null)
		{
		}

		public void RunIt()
		{
			// 1st algorithm
			//var algo1 = new QuickGraph.Algorithms.SourceFirstTopologicalSortAlgorithm<string, Edge<string>>(graph);
			//algo1.Compute();
			//while (algo1.State != QuickGraph.Algorithms.ComputationState.Finished)
			//	Thread.Sleep(100);

			//layout = new Dictionary<int, Point>();
			//foreach (var pos in algo1.)
			//{
			//	layout.Add(int.Parse(pos.Key), new Point(pos.Value.X, pos.Value.Y));
			//}

			// 2nd algorithm
			//ISOMLayoutParameters params2 = null;//new ISOMLayoutParameters();
			//var algo2 = new ISOMLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>>(graph, params2);
			//algo2.Compute();
			//while (algo2.State != QuickGraph.Algorithms.ComputationState.Finished)
			//	Thread.Sleep(100);

			//layout = new Dictionary<int, Point>();
			//foreach (var pos in algo2.VertexPositions)
			//{
			//	layout.Add(int.Parse(pos.Key), new Point(pos.Value.X, pos.Value.Y));
			//}

			//if (CalculateLayoutScore(layout) == 0)
			//	return layout;

			// 3rd algorithm
			//var params3 = new BalloonTreeLayoutParameters();
			//params3.MinRadius = 50;
			//params3.Border = 1;
			//var algo3 = new BalloonTreeLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>>(
			//	graph, null/*vertexPositions*/, vertexSizes, params3, vertices[0]);
			//algo3.Compute();
			//while (algo3.State != QuickGraph.Algorithms.ComputationState.Finished)
			//	Thread.Sleep(100);

			//layout = new Dictionary<int, Point>();
			//foreach (var pos in algo3.VertexPositions)
			//{
			//	layout.Add(int.Parse(pos.Key), new Point(pos.Value.X, pos.Value.Y));
			//}

			//if (CalculateLayoutScore(layout) == 0)
			//	return layout;
		}
	}

}
