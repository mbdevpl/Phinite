using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Phinite
{
	/// <summary>
	/// Used to generate graphical representation of the parse tree.
	/// </summary>
	public static class ParseTreeDrawing
	{

		private static readonly Size ParseTreeNodeContentSize = new Size(50, 16);

		private static readonly Size ParseTreeNodeSize
			= new Size(ParseTreeNodeContentSize.Width + 4, ParseTreeNodeContentSize.Height * 2 + 2);

		private static readonly Point ParseTreeDrawingRootOffset
			= new Point(ParseTreeNodeSize.Width / 2 + 15, ParseTreeNodeSize.Height / 2);

		private static readonly Point ParseTreeNodeSpacing = new Point(40, 3);

		private static readonly int EdgeHorizontalMargin = -1;

		private static readonly double RootLineLength = 15.0;

		/// <summary>
		/// Clears the canvas and draws the parse tree on it.
		/// </summary>
		/// <param name="canvas">the canvas</param>
		/// <param name="parseTree">the parse tree to be drawn</param>
		public static void Draw(Canvas canvas, PartialExpression parseTree)
		{
			DrawParseTree(canvas, parseTree);
		}

		private static Point DrawParseTree(Canvas canvas, PartialExpression parseTree, double x = 0, double y = 0)
		{
			var canvasContent = canvas.Children;

			bool root = x == 0 && y == 0;
			if (root)
			{
				x = ParseTreeDrawingRootOffset.X;
				y = ParseTreeDrawingRootOffset.Y;
				canvasContent.Clear();
			}

			int treeWidth = parseTree.CalculateTreeWidth();
			double actualX = x;
			double actualY = y;
			if (treeWidth > 1)
			{
				double subtreeDrawingHeight = (treeWidth - 1) * (ParseTreeNodeSize.Height + ParseTreeNodeSpacing.Y);
				actualY += subtreeDrawingHeight / 2;
			}

			#region an indicator

			var indicator = new Ellipse();
			indicator.Fill = Brushes.DarkViolet;
			indicator.Width = 6;
			indicator.Height = indicator.Width;

			canvasContent.Add(indicator);
			Canvas.SetLeft(indicator, actualX - indicator.Width / 2);
			Canvas.SetTop(indicator, actualY - indicator.Height / 2);
			Canvas.SetZIndex(indicator, -1000);

			#endregion

			#region border for text block

			var border = new Ellipse();
			border.Stroke = Brushes.Gray;
			border.Fill = Brushes.White;
			border.StrokeThickness = 1;
			border.Width = ParseTreeNodeSize.Width;
			border.Height = ParseTreeNodeSize.Height;

			canvasContent.Add(border);
			Canvas.SetLeft(border, actualX - ParseTreeNodeSize.Width / 2);
			Canvas.SetTop(border, actualY - ParseTreeNodeSize.Height / 2);

			#endregion

			#region text block

			var elem = new TextBlock();
			elem.TextAlignment = TextAlignment.Center;
			elem.Width = ParseTreeNodeContentSize.Width;
			elem.Height = ParseTreeNodeContentSize.Height;

			canvasContent.Add(elem);
			Canvas.SetLeft(elem, actualX - ParseTreeNodeContentSize.Width / 2);
			Canvas.SetTop(elem, actualY - ParseTreeNodeContentSize.Height / 2);

			#endregion

			string text = String.Empty;
			if (parseTree.Role == PartialExpressionRole.EmptyWord)
				text = "epsilon";
			else if (parseTree.Role == PartialExpressionRole.Letter)
				text = parseTree.Value;
			else if ((parseTree.Role & PartialExpressionRole.InternalNode) > 0)
			{
				if (parseTree.Role == PartialExpressionRole.Concatenation)
					text = "concat";
				else if (parseTree.Role == PartialExpressionRole.Union)
					text = "union";

				double yy = y;
				foreach (var part in parseTree.Parts)
				{
					#region edge

					var edge = new Polyline();
					edge.Stroke = Brushes.Gray;
					edge.StrokeThickness = 2;
					edge.Points.Add(new Point(EdgeHorizontalMargin, actualY));
					edge.Points.Add(new Point(ParseTreeNodeSpacing.X - 2 * EdgeHorizontalMargin, yy));

					canvasContent.Add(edge);
					Canvas.SetLeft(edge, actualX + ParseTreeNodeSize.Width / 2);
					//Canvas.SetTop(edge, ParseTreeNodeSize.Height / 2);
					Canvas.SetZIndex(edge, -500);

					#endregion

					var rootPt = DrawParseTree(canvas, part, x + ParseTreeNodeSize.Width + ParseTreeNodeSpacing.X, yy);

					int partWidth = part.CalculateTreeWidth();

					if (partWidth > 1)
					{
						var pt = edge.Points[1];
						edge.Points[1] = new Point(pt.X, rootPt.Y);
					}

					yy += partWidth * (ParseTreeNodeSize.Height + ParseTreeNodeSpacing.Y);
				}
			}

			if (parseTree.Operator != UnaryOperator.None)
				text += RegularExpression.TagsStrings[(InputSymbolTag)parseTree.Operator];

			elem.Text = text;

			if (root)
			{
				#region start line

				var startLine = new Polyline();
				startLine.Stroke = Brushes.Gray;
				startLine.StrokeThickness = 2;
				startLine.Points.Add(new Point(0, 0));
				startLine.Points.Add(new Point(RootLineLength, 0));

				canvasContent.Add(startLine);
				Canvas.SetLeft(startLine, actualX - ParseTreeNodeSize.Width / 2 - RootLineLength);
				Canvas.SetTop(startLine, actualY);
				Canvas.SetZIndex(startLine, -500);

				#endregion

				double treeHeight = parseTree.CalculateTreeHeight();
				canvas.Width = (treeHeight - 1) * (ParseTreeNodeSize.Width + ParseTreeNodeSpacing.X)
					+ ParseTreeNodeSize.Width / 2 + ParseTreeDrawingRootOffset.X;
				canvas.Height = (treeWidth - 1) * (ParseTreeNodeSize.Height + ParseTreeNodeSpacing.Y)
					+ ParseTreeNodeSize.Height / 2 + ParseTreeDrawingRootOffset.Y;
			}

			return new Point(actualX, actualY);
		}

	}
}
