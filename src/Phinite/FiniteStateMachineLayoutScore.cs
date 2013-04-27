using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phinite
{
	public class FiniteStateMachineLayoutScore : IComparable
	{
		public static readonly FiniteStateMachineLayoutScore Perfect;

		public static readonly FiniteStateMachineLayoutScore Worst;

		static FiniteStateMachineLayoutScore()
		{
			Perfect = new FiniteStateMachineLayoutScore();
			Perfect.AnalyzeData();

			Worst = new FiniteStateMachineLayoutScore();
			Worst.AnalyzeData();
			Worst.Penalty = Double.MinValue;
		}

		private bool dataAnalyzed;

		public List<Tuple<int, int, double>> VerticesOnVertices;
		public int VerticesOnVerticesCount { get { return VerticesOnVertices.Count; } }

		public List<Tuple<int, int, double>> VerticesOnEdges;
		public int VerticesOnEdgesCount { get { return VerticesOnEdges.Count; } }

		public List<Tuple<int, int>> IntersectingEdges;
		public int IntersectingEdgesCount { get { return IntersectingEdges.Count; } }

		public double OptimalScaling;

		public double Penalty;

		public FiniteStateMachineLayoutScore()
		{
			VerticesOnVertices = new List<Tuple<int, int, double>>();

			VerticesOnEdges = new List<Tuple<int, int, double>>();

			IntersectingEdges = new List<Tuple<int, int>>();

			OptimalScaling = 0.0;

			Penalty = 0.0;

			dataAnalyzed = false;
		}

		public void AnalyzeData()
		{
			if (dataAnalyzed)
				return;
			dataAnalyzed = true;

			OptimalScaling = 1.0;

			foreach (var tuple in VerticesOnEdges)
				Penalty -= Math.Sqrt(FiniteStateMachineLayout.StateEllipseDiameter - tuple.Item3);
			foreach (var tuple in IntersectingEdges)
				Penalty -= 7;
			foreach (var tuple in VerticesOnVertices)
				Penalty -= Math.Sqrt(FiniteStateMachineLayout.MinimumStateDistance - tuple.Item3);
		}

		/// <summary>
		/// Converts this object into a single line of text that describes its current state.
		/// </summary>
		/// <returns>a description of current state of this instance</returns>
		public override string ToString()
		{
			if (!dataAnalyzed)
				AnalyzeData();

			if (this == Perfect)
				return "Perfect";

			if (this == Worst)
				return "Worst";

			return String.Format("Penalty={0:0.00} Edge^2={1} Vertex*Edge={2} Vertex^2={3} Scale={4:0.00}",
				Penalty, IntersectingEdgesCount, VerticesOnEdgesCount, VerticesOnVerticesCount, OptimalScaling);
		}

		public int CompareTo(object obj)
		{
			if (obj is FiniteStateMachineLayoutScore == false)
				return -1;

			var score = (FiniteStateMachineLayoutScore)obj;

			if (this == score)
				return 0;

			if (this == Perfect)
				return 1;

			if (score == Perfect)
				return -1;

			if (!dataAnalyzed)
				AnalyzeData();
			if (!score.dataAnalyzed)
				score.AnalyzeData();

			if (Penalty < score.Penalty)
				return -1;
			else if (Penalty > score.Penalty)
				return 1;

			return 0;
		}
	}
}
