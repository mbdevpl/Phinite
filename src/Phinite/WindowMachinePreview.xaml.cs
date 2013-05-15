using System;
using System.Collections.Generic;
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
	/// Interaction logic for WindowMachinePreview.xaml
	/// </summary>
	public partial class WindowMachinePreview : Window, INotifyPropertyChanged
	{

		private RegularExpression regexp1;
		private FiniteStateMachine fsm1;
		private FiniteStateMachineLayout layout1;

		private RegularExpression regexp2;
		private FiniteStateMachine fsm2;
		private FiniteStateMachineLayout layout2;

		public event PropertyChangedEventHandler PropertyChanged;

		public string Similarity { get { return similarity.ToString(); } }
		private double similarity;

		public string SimilarityRefined { get { return similarityRefined.ToString(); } }
		private double similarityRefined;

		public string Relationships { get { return relationships; } }
		private string relationships;

		public WindowMachinePreview(RegularExpression regexp1, RegularExpression regexp2, double refinedSimilarity)
		{
			int i = 0;

			this.regexp1 = regexp1;

			fsm1 = new FiniteStateMachine(regexp1);
			fsm1.Construct(4, null, false);

			layout1 = new FiniteStateMachineLayout(fsm1);
			layout1.Create(0, ref i);

			this.regexp2 = regexp2;

			fsm2 = new FiniteStateMachine(regexp2);
			fsm2.Construct(4, null, false);

			layout2 = new FiniteStateMachineLayout(fsm2);
			layout2.Create(0, ref i);

			similarity = regexp1.Similarity(regexp2);

			similarityRefined = refinedSimilarity;

			var relations = FiniteStateMachine.FindRelatedStates(fsm1, fsm2, 16);

			relationships = String.Join(", ", relations.Select((tuple) => String.Format("fsm1.q{0}==fsm2.q{1}", tuple.Item1, tuple.Item2)));

			DataContext = this;
			InitializeComponent();

			layout1.Draw(CanvasLeft);

			layout2.Draw(CanvasRight);

			this.InvokePropertyChanged(PropertyChanged, "Relationships");
		}

		private void ButtonClose_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

	}
}
