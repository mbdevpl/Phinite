using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
	/// Interaction logic for WindowUserHelp.xaml
	/// </summary>
	public partial class WindowUserHelp : Window, INotifyPropertyChanged
	{
		/// <summary>
		/// Application settings.
		/// </summary>
		public PhiniteSettings Settings { get { return settings; } }
		private PhiniteSettings settings;

		private object fsmLock;

		private FiniteStateMachine fsm;

		private ReadOnlyCollection<RegularExpression> states;

		private ReadOnlyCollection<RegularExpression> accepting;

		public event PropertyChangedEventHandler PropertyChanged;

		public RegularExpression NewExpression
		{ get { return newExpression; } set { this.ChangeProperty(PropertyChanged, ref newExpression, value, "NewExpression"); } }
		private RegularExpression newExpression;

		private PartialExpression parseTree;

		private ReadOnlyCollection<double> newExpressionSimilarities;

		private bool newExpressionProcessed;

		public Boolean ExpressionIsSelected
		{ get { return expressionIsSelected; } set { this.ChangeProperty(PropertyChanged, ref expressionIsSelected, ref value, "ExpressionIsSelected"); } }
		private Boolean expressionIsSelected;

		public int SelectedExpression
		{ get { return selectedExpression; } set { this.ChangeProperty(PropertyChanged, ref selectedExpression, ref value, "SelectedExpression"); } }
		private int selectedExpression;

		public List<Tuple<RegularExpression, string, string, string>> LabeledExpressionsData
		{ get { return labeledExpressionsData; } set { this.ChangeProperty(PropertyChanged, ref labeledExpressionsData, value, "LabeledExpressionsData"); } }
		private List<Tuple<RegularExpression, string, string, string>> labeledExpressionsData;

		private bool? resolvedEquivalent = null;
		public bool? ResolvedEquivalent { get { return resolvedEquivalent; } }

		public WindowUserHelp(PhiniteSettings settings, object machineOperationsLock, FiniteStateMachine machine)
		{
			if (machineOperationsLock == null)
				throw new ArgumentNullException("machineOperationsLock");
			if (machine == null)
				throw new ArgumentNullException("machine");

			this.settings = settings;

			fsmLock = machineOperationsLock;
			fsm = machine;

			//lock (fsmLock)
			{
				states = fsm.States;
				accepting = fsm.AcceptingStates;

				newExpression = fsm.NextNotLabeledState;

				parseTree = newExpression.ParseTree;

				newExpressionSimilarities = fsm.NextNotLabeledStateSimilarities;
			}

			newExpressionProcessed = false;
			expressionIsSelected = false;
			selectedExpression = -1;
			labeledExpressionsData = null;

			DataContext = this;
			InitializeComponent();

			ParseTreeDrawing.Draw(ParseTreeCanvas, parseTree);

			var data = new List<Tuple<RegularExpression, string, string, string>>();
			int i = 0;
			foreach (var state in states)
			{
				var stateIsAccepting = accepting.Contains(state);

				StringBuilder s = new StringBuilder();
				if (i == 0)
					s.Append("initial state");
				if (stateIsAccepting)
				{
					if (i == 0)
						s.Append(", ");
					s.Append("accepting state");
				}

				//double similarity = newExpression.Similarity(state);

				data.Add(new Tuple<RegularExpression, string, string, string>(state, String.Format("q{0}", i), s.ToString(),
					String.Format("{0:0.00}%", newExpressionSimilarities[i] * 100)));

				++i;
			}

			LabeledExpressionsData = data;
		}

		private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ExpressionIsSelected == false)
				ExpressionIsSelected = true;
		}

		private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender == null || sender is DataGrid == false)
				return;
			if (e == null || e.OriginalSource is DependencyObject == false)
				return;

			int row;
			int column;

			((DataGrid)sender).FindElementLocation((DependencyObject)e.OriginalSource, out column, out row);

			if (column != 3)
				return;

			var parseTree = LabeledExpressionsData[row].Item1.ParseTree;

			if (parseTree == null)
				return;

			e.Handled = true;

			new WindowSimpleCanvas(parseTree).ShowDialog();
		}

		private void ResolveEquivalent()
		{
			lock (fsmLock)
			{
				if (!newExpressionProcessed)
				{
					resolvedEquivalent = true;
					fsm.Construct(1, LabeledExpressionsData[SelectedExpression].Item1, true);
					newExpressionProcessed = true;
				}
			}
		}

		private void ResolveDifferent()
		{
			lock (fsmLock)
			{
				if (!newExpressionProcessed)
				{
					resolvedEquivalent = false;
					fsm.Construct(1, null, true);
					newExpressionProcessed = true;
				}
			}
		}

		private void AutoResolveEquivalenceProblem()
		{
			lock (fsmLock)
			{
				if (!newExpressionProcessed)
				{
					// analyze similarities
					int iMax = -1;
					for (int i = 0; i < newExpressionSimilarities.Count; ++i)
					{
						if (newExpressionSimilarities[i] < FiniteStateMachine.SimilarityThresholdToInferEquivalence)
							continue;

						if (iMax == -1 || newExpressionSimilarities[iMax] < newExpressionSimilarities[i])
							iMax = i;
					}

					if (iMax == -1)
					{
						resolvedEquivalent = false;
						fsm.Construct(1, null, true);
					}
					else
					{
						resolvedEquivalent = true;
						fsm.Construct(1, LabeledExpressionsData[iMax].Item1, true);
					}
					newExpressionProcessed = true;
				}
			}
		}

		private void ButtonAbort_Click(object sender, RoutedEventArgs e)
		{
			lock (fsmLock)
			{
				newExpressionProcessed = true;
			}
			Close();
		}

		private void ButtonEquivalent_Click(object sender, RoutedEventArgs e)
		{
			if (ExpressionIsSelected == false)
				throw new InvalidOperationException("this button was supposed to be disabled...");

			ResolveEquivalent();
			Close();
		}

		private void ButtonDifferent_Click(object sender, RoutedEventArgs e)
		{
			ResolveDifferent();
			Close();
		}

		private void ButtonAuto_Click(object sender, RoutedEventArgs e)
		{
			AutoResolveEquivalenceProblem();
			Close();
		}

		private void Info_UserHelp(object sender, RoutedEventArgs e)
		{
			var msg = new MessageFrame(this, "Phinite information", "User-assisted expression labeling", App.Text_UserHelp);
			msg.ShowDialog();
		}

		private void WindowUserHelp_Closing(object sender, CancelEventArgs e)
		{
			AutoResolveEquivalenceProblem();
		}

	}
}
