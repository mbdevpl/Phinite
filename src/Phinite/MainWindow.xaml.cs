using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

		private object regexpAndFsmLock = new object();
		private RegularExpression regexp;
		private FiniteStateMachine fsm;

		private object uiStateLock = new object();
		private UIState uiState;

		private bool stepByStep;

		private string pdflatexCommand
			//= @"MiKTeX\miktex\bin\pdflatex";
			= @"pdflatex";

		private bool useSystemDefaultPdfViewer
			//= false;
			= true;

		private string pdfViewerCommand = @"SumatraPDF\SumatraPDF.exe";

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Plain text input to be converted to a regular expression
		/// </summary>
		public string InputRegexpText
		{
			get { return inputRegexpText; }
			set
			{
				if (inputRegexpText == value)
					return;
				inputRegexpText = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("InputRegexpText"));
			}
		}
		private string inputRegexpText = Examples["Hard 3"];

		/// <summary>
		/// Plain text that represents a preprocessed (validated and optimized) regular expression.
		/// </summary>
		public string ValidatedRegexpText
		{
			get { return validatedRegexpText; }
			set
			{
				if (validatedRegexpText == value)
					return;
				validatedRegexpText = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("ValidatedRegexpText"));
			}
		}
		private string validatedRegexpText = String.Empty;

		/// <summary>
		/// Plain text to be evaluated as an input word for a finite-state machine.
		/// </summary>
		public string InputWordText
		{
			get { return inputWordText; }
			set
			{
				if (inputWordText == value)
					return;
				inputWordText = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("InputWordText"));
			}
		}
		private string inputWordText = String.Empty;

		/// <summary>
		/// Text visible in the status bar.
		/// </summary>
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
			DataContext = this;

			InitializeComponent();

			SetUIState(UIState.Loading);
			CallMethodInNewThread(WindowInitializationWorker, "WindowInitialization");
		}

		//private void ChangeStatus(Status newStatus)
		//{
		//	if (status == newStatus)
		//		return;

		//	StatusText = StatusTexts[newStatus];
		//	previousStatus = status;
		//	status = newStatus;

		//	if ((previousStatus & Status.Computing) != Status.Invalid &&
		//		(status & Status.Computing) != Status.Invalid)
		//		return;

		//	bool enabledState = true;
		//	if ((status & Status.Computing) != Status.Invalid)
		//		enabledState = false;
		//	else if ((status & Status.NotComputing) != Status.Invalid)
		//		enabledState = true;
		//	else
		//		throw new ArgumentException("failed to enter an invalid status", "status");

		//	UIEnabled enabled = enabledState ? UIEnabled.Yes : UIEnabled.No;
		//	SetUIEnabled(enabled);
		//}

		//private void SetUIEnabled(UIEnabled enabled)
		//{
		//	SetEnabledStateDel del = new SetEnabledStateDel(SetUIEnabledDirectly);
		//	if (!Dispatcher.CheckAccess())
		//		Dispatcher.BeginInvoke(del, DispatcherPriority.Normal, enabled);
		//	else
		//		SetUIEnabledDirectly(enabled);
		//}

		//private delegate void SetEnabledStateDel(UIEnabled enabled);

		//private void SetUIEnabledDirectly(UIEnabled enabled)
		//{
		//	if (enabled == UIEnabled.Yes || enabled == UIEnabled.No)
		//	{
		//		bool enabledState = enabled == UIEnabled.Yes ? true : false;

		//		Input.IsEnabled = enabledState;
		//		MenuExamples.IsEnabled = enabledState;
		//		OptionStepByStep.IsEnabled = enabledState;
		//		OptionImmediate.IsEnabled = enabledState;
		//	}
		//	else
		//		throw new NotImplementedException("partial ui disabling is not implemented");
		//}

		//private void SetUIVisibilityState(UIVisibility visibility)
		//{
		//	//Action a = new Action<UIVisibility>(
		//	//	method(UIVisibility visibility){
		//	//		if (visibility == UIVisibility.Input)
		//	//		{
		//	//			AreaForIntermediateResult.Visibility = Visibility.Hidden;
		//	//			AreaForFinalResult.Visibility = Visibility.Hidden;
		//	//			AreaForRegexpInput.Visibility = Visibility.Visible;
		//	//		}
		//	//	}
		//	//);

		//	if (!Dispatcher.CheckAccess())
		//		Dispatcher.BeginInvoke(new Action<UIVisibility>(SetUIVisibilityStateDirectly),
		//			DispatcherPriority.Normal, visibility);
		//	else
		//		SetUIVisibilityStateDirectly(visibility);
		//}

		private void SetUIState(UIState newUiState, string overrideStatusText = null)
		{
			if (!Dispatcher.CheckAccess())
				Dispatcher.BeginInvoke(new Action<UIState, string>(SetUIStateDirectly),
					DispatcherPriority.Normal, newUiState, overrideStatusText);
			else
				SetUIStateDirectly(newUiState, overrideStatusText);
		}

		private void SetUIStateDirectly(UIState newUiState, string overrideStatusText)
		{
			lock (uiStateLock)
			{
				if (uiState == newUiState)
					return;
				if ((newUiState & UIState.Loading) > 0)
				{
					SetUIVisibility(null);

					MenuMain.IsEnabled = false;
				}
				else if ((newUiState & UIState.RegexpInputPhase) > 0)
				{
					SetUIVisibility(AreaForRegexpInput);

					MenuMain.IsEnabled = true;
					MenuExamples.IsEnabled = true;

					if (newUiState == UIState.ReadyForNewInputAfterInvalidInput)
					{
						InvalidExpressionBorder.BorderBrush = Brushes.Red;
						OptionStepByStep.IsEnabled = false;
						OptionImmediate.IsEnabled = false;
					}
					else
					{
						InvalidExpressionBorder.BorderBrush = Brushes.Transparent;
						OptionStepByStep.IsEnabled = true;
						OptionImmediate.IsEnabled = true;
					}

					Input.IsEnabled = true;
				}
				else if ((newUiState & UIState.ValidationPhase) > 0)
				{
					SetUIVisibility(AreaForRegexpInput);

					MenuMain.IsEnabled = true;
					MenuExamples.IsEnabled = false;

					Input.IsEnabled = false;
					OptionStepByStep.IsEnabled = false;
					OptionImmediate.IsEnabled = false;
				}
				else if ((newUiState & UIState.ValidationResultsPhase) > 0)
				{
					SetUIVisibility(AreaForParseTree);

					MenuMain.IsEnabled = true;
					MenuExamples.IsEnabled = false;
				}
				else if ((newUiState & UIState.ConstructionPhase) > 0)
				{
					SetUIVisibility(AreaForMachineCreation);

					MenuMain.IsEnabled = true;
					MenuExamples.IsEnabled = false;

					if (newUiState == UIState.BusyConstructing)
					{
						OptionAbort.IsEnabled = true;
						OptionNextStep.IsEnabled = false;
						OptionFinalResult.IsEnabled = false;
						OptionLatex.IsEnabled = false;
						OptionEvaluate.IsEnabled = false;
					}
				}
				else if ((newUiState & UIState.ConstructionStepResultsPhase) > 0)
				{
					SetUIVisibility(AreaForMachineCreation);

					if (newUiState == UIState.ReadyForNextConstructionStep)
					{
						OptionAbort.IsEnabled = true;
						OptionNextStep.IsEnabled = true;
						OptionFinalResult.IsEnabled = true;
						OptionLatex.IsEnabled = false;
						OptionEvaluate.IsEnabled = false;
					}
				}
				else if ((newUiState & UIState.ConstructionResultsPhase) > 0)
				{
					SetUIVisibility(AreaForMachineCreation);

					if (newUiState == UIState.ReadyForEvaluation)
					{
						OptionAbort.IsEnabled = true;
						OptionNextStep.IsEnabled = false;
						OptionFinalResult.IsEnabled = false;
						OptionLatex.IsEnabled = true;
						OptionEvaluate.IsEnabled = true;
					}
					else if (newUiState == UIState.BusyGeneratingLatex)
					{
						OptionAbort.IsEnabled = false;
						OptionNextStep.IsEnabled = false;
						OptionFinalResult.IsEnabled = false;
						OptionLatex.IsEnabled = false;
						OptionEvaluate.IsEnabled = false;
					}
				}
				else if ((newUiState & UIState.LatexResultPhase) > 0)
				{
					SetUIVisibility(AreaForGeneratedLatex);

					if (newUiState == UIState.BusyGeneratingPdf)
					{
						OptionBackToInput.IsEnabled = false;
						OptionBackToFSM.IsEnabled = false;
						OptionGeneratePDF.IsEnabled = false;
					}
					else
					{
						OptionBackToInput.IsEnabled = true;
						OptionBackToFSM.IsEnabled = true;
						OptionGeneratePDF.IsEnabled = true;
					}
				}
				else if ((newUiState & UIState.WordInputPhase) > 0)
				{
					SetUIVisibility(AreaForWordInput);

					OptionStepByStepEvaluation.IsEnabled = false;
					OptionImmediateEvaluation.IsEnabled = false;
				}
				else if ((newUiState & UIState.EvaluationPhase) > 0)
				{
					SetUIVisibility(AreaForWordEvaluation);
				}
				else if ((newUiState & UIState.EvaluationResultsPhase) > 0)
				{
					SetUIVisibility(AreaForWordEvaluation);
				}

				if (overrideStatusText == null)
				{
					string newStatusText;
					if (UIStateInfo.Status.TryGetValue(newUiState, out newStatusText))
						StatusText = newStatusText;
					else
						StatusText = "no status description for " + newUiState.ToString();
				}
				else
					StatusText = overrideStatusText;
				uiState = newUiState;
			}
		}

		private void SetUIVisibility(UIElement visibleElem)
		{
			if (visibleElem == AreaForRegexpInput)
				AreaForRegexpInput.Visibility = Visibility.Visible;
			else
				AreaForRegexpInput.Visibility = Visibility.Hidden;

			if (visibleElem == AreaForParseTree)
				AreaForParseTree.Visibility = Visibility.Visible;
			else
				AreaForParseTree.Visibility = Visibility.Hidden;

			if (visibleElem == AreaForMachineCreation)
				AreaForMachineCreation.Visibility = Visibility.Visible;
			else
				AreaForMachineCreation.Visibility = Visibility.Hidden;

			if (visibleElem == AreaForGeneratedLatex)
				AreaForGeneratedLatex.Visibility = Visibility.Visible;
			else
				AreaForGeneratedLatex.Visibility = Visibility.Hidden;

			if (visibleElem == AreaForWordInput)
				AreaForWordInput.Visibility = Visibility.Visible;
			else
				AreaForWordInput.Visibility = Visibility.Hidden;

			if (visibleElem == AreaForWordEvaluation)
				AreaForWordEvaluation.Visibility = Visibility.Visible;
			else
				AreaForWordEvaluation.Visibility = Visibility.Hidden;
		}

		private void CallMethodInNewThread(ThreadStart method, string name)
		{
			Thread t = new Thread(method);
			t.Name = name + "Thread";
			//t.Priority = ThreadPriority.BelowNormal;
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
		}

		private void WindowInitializationWorker()
		{
			foreach (string key in Examples.Keys)
			{
				Dispatcher.BeginInvoke((Action)delegate
				{
					var item = new MenuItem();
					item.Header = key;
					item.Click += OptionExample_Click;
					MenuExamples.Items.Add(item);
				});
			}

			lock (regexpAndFsmLock)
			{
				// dummy calls to force loading dlls
				regexp = new RegularExpression("abc+def", true);
				fsm = new FiniteStateMachine(regexp, true);
				regexp = null;
				fsm = null;
			}

			SetUIState(UIState.ReadyForRegexpInput);
		}

		private void ValidationWorker()
		{
			try
			{
				lock (regexpAndFsmLock)
				{
					regexp = new RegularExpression(InputRegexpText);
					regexp.EvaluateInput();
				}
			}
			catch (ArgumentException e)
			{
				SetUIState(UIState.ReadyForNewInputAfterInvalidInput,
					String.Format("input is invalid ({0}); ready for new input", e.Message));
				return;
			}
			catch (Exception e)
			{
				new MessageFrame("Phinite/Regexp error", "Regular expression validation failed",
					String.Format("There was some unexpected error while evaluating the input: {0}.", e.Message)
					).ShowDialog();
				SetUIState(UIState.ReadyForNewInputAfterError);
				return;
			}

			lock (regexpAndFsmLock)
			{
				ValidatedRegexpText = regexp.ToString();
			}

			LabeledExpressionsData = null;
			TransitionsData = null;

			try
			{
				lock (regexpAndFsmLock)
				{
					if (regexp == null)
					{
						SetUIState(UIState.ReadyForNewInputAfterAbortedComputation);
						return;
					}
					fsm = new FiniteStateMachine(regexp);
				}
			}
			catch (Exception e)
			{
				new MessageFrame("Phinite/FSM error", "Finite-state machine was not created",
					String.Format("There was some unexpected error while creating the finite-state machine: {0}.", e.Message)
					).ShowDialog();
				SetUIState(UIState.ReadyForNewInputAfterError);
				return;
			}

			if (stepByStep)
			{
				Dispatcher.BeginInvoke((Action)delegate { ParseTreeCanvas.Children.Clear(); });
				DrawParseTree(regexp.ParseTree, 10, 10);

				SetUIState(UIState.ReadyForConstruction);
				return;
			}

			SetUIState(UIState.BusyConstructing);
			CallMethodInNewThread(ConstructionStepWorker, "ConstructionStep");
		}

		private void ConstructionStepWorker()
		{
			try
			{
				lock (regexpAndFsmLock)
				{
					if (fsm == null)
					{
						SetUIState(UIState.ReadyForNewInputAfterAbortedComputation);
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
				SetUIState(UIState.ReadyForNewInputAfterError);
				return;
			}

			CallMethodInNewThread(ConstructionStepResultsWorker, "ConstructionStepResults");
		}

		private void ConstructionStepResultsWorker()
		{
			//Thread.Sleep(500); // put intermediate results into AreaForIntermediateResult

			ReadOnlyCollection<RegularExpression> accepting = null;
			ReadOnlyCollection<RegularExpression> states = null;
			ReadOnlyCollection<MachineTransition> transitions = null;
			lock (regexpAndFsmLock)
			{
				if (fsm == null)
				{
					SetUIState(UIState.ReadyForNewInputAfterAbortedComputation);
					return;
				}

				accepting = fsm.AcceptingStates;
				states = fsm.States;
				transitions = fsm.Transitions;
			}

			var data = new List<Tuple<RegularExpression, string, string>>();
			int i = 0;
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
			foreach (var transition in transitions)
			{
				data2.Add(new Tuple<RegularExpression, string, string, string, RegularExpression>(
					states[transition.Item1], String.Format("q{0}", transition.Item1),
					String.Join(", ", transition.Item2),
					String.Format("q{0}", transition.Item3), states[transition.Item3]));
			}
			TransitionsData = data2;

			lock (regexpAndFsmLock)
			{
				if (fsm == null)
				{
					SetUIState(UIState.ReadyForNewInputAfterAbortedComputation);
					return;
				}
				if (fsm.IsConstructionFinished())
				{
					SetUIState(UIState.ReadyForEvaluation);
					return;
				}
			}

			if (stepByStep)
			{
				SetUIState(UIState.ReadyForNextConstructionStep);
				return;
			}

			SetUIState(UIState.BusyConstructing);
			CallMethodInNewThread(ConstructionStepWorker, "ConstructionStep");
		}

		private void DrawParseTree(PartialExpression parseTree, double x, double y)
		{
			Dispatcher.BeginInvoke((Action)delegate
			{
				//var e = new Ellipse();
				var elem = new TextBlock();
				elem.TextAlignment = TextAlignment.Center;
				elem.Width = 50;
				elem.Height = 15;

				if (parseTree.Role == PartialExpressionRole.EmptyWord)
					elem.Text = ".";
				else if (parseTree.Role == PartialExpressionRole.Letter)
					elem.Text = parseTree.Value;
				else if (parseTree.Role == PartialExpressionRole.Concatenation)
				{
					elem.Text = "concat";
					double yy = y;
					foreach (var part in parseTree.Parts)
					{
						DrawParseTree(part, x + elem.Width * 2, yy);

						var poly = new Polyline();
						poly.Stroke = Brushes.Black;
						poly.StrokeThickness = 1;
						poly.Points.Add(new Point(x + elem.Width + 2, y + elem.Height / 2));
						poly.Points.Add(new Point(x + elem.Width * 2 - 2, yy + elem.Height / 2));
						ParseTreeCanvas.Children.Add(poly);

						int width = part.CalculateTreeWidth();
						yy += (elem.Height + 5) * width;
					}
				}
				else if (parseTree.Role == PartialExpressionRole.Union)
				{
					elem.Text = "union";
					double yy = y;
					foreach (var part in parseTree.Parts)
					{
						DrawParseTree(part, x + elem.Width * 2, yy);

						var poly = new Polyline();
						poly.Stroke = Brushes.Black;
						poly.StrokeThickness = 1;
						poly.Points.Add(new Point(x + elem.Width + 2, y + elem.Height / 2));
						poly.Points.Add(new Point(x + elem.Width * 2 - 2, yy + elem.Height / 2));
						ParseTreeCanvas.Children.Add(poly);

						int width = part.CalculateTreeWidth();
						yy += (elem.Height + 5) * width;
					}
				}
				if (parseTree.Operator != UnaryOperator.None)
					elem.Text += RegularExpression.TagsStrings[(InputSymbolTag)parseTree.Operator];

				ParseTreeCanvas.Children.Add(elem);
				Canvas.SetLeft(elem, x);
				Canvas.SetTop(elem, y);

				var rect = new Rectangle();
				rect.Stroke = Brushes.Gray;
				rect.StrokeThickness = 1;
				rect.Width = elem.Width + 2;
				rect.Height = elem.Height + 2;

				ParseTreeCanvas.Children.Add(rect);
				Canvas.SetLeft(rect, x - 1);
				Canvas.SetTop(rect, y - 1);
			});
		}

		private void DrawSubParseTree(PartialExpression parseTree)
		{

			//ParseTreeCanvas.Children.Add();
		}

		private void LatexGenerationWorker()
		{
			RegularExpression r = null;
			FiniteStateMachine f = null;

			lock (regexpAndFsmLock)
			{
				if (regexp == null || fsm == null)
				{
					SetUIState(UIState.ReadyForNewInputAfterAbortedComputation);
					return;
				}
				r = regexp;
				f = fsm;
			}

			LatexOutputText = LatexWriter.GenerateFullLatex(inputRegexpText, r, f, true, true);
			SetUIState(UIState.ReadyForLatexProcessing);
		}

		private void PdfGenerationWorker()
		{
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
					SetUIState(UIState.PdfGenerationError);
					return;
				}

				if (!p.WaitForExit(30000))
				{
					new MessageFrame("Phinite/LaTeX error", "LaTeX timeout",
								"It takes more than 30 seconds to build PDF file, Phinite will not wait for this to finish. The PDF file will not be opened automatically, even if it is finally created."
								).ShowDialog();
					SetUIState(UIState.PdfGenerationError);
					return;
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
					SetUIState(UIState.PdfGenerationError);
					return;
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
				SetUIState(UIState.PdfGenerationError);
				return;
			}
			catch (ThreadStateException)
			{
				// silent catch
			}

			SetUIState(UIState.PdfGenerated);
			//Thread t = new Thread(LatexEnded);
			//t.Name = "LatexAndPdfGenerationEndingThread";
			//t.SetApartmentState(ApartmentState.STA);
			//t.Start();
		}

		#region main menu handlers

		private void OptionExample_Click(object sender, RoutedEventArgs e)
		{
			if (sender is HeaderedItemsControl == false)
				return;
			var item = (HeaderedItemsControl)sender;
			InputRegexpText = Examples[item.Header.ToString()];
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

		#region regexp input screen handlers

		private void InputTextChanged(object sender, TextChangedEventArgs e)
		{
			if (uiState == UIState.ReadyForNewInputAfterInvalidInput)
				SetUIState(UIState.ReadyForRegexpInput);
		}

		private void OptionImmediate_Click(object sender, RoutedEventArgs e)
		{
			SetUIState(UIState.ValidatingInputExpression);
			stepByStep = false;
			CallMethodInNewThread(ValidationWorker, "Validation");
		}

		private void OptionStepByStep_Click(object sender, RoutedEventArgs e)
		{
			SetUIState(UIState.ValidatingInputExpression);
			stepByStep = true;
			CallMethodInNewThread(ValidationWorker, "Validation");
		}

		#endregion

		#region fsm buttons handlers

		private void OptionAbort_Click(object sender, RoutedEventArgs e)
		{
			lock (regexpAndFsmLock)
			{
				fsm = null;
				regexp = null;
			}

			SetUIState(UIState.ReadyForNewInputAfterAbortedComputation);
		}

		private void OptionNextStep_Click(object sender, RoutedEventArgs e)
		{
			SetUIState(UIState.BusyConstructing);
			CallMethodInNewThread(ConstructionStepWorker, "ConstructionStep");
		}

		private void OptionFinalResult_Click(object sender, RoutedEventArgs e)
		{
			stepByStep = false;

			SetUIState(UIState.BusyConstructing);
			CallMethodInNewThread(ConstructionStepWorker, "ConstructionStep");
		}

		private void GenerateLatex_Click(object sender, RoutedEventArgs e)
		{
			SetUIState(UIState.BusyGeneratingLatex);
			CallMethodInNewThread(LatexGenerationWorker, "LatexGeneration");
		}

		private void OptionEvaluate_Click(object sender, RoutedEventArgs e)
		{
			SetUIState(UIState.ReadyForNewWord);
		}

		#endregion

		#region latex buttons handlers

		private void OptionGenerateAndViewPDF_Click(object sender, RoutedEventArgs e)
		{
			SetUIState(UIState.BusyGeneratingPdf);
			CallMethodInNewThread(PdfGenerationWorker, "PdfGeneration");
		}

		private void OptionBackToFSM_Click(object sender, RoutedEventArgs e)
		{
			SetUIState(UIState.ReadyForEvaluation);
		}

		#endregion

		#region word input screen handlers

		private void InputWord_TextChanged(object sender, TextChangedEventArgs e)
		{
			//if (uiState == UIState.ReadyForNewInputAfterInvalidInput)
			//	SetUIState(UIState.ReadyForRegexpInput);
		}

		private void OptionStepByStepEvaluation_Click(object sender, RoutedEventArgs e)
		{
			//SetUIState(UIState.ValidatingInputExpression);
			//stepByStep = true;
			//CallMethodInNewThread(ValidationWorker, "Validation");
		}

		private void OptionImmediateEvaluation_Click(object sender, RoutedEventArgs e)
		{
			//SetUIState(UIState.ValidatingInputExpression);
			//stepByStep = false;
			//CallMethodInNewThread(ValidationWorker, "Validation");
		}

		#endregion

		#region information messages

		private void Info_Input(object sender, RoutedEventArgs e)
		{
			var s = new StringBuilder();

			s.Append("Enter here an expression that is a valid regular expression.");
			s.Append(" You can use any symbol,\nbut remember that spaces will be ignored and some symbols have special meaning:");
			s.Append("\n\"");
			s.Append(RegularExpression.TagsStrings[InputSymbolTag.EmptyWord]);
			s.Append("\" - empty word\n\"");
			s.Append(RegularExpression.TagsStrings[InputSymbolTag.Union]);
			s.Append("\" - union\n\"");
			s.Append(RegularExpression.TagsStrings[InputSymbolTag.KleeneStar]);
			s.Append("\" - Kleene star\n\"");
			s.Append(RegularExpression.TagsStrings[InputSymbolTag.KleenePlus]);
			s.Append("\" - Kleene plus\n\"");
			s.Append(RegularExpression.TagsStrings[InputSymbolTag.OpeningParenthesis]);
			s.Append("\" and \"");
			s.Append(RegularExpression.TagsStrings[InputSymbolTag.ClosingParenthesis]);
			s.Append("\" -  parentheses\n\n");
			s.Append("If you are unsure, load one of the example expressions to see how it works.");

			new MessageFrame("Phinite information", "Regular expression input", s.ToString()).ShowDialog();
		}

		private void Info_InputWord(object sender, RoutedEventArgs e)
		{
			var s = new StringBuilder();

			s.Append("Enter a word.");
			s.Append(" You can use any symbols,\nbut remember that spaces will be ignored and some symbols are forbidden:");
			s.Append("\n");
			foreach (var symbol in RegularExpression.ForbiddenSymbols)
			{
				s.Append("\"");
				s.Append(symbol);
				s.Append("\"\n");
			}
			s.Append("\n");
			s.Append("Leave the field blank to evaluate (i.e. check if the machine accepts) the empty word.");
			s.Append("\n");
			s.Append("If you are unsure, just start computing without any input\n");
			s.Append("or write just a single letter to see how the basic case works.");

			new MessageFrame("Phinite information", "Regular expression input", s.ToString()).ShowDialog();
		}

		#endregion

		protected override void OnClosing(CancelEventArgs e)
		{
			SetUIState(UIState.Loading);

			lock (regexpAndFsmLock)
			{
				fsm = null;
				regexp = null;
			}

			base.OnClosing(e);
		}

	}
}
