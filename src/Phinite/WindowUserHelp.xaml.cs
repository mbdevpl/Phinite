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
		private static string infoUserHelp;

		private object fsmLock;

		private FiniteStateMachine fsm;

		private ReadOnlyCollection<RegularExpression> states;

		private ReadOnlyCollection<RegularExpression> accepting;

		public event PropertyChangedEventHandler PropertyChanged;

		public RegularExpression NewExpression
		{ get { return newExpression; } set { this.ChangeProperty(PropertyChanged, ref newExpression, value, "NewExpression"); } }
		private RegularExpression newExpression;

		private double[] newExpressionSimilarities;

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

		public WindowUserHelp(object machineOperationsLock, FiniteStateMachine machine)
		{
			fsmLock = machineOperationsLock;
			fsm = machine;

			//lock (fsmLock)
			{
				states = fsm.States;
				accepting = fsm.AcceptingStates;

				newExpression = fsm.NextNotLabeledState;

				newExpressionSimilarities = fsm.NextNotLabeledStateSimilarities;
			}

			newExpressionProcessed = false;
			expressionIsSelected = false;
			selectedExpression = -1;
			labeledExpressionsData = null;

			DataContext = this;
			InitializeComponent();

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
					String.Format("{0:0}%", newExpressionSimilarities[i] * 100)));

				++i;
			}

			LabeledExpressionsData = data;
		}

		private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ExpressionIsSelected == false)
				ExpressionIsSelected = true;
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
			// hard stuff...

			// TODO: really implement something here
			ResolveDifferent();
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
			if (infoUserHelp == null)
			{
				var s = new StringBuilder();

				s.AppendLine("Equivalent - given expression is equivalent to selected one.");
				s.AppendLine("Different - the expression is different from all labeled expressions.");
				s.AppendLine("No idea - PHINITE will automatically solve this problem.");
				s.AppendLine();
				s.AppendLine("Hint: closing the window is the same as last option");

				infoUserHelp = s.ToString();
			}
			var msg = new MessageFrame(this, "Phinite information", "User-assisted expression labeling", infoUserHelp);
			msg.ShowDialog();
		}

		private void WindowUserHelp_Closing(object sender, CancelEventArgs e)
		{
			AutoResolveEquivalenceProblem();
		}

	}
}
