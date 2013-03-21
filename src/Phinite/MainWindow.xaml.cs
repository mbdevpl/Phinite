using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Phinite
{
	/// <summary>
	/// Main frame of Phinite.
	/// 
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public static readonly Dictionary<string, string> Examples
			= new Dictionary<string, string>
			{
				{"Empty word", "."},
				{"Concatenation", "ababa"},
				{"Union", "aa+ab+ba+bb"},
				{"Empty word, concat. & union", "a(a+.)b(a+b)"},
				{"Kleene star", "ab^*"},
				{"Empty word, concat., union & star", "(a(a+.)(b(a+b))^*)^*"},
				{"Kleene plus", "a+b^+"},
				{"Parentheses", "(ab)^*+ab^*"},
				{"Binary numbers", "0+1(0+1)^*"},
				{"3 digit hexadecimal numbers", "(1+2+3+4+5+6+7)(0+1+2+3+4+5+6+7)(0+1+2+3+4+5+6+7)"},
				{"Example from BA", "a^+b^+ + ab^+c"},
				{"Hard 1", "(a+ab+abc+abcd+abcde+abcdef)^*"},
				{"Hard 2", "(a+.)^*b"},
				{"Hard 3", "(a(a+.)b^*)^*"},
				{"Hard 4", "(ab^*)^*"},
				{"Hard 5", "(((b)^*)((a((b)^*))^*))"},
				{"Harder", "(f+ef+def+cdef+bcdef+abcdef)^*"},
				{"All features", "(.+bb)(aabb)^+(.+aa)+(aa+bb)^*(aa+.)"}
			};

		public static readonly Dictionary<Status, string> StatusTexts
			= new Dictionary<Status, string>
			{
				{Status.Busy,"busy"},
				{Status.Ready,"ready"},
				{Status.ReadyForNextStep,"ready for next step"},
				{Status.AwaitingUserInput,"waiting for user input"},
				{Status.ValidatingInput,"validating input expression"},
				
				{Status.Computing,"computing"},
				{Status.NotComputing,"not computing"},
				{Status.Invalid,"invalid"}
			};

		private RegularExpression regexp;

		private object fsm_lock = new object();
		private FiniteStateMachine fsm;

		private Status status;

		private Status previousStatus;

		private bool stepByStep;

		public event PropertyChangedEventHandler PropertyChanged;

		public string InputText
		{
			get { return inputText; }
			set
			{
				if (inputText == value)
					return;
				inputText = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("InputText"));
			}
		}
		private string inputText = Examples["Hard 3"];

		public string StatusText
		{
			get { return statusText; }
			set
			{
				if (statusText == value)
					return;
				statusText = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
			}
		}
		private string statusText = "busy";

		private string pdflatexCommand
			//= @"MiKTeX\miktex\bin\pdflatex";
			= @"pdflatex";

		private bool useSystemDefaultPdfViewer
			//= false;
			= true;

		private string pdfViewerCommand = @"SumatraPDF\SumatraPDF.exe";

		public string LatexOutputText
		{
			get { return latexOutputText; }
			set
			{
				if (latexOutputText == value)
					return;
				latexOutputText = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("LatexOutputText"));
			}
		}
		private string latexOutputText = "";

		public List<Tuple<RegularExpression, string, string>> LabeledExpressionsData
		{
			get { return labeledExpressionsData; }
			set
			{
				if (labeledExpressionsData == value)
					return;
				labeledExpressionsData = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("LabeledExpressionsData"));
			}
		}
		private List<Tuple<RegularExpression, string, string>> labeledExpressionsData;

		public List<Tuple<RegularExpression, string, string, string, RegularExpression>> TransitionsData
		{
			get { return transitionsData; }
			set
			{
				if (transitionsData == value)
					return;
				transitionsData = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("TransitionsData"));
			}
		}
		private List<Tuple<RegularExpression, string, string, string, RegularExpression>> transitionsData;

		public MainWindow()
		{
			//some dummy calls to load dlls:
			//var var1 = new RegularExpression("a", true);
			//var var2 = new FiniteStateMachine(var1, true);

			DataContext = this;

			InitializeComponent();

			SetUIVisibilityState(UIVisibility.Input);

			foreach (string key in Examples.Keys)
			{
				var item = new MenuItem();
				item.Header = key;
				item.Click += OptionExample_Click;
				MenuExamples.Items.Add(item);
			}

			ChangeStatus(Status.Ready);
		}

		private void ChangeStatus(Status newStatus)
		{
			if (status == newStatus)
				return;

			StatusText = StatusTexts[newStatus];
			previousStatus = status;
			status = newStatus;

			if ((previousStatus & Status.Computing) != Status.Invalid &&
				(status & Status.Computing) != Status.Invalid)
				return;

			bool enabledState = true;
			if ((status & Status.Computing) != Status.Invalid)
				enabledState = false;
			else if ((status & Status.NotComputing) != Status.Invalid)
				enabledState = true;
			else
				throw new ArgumentException("failed to enter an invalid status", "status");

			UIEnabled enabled = enabledState ? UIEnabled.Yes : UIEnabled.No;
			SetUIEnabled(enabled);
		}

		private void SetUIEnabled(UIEnabled enabled)
		{
			SetEnabledStateDel del = new SetEnabledStateDel(SetUIEnabledDirectly);
			if (!Dispatcher.CheckAccess())
				Dispatcher.BeginInvoke(del, DispatcherPriority.Normal, enabled);
			else
				SetUIEnabledDirectly(enabled);
		}

		private delegate void SetEnabledStateDel(UIEnabled enabled);

		private void SetUIEnabledDirectly(UIEnabled enabled)
		{
			if (enabled == UIEnabled.Yes || enabled == UIEnabled.No)
			{
				bool enabledState = enabled == UIEnabled.Yes ? true : false;

				Input.IsEnabled = enabledState;
				MenuExamples.IsEnabled = enabledState;
				OptionStepByStep.IsEnabled = enabledState;
				OptionImmediate.IsEnabled = enabledState;
			}
			else
				throw new NotImplementedException("partial ui disabling is not implemented");
		}

		private void SetUIVisibilityState(UIVisibility visibility)
		{
			//Action a = new Action<UIVisibility>(
			//	method(UIVisibility visibility){
			//		if (visibility == UIVisibility.Input)
			//		{
			//			AreaForIntermediateResult.Visibility = Visibility.Hidden;
			//			AreaForFinalResult.Visibility = Visibility.Hidden;
			//			AreaForInput.Visibility = Visibility.Visible;
			//		}
			//	}
			//);

			if (!Dispatcher.CheckAccess())
				Dispatcher.BeginInvoke(new Action<UIVisibility>(SetUIVisibilityStateDirectly),
					DispatcherPriority.Normal, visibility);
			else
				SetUIVisibilityStateDirectly(visibility);
		}

		private void SetUIVisibilityStateDirectly(UIVisibility visibility)
		{
			if (visibility == UIVisibility.Input)
			{
				AreaForIntermediateResult.Visibility = Visibility.Hidden;
				AreaForFinalResult.Visibility = Visibility.Hidden;
				AreaForInput.Visibility = Visibility.Visible;
			}
			else if (visibility == UIVisibility.IntermediateResult)
			{
				AreaForInput.Visibility = Visibility.Hidden;
				AreaForFinalResult.Visibility = Visibility.Hidden;
				AreaForIntermediateResult.Visibility = Visibility.Visible;
			}
			else if (visibility == UIVisibility.FinalResult)
			{
				AreaForInput.Visibility = Visibility.Hidden;
				AreaForIntermediateResult.Visibility = Visibility.Hidden;
				AreaForFinalResult.Visibility = Visibility.Visible;
			}
		}

		private void InitializeValidation()
		{
			ChangeStatus(Status.ValidatingInput);

			Thread t = new Thread(ValidationWorker);
			t.Name = "ValidationThread";
			//t.Priority = ThreadPriority.BelowNormal;
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
		}

		private void ValidationWorker()
		{
			try
			{
				//Thread.Sleep(500);
				regexp = new RegularExpression(InputText);
				regexp.EvaluateInput();
			}
			catch (ArgumentException e)
			{
				new MessageFrame("Phinite/Regexp error", "Regular expression validation failed",
					String.Format("Given regular expression is not valid: {0}.", e.Message)
					).ShowDialog();
				ChangeStatus(Status.Ready);
				return;
			}
			catch (Exception e)
			{
				new MessageFrame("Phinite/Regexp error", "Regular expression validation failed",
					String.Format("There was some unexpected error while evaluating the input: {0}.", e.Message)
					).ShowDialog();
				ChangeStatus(Status.Ready);
				return;
			}

			//Thread t = new Thread(ValidationEnded);
			//t.Name = "ValidationEndingThread";
			////t.Priority = ThreadPriority.BelowNormal;
			//t.SetApartmentState(ApartmentState.STA);
			//t.Start();

			Dispatcher.BeginInvoke((Action)delegate { OriginalInput.Text = regexp.Input; });
			Dispatcher.BeginInvoke((Action)delegate { ValidatedInput.Text = regexp.ToString(); });
			LabeledExpressionsData = null;
			TransitionsData = null;

			try
			{
				//Thread.Sleep(500);
				//if (fsm == null)
				fsm = new FiniteStateMachine(regexp);
				//fsm.EvaluateInput();
			}
			catch (Exception e)
			{
				new MessageFrame("Phinite/FSM error", "Finite-state machine was not created",
					String.Format("There was some unexpected error while creating the finite-state machine: {0}.", e.Message)
					).ShowDialog();
				ChangeStatus(Status.ReadyForNextStep);
				return;
			}

			SetUIVisibilityState(UIVisibility.IntermediateResult);

			if (stepByStep)
			{
				ChangeStatus(Status.ReadyForNextStep);
				return;
			}

			PerformOneStepOfFSMConstruction();
			//InitializeComputation();
		}

		//private void ValidationEnded()
		//{
		//}

		//private void InitializeComputation()
		//{
		//	Thread t = new Thread(ComputationInitializationWorker);
		//	t.Name = "ComputationInitializationThread";
		//	t.SetApartmentState(ApartmentState.STA);
		//	t.Start();
		//}

		//private void ComputationInitializationWorker()
		//{
		//	ChangeStatus(Status.Busy);

		//	//if (stepByStep)
		//	//{
		//	//	//return;
		//	//}

		//	//Thread t = new Thread(ComputationEnded);
		//	//t.Name = "ComputationEndingThread";
		//	//t.SetApartmentState(ApartmentState.STA);
		//	//t.Start();


		//}

		private void PerformOneStepOfFSMConstruction()
		{
			Thread t = new Thread(ComputationStepWorker/*, int.MaxValue*/);
			t.Name = "ComputationStepThread";
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
		}

		private void ComputationStepWorker()
		{
			ChangeStatus(Status.Busy);

			try
			{
				//Thread.Sleep(500); // perform one step of computation
				lock (fsm_lock)
				{
					if (fsm == null)
					{
						return;
					}
					fsm.Construct(1);
				}
			}
			//catch (StackOverflowException e) // this cannot be caught since .NET 2.0
			//{
			//	new MessageFrame("Phinite/FSM error", "Finite-state machine was not created",
			//		String.Format("The provided regular expression seems too complicated for the program,"
			//		+ " which resulted in too many recursively nested function calls (i.e. a stack overflow): {0}.", e.Message)
			//		).ShowDialog();
			//	ChangeStatus(Status.ReadyForNextStep);
			//	return;
			//}
			catch (Exception e)
			{
				new MessageFrame("Phinite/FSM error", "Finite-state machine was not created",
					String.Format("There was some unexpected error while creating the finite-state machine: {0}.", e.Message)
					).ShowDialog();
				ChangeStatus(Status.ReadyForNextStep);
				return;
			}

			if (fsm.IsConstructionFinished())
			{
				ComputationEnded();
				return;
			}

			Thread t = new Thread(ComputationStepEnded);
			t.Name = "ComputationStepEndingThread";
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
		}

		private void ComputationStepEnded()
		{
			//Thread.Sleep(500); // put intermediate results into AreaForIntermediateResult

			var data = new List<Tuple<RegularExpression, string, string>>();
			int i = 0;
			lock (fsm_lock)
			{
				if (fsm == null)
					return;

				var accepting = fsm.AcceptingStates;
				var states = fsm.States;
				foreach (var state in states)
				{
					StringBuilder s = new StringBuilder();
					if (i == 0)
						s.Append("initial state");
					if (accepting.Contains(state))
					{
						if (i == 0)
							s.Append(", ");
						s.Append("accepting state");
					}
					data.Add(new Tuple<RegularExpression, string, string>(state, String.Format("q{0}", i), s.ToString()));
					++i;
				}
				LabeledExpressionsData = data;

				var data2 = new List<Tuple<RegularExpression, string, string, string, RegularExpression>>();
				foreach (var transition in fsm.Transitions)
				{
					data2.Add(new Tuple<RegularExpression, string, string, string, RegularExpression>(
						states[transition.Item1], String.Format("q{0}", transition.Item1),
						String.Join(", ", transition.Item2),
						String.Format("q{0}", transition.Item3), states[transition.Item3]));
				}
				TransitionsData = data2;
			}

			SetUIVisibilityState(UIVisibility.IntermediateResult);

			if (stepByStep)
			{
				ChangeStatus(Status.ReadyForNextStep);
				return;
			}

			PerformOneStepOfFSMConstruction();
		}

		private void ComputationEnded()
		{
			//Thread.Sleep(500); // generate tex code for resulting graph

			lock (fsm_lock)
			{
				if (fsm == null)
					return;
				LatexOutputText = LatexWriter.GenerateFullLatex(inputText, regexp, fsm, true, true);
			}

			ChangeStatus(Status.ReadyForNextStep);

			SetUIVisibilityState(UIVisibility.FinalResult);
		}

		private void LatexWorker()
		{
			ChangeStatus(Status.Busy);

			DateTime dt = DateTime.Now;
			string filename = "phinite-result_" + dt.ToString("yyyy-MM-dd_HH-mm-ss");
			string generatedTex = filename + ".tex";
			string generatedTexDir = Environment.CurrentDirectory.Replace('\\', '/');
			string generatedPdf = filename + ".pdf";

			File.AppendAllText(generatedTex, LatexOutputText);

			string latexExecutable = pdflatexCommand;
			string latexOptions = "-shell-escape -interaction=nonstopmode"
				+ " \"" + generatedTexDir + "/" + generatedTex + "\"";

			Process p = new Process();
			p.StartInfo = new ProcessStartInfo(latexExecutable, latexOptions);
			try
			{
				if (!p.Start())
				{
					new MessageFrame("Phinite/LaTeX error", "Error while starting LaTeX",
								String.Format("Unable to start LaTeX with this command:\n\n{0} {1}", latexExecutable, latexOptions)
								).ShowDialog();
					throw new ThreadStateException();
				}

				if (!p.WaitForExit(30000))
				{
					new MessageFrame("Phinite/LaTeX error", "LaTeX timeout",
								"It takes more than 30 seconds to build PDF file, Phinite will not wait for this to finish. The PDF file will not be opened automatically, even if it is finally created."
								).ShowDialog();
					throw new ThreadStateException();
				}

				if (p.ExitCode != 0)
				{
					if (File.Exists(generatedPdf))
						new MessageFrame("Phinite/LaTeX error", "Minor errors in LaTeX execution",
							"PDF file was created, but there were some errors and the result may not look as good as expected."
							).ShowDialog();
					else
						new MessageFrame("Phinite/LaTeX error", "Severe errors in LaTeX execution",
							"LaTeX failed to create the PDF file due to some critical errors. Read log to diagnose a problem."
							).ShowDialog();
					throw new ThreadStateException();
				}

				File.Delete(filename + ".log");
				File.Delete(filename + ".aux");
				if (useSystemDefaultPdfViewer)
					Process.Start(generatedPdf);
				else
					Process.Start(pdfViewerCommand, generatedPdf);
			}
			catch (Win32Exception)
			{
				new MessageFrame("Phinite/LaTeX/PDF error", "Error in Phinite configuration",
							String.Format("There is no LaTeX executable at this path:\n\n{0}\n\nAnd/or there is no PDF viewer at this path:\n\n", pdflatexCommand, pdfViewerCommand)
							).ShowDialog();
			}
			catch (ThreadStateException)
			{
				// silent catch
			}

			Thread t = new Thread(LatexEnded);
			t.Name = "LatexAndPdfGenerationEndingThread";
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
		}

		private void LatexEnded()
		{
			ChangeStatus(Status.ReadyForNextStep);
		}

		#region input screen handlers

		private void OptionImmediate_Click(object sender, RoutedEventArgs e)
		{
			stepByStep = false;
			InitializeValidation();
		}

		private void OptionStepByStep_Click(object sender, RoutedEventArgs e)
		{
			stepByStep = true;
			InitializeValidation();
		}

		#endregion

		#region main menu handlers

		private void OptionExample_Click(object sender, RoutedEventArgs e)
		{
			if (sender is HeaderedItemsControl == false)
				return;
			var item = (HeaderedItemsControl)sender;
			InputText = Examples[item.Header.ToString()];
		}

		private void OptionAbout_Click(object sender, RoutedEventArgs e)
		{
			StringBuilder s = new StringBuilder();

			s.AppendLine("Implemented in WPF by Mateusz Bysiek.");
			s.AppendLine();
			s.AppendLine("To work properly, this application needs:");
			s.AppendLine("- Windows 7 operating system");
			s.AppendLine("- .NET 4.0 framework, full profile");
			s.AppendLine("- any LaTeX distibution with required packages");
			s.AppendLine("- any PDF viewer");
			s.AppendLine();
			s.AppendLine("LaTeX packages required:");
			s.AppendLine("- l3kernel");
			s.AppendLine("- preprint");
			s.AppendLine("- pgf");
			s.AppendLine("- hm");
			s.AppendLine("- hs");
			s.AppendLine("- xcolor");

			new MessageFrame("About Phinite", "Information about Phinite", s.ToString(), PhiImage.Source).ShowDialog();
		}

		private void OptionViewBA_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Process.Start("bysiekm-business-analysis.pdf");
			}
			catch (Win32Exception)
			{
				new MessageFrame("Phinite", "Missing content", "business analysis file was not found").ShowDialog();
			}
		}

		private void OptionViewTA_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Process.Start("bysiekm-technical-analysis.pdf");
			}
			catch (Win32Exception)
			{
				new MessageFrame("Phinite", "Missing content", "technical analysis file was not found").ShowDialog();
			}
		}

		private void OptionExit_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		#endregion

		private void OptionNextStep_Click(object sender, RoutedEventArgs e)
		{
			PerformOneStepOfFSMConstruction();
		}

		private void OptionFinalResult_Click(object sender, RoutedEventArgs e)
		{
			stepByStep = false;

			PerformOneStepOfFSMConstruction();
		}

		private void OptionAbort_Click(object sender, RoutedEventArgs e)
		{
			ChangeStatus(Status.Busy);

			lock (fsm_lock)
			{
				fsm = null;
				regexp = null;
			}

			SetUIVisibilityState(UIVisibility.Input);

			ChangeStatus(Status.Ready);
		}

		private void OptionGenerateAndViewPDF_Click(object sender, RoutedEventArgs e)
		{
			Thread t = new Thread(LatexWorker);
			t.Name = "LatexAndPdfGenerationThread";
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			ChangeStatus(Status.Busy);

			lock (fsm_lock)
			{
				fsm = null;
				regexp = null;
			}

			base.OnClosing(e);
		}

	}
}
