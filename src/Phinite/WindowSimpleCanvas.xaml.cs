using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Phinite
{
	/// <summary>
	/// Interaction logic for WindowSimpleCanvas.xaml
	/// </summary>
	public partial class WindowSimpleCanvas : Window
	{
		private PartialExpression parseTree;

		public WindowSimpleCanvas(PartialExpression parseTree)
		{
			this.parseTree = parseTree;

			DataContext = this;
			InitializeComponent();

			ParseTreeDrawing.Draw(ParseTreeCanvas, parseTree);
		}

		private void ButtonOptimize_Click(object sender, RoutedEventArgs e)
		{
			parseTree.Optimize();

			ParseTreeDrawing.Draw(ParseTreeCanvas, parseTree);
		}

		private void ButtonReduce_Click(object sender, RoutedEventArgs e)
		{
			parseTree.Reduce();

			ParseTreeDrawing.Draw(ParseTreeCanvas, parseTree);
		}

	}
}
