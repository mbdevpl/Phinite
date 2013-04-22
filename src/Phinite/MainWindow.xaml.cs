﻿using System;
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
		public static Dictionary<string, string> Examples
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
				{"Example from old BA", "a^+c^+ + ab^+c"},
				{"Example from BA", "a(a+b)^*b"},
				{"Example from TA", "a^+b^+ + ab^+c"},
				{"High tree", "((((((((a^+b)^+c)^+d)^+e)^+f)^+g)^+i)^+j)^+k"},
				{"Hard 1", "(a+ab+abc+abcd+abcde+abcdef)^*"},
				{"Hard 2", "(a+.)^*b"},
				{"Hard 3", "(a(a+.)b^*)^*"},
				{"Hard 4", "(ab^*)^*"},
				{"Hard 5", "(((b)^*)((a((b)^*))^*))"},
				{"Harder", "(f+ef+def+cdef+bcdef+abcdef)^*"},
				{"Infinite loop", "(a^*a)^*"},
				{"Max processor use test", "0+(1+2+3+4+5+6+7+8+9)(((0+1+2+3+4+5+6+7+8+9)^*(0+1+2+3+4+5+6+7+8+9))^*(0+1+2+3+4+5+6+7+8+9))^*(0+1+2+3+4+5+6+7+8+9)^*"},
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

		private int pdflatexTimeout = 15;

		private bool useSystemDefaultPdfViewer
			//= false;
			= true;

		private string pdfViewerCommand = @"SumatraPDF\SumatraPDF.exe";

		private FiniteStateMachineLayout fsmLayout = null;

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

		public string ProcessedWordFragmentText
		{
			get { return processedWordFragmentText; }
			set
			{
				if (processedWordFragmentText == value)
					return;
				processedWordFragmentText = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("ProcessedWordFragmentText"));
			}
		}
		private string processedWordFragmentText = String.Empty;

		public string RemainingWordFragmentText
		{
			get { return remainingWordFragmentText; }
			set
			{
				if (remainingWordFragmentText == value)
					return;
				remainingWordFragmentText = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("RemainingWordFragmentText"));
			}
		}
		private string remainingWordFragmentText = String.Empty;

		public string CurrentStateText
		{
			get { return currentStateText; }
			set
			{
				if (currentStateText == value)
					return;
				currentStateText = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("CurrentStateText"));
			}
		}
		private string currentStateText = String.Empty;

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

					OptionStepByStepEvaluation.IsEnabled = true;
					OptionImmediateEvaluation.IsEnabled = true;
				}
				else if ((newUiState & UIState.EvaluationPhase) > 0)
				{
					SetUIVisibility(AreaForWordEvaluation);

					if (newUiState == UIState.BusyEvaluating)
					{
						OptionEvalAbort.IsEnabled = true;
						OptionEvalNextStep.IsEnabled = false;
						OptionEvalFinalResult.IsEnabled = false;
						OptionEvalAgain.IsEnabled = false;
						OptionEvalFinalize.IsEnabled = false;
					}
					else if (newUiState == UIState.ReadyForNextEvaluationStep)
					{
						OptionEvalAbort.IsEnabled = true;
						OptionEvalNextStep.IsEnabled = true;
						OptionEvalFinalResult.IsEnabled = true;
						OptionEvalAgain.IsEnabled = true;
						OptionEvalFinalize.IsEnabled = false;
					}
				}
				else if ((newUiState & UIState.EvaluationResultsPhase) > 0)
				{
					SetUIVisibility(AreaForWordEvaluation);

					if (newUiState == UIState.WordWasAccepted || newUiState == UIState.WordWasRejected)
					{
						OptionEvalAbort.IsEnabled = false;
						OptionEvalNextStep.IsEnabled = false;
						OptionEvalFinalResult.IsEnabled = false;
						OptionEvalAgain.IsEnabled = true;
						OptionEvalFinalize.IsEnabled = true;
					}
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
			t.Priority = ThreadPriority.Lowest;
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
		}

		private bool CheckIfComputationAbortedAndDealWithIt(object obj)
		{
			if (obj != null)
				return false;

			regexp = null;
			fsm = null;

			SetUIState(UIState.ReadyForNewInputAfterAbortedComputation);
			return true;
		}

		private bool CheckIfComputationAbortedAndDealWithIt(object obj1, object obj2)
		{
			if (obj1 != null && obj2 != null)
				return false;

			regexp = null;
			fsm = null;

			SetUIState(UIState.ReadyForNewInputAfterAbortedComputation);
			return true;
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

			{
				// dummy calls to force dll load
				var a1 = new RegularExpression("abc+def", true);
				var a2 = new FiniteStateMachine(a1, true);
				var a3 = new FiniteStateMachineLayout(a2);
				a3.Create();

				// System.Xml.Linq
				System.Xml.Linq.Extensions.Equals(a1, a2);

				//WindowBase, PresentationCore, PresentationFramework
				System.Diagnostics.Trace.WriteLine("WindowBase");
				System.Windows.Media.PointCollection a4 = new PointCollection();
				System.Windows.Shapes.Line a5 = new Line();
			}

			lock (regexpAndFsmLock)
			{
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
				new MessageFrame(this, "Phinite/Regexp error", "Regular expression validation failed",
					String.Format("There was some unexpected error while evaluating the input: {0}.", e.Message)
					).ShowDialog();
				SetUIState(UIState.ReadyForNewInputAfterError);
				return;
			}

			lock (regexpAndFsmLock)
			{
				if (CheckIfComputationAbortedAndDealWithIt(regexp))
					return;

				ValidatedRegexpText = regexp.ToString();
			}

			LabeledExpressionsData = null;
			TransitionsData = null;

			try
			{
				lock (regexpAndFsmLock)
				{
					if (CheckIfComputationAbortedAndDealWithIt(regexp))
						return;

					fsm = new FiniteStateMachine(regexp);
				}
			}
			catch (Exception e)
			{
				new MessageFrame(this, "Phinite/FSM error", "Finite-state machine was not created",
					String.Format("There was some unexpected error while creating the finite-state machine: {0}.", e.Message)
					).ShowDialog();
				SetUIState(UIState.ReadyForNewInputAfterError);
				return;
			}

			if (stepByStep)
			{
				Dispatcher.BeginInvoke((Action)delegate { ParseTreeDrawing.Draw(ParseTreeCanvas, regexp.ParseTree); });

				SetUIState(UIState.ReadyForConstruction);
				return;
			}

			Dispatcher.BeginInvoke((Action)delegate
			{
				foreach (DataGridColumn column in DataGridForStates.Columns)
					column.Width = DataGridLength.SizeToHeader;
				foreach (DataGridColumn column in DataGridForTransitions.Columns)
					column.Width = DataGridLength.SizeToHeader;
				foreach (DataGridColumn column in DataGridForStates.Columns)
					column.Width = DataGridLength.Auto;
				foreach (DataGridColumn column in DataGridForTransitions.Columns)
					column.Width = DataGridLength.Auto;
			});

			SetUIState(UIState.BusyConstructing);
			CallMethodInNewThread(ConstructionStepWorker, "ConstructionStep");
		}

		private void ConstructionStepWorker()
		{
			try
			{
				lock (regexpAndFsmLock)
				{
					if (CheckIfComputationAbortedAndDealWithIt(fsm))
						return;

					fsm.Construct(1);
				}
			}
			catch (Exception e)
			{
				new MessageFrame(this, "Phinite/FSM error", "Finite-state machine was not created",
					String.Format("There was some unexpected error while creating the finite-state machine: {0}.", e.Message)
					).ShowDialog();
				SetUIState(UIState.ReadyForNewInputAfterError);
				return;
			}

			CallMethodInNewThread(ConstructionStepResultsWorker, "ConstructionStepResults");
		}

		/// <summary>
		/// Puts intermediate results into AreaForMachineCreation.
		/// </summary>
		private void ConstructionStepResultsWorker()
		{
			ReadOnlyCollection<RegularExpression> accepting = null;
			ReadOnlyCollection<RegularExpression> states = null;
			ReadOnlyCollection<MachineTransition> transitions = null;
			ReadOnlyCollection<RegularExpression> latestStates = null;
			ReadOnlyCollection<MachineTransition> latestTransitions = null;
			FiniteStateMachineLayout layout = null;
			lock (regexpAndFsmLock)
			{
				if (CheckIfComputationAbortedAndDealWithIt(fsm))
					return;

				accepting = fsm.AcceptingStates;
				states = fsm.States;
				transitions = fsm.Transitions;

				latestStates = fsm.LatestStates;
				latestTransitions = fsm.LatestTransitions;

				layout = new FiniteStateMachineLayout(fsm);
			}

			layout.Create();

			Dispatcher.BeginInvoke((Action)delegate
			{
				lock (regexpAndFsmLock)
				{
					if (CheckIfComputationAbortedAndDealWithIt(fsm))
						return;
				}
				layout.Draw(ConstructedMachineCanvas, latestStates, latestTransitions);
			});

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
				if (CheckIfComputationAbortedAndDealWithIt(fsm))
					return;

				if (fsm.IsConstructionFinished())
				{
					fsmLayout = layout;

					Dispatcher.BeginInvoke((Action)delegate
					{
						lock (regexpAndFsmLock)
						{
							if (CheckIfComputationAbortedAndDealWithIt(fsm))
								return;
						}
						// draw the fsm behind word input controls
						layout.Draw(WordInputBackgroundCanvas);
					});

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

		private void EvaluationStepWorker()
		{
			bool finished = false;
			int previous = -1;
			int state = -1;

			lock (regexpAndFsmLock)
			{
				if (CheckIfComputationAbortedAndDealWithIt(fsm))
					return;

				if (fsm.IsEvaluationFinished())
				{
					// just begin evaluation, because otherwise user will never see
					// the very 1st step
					fsm.BeginEvaluation(InputWordText);
				}
				else if (stepByStep)
					fsm.Evaluate(1);

				if (!stepByStep)
					fsm.Evaluate(0);

				finished = fsm.IsEvaluationFinished();
				previous = fsm.PreviousState;
				state = fsm.CurrentState;

				// update strings
				ProcessedWordFragmentText = fsm.EvaluatedWordProcessedFragment;
				RemainingWordFragmentText = fsm.EvaluatedWordRemainingFragment;
			}

			// update info about current state
			var s = new StringBuilder("q");
			if (state >= 0)
				s.Append(state);
			else
				s.Append("R"); // rejecting state
			CurrentStateText = s.ToString();

			// draw machine
			Dispatcher.BeginInvoke((Action)delegate
			{
				lock (regexpAndFsmLock)
				{
					if (CheckIfComputationAbortedAndDealWithIt(fsm))
						return;
				}
				fsmLayout.Draw(WordEvaluationCanvas, previous, state, finished);
			});

			if (finished)
			{
				if (state >= 0)
					SetUIState(UIState.WordWasAccepted);
				else
					SetUIState(UIState.WordWasRejected);
				return;
			}

			SetUIState(UIState.ReadyForNextEvaluationStep);
		}

		private void LatexGenerationWorker()
		{
			RegularExpression r = null;
			FiniteStateMachine f = null;

			lock (regexpAndFsmLock)
			{
				if (CheckIfComputationAbortedAndDealWithIt(regexp, fsm))
					return;

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
			string latexOptions = new StringBuilder()
				.Append(@"-shell-escape ")
				.Append(@"-interaction=batchmode ")
				//.Append(@" -interaction=nonstopmode")
				.Append('"').Append(generatedTexDir).Append('/').Append(generatedTex).Append('"').ToString();

			Process p = new Process();
			p.StartInfo = new ProcessStartInfo(latexExecutable, latexOptions);
			try
			{
				if (!p.Start())
				{
					new MessageFrame(this, "Phinite/LaTeX error", "Error while starting LaTeX",
						String.Format("Unable to start LaTeX with this command:\n\n{0} {1}", latexExecutable, latexOptions)
						).ShowDialog();
					SetUIState(UIState.PdfGenerationError);
					return;
				}

				bool userWaitsForTimeout = true;
				var timeoutMessageFormat = new StringBuilder()
					.Append("It takes more than {0} seconds to build PDF file.")
					.Append(" Do you want Phinite to wait for another {1} seconds to finish?")
					.Append(" If not, the PDF file will not be opened automatically, even when it is finally created.")
					.ToString();
				while (userWaitsForTimeout)
				{
					if (p.WaitForExit(1000 * pdflatexTimeout))
						break;

					if (new MessageFrame(this, "Phinite/LaTeX error", "LaTeX timeout",
							String.Format(timeoutMessageFormat, pdflatexTimeout, pdflatexTimeout),
							null, true, true, false, "Yes", "No").ShowDialog() == true)
						continue;

					userWaitsForTimeout = false;
					SetUIState(UIState.PdfGenerationError);
					return;
				}

				if (p.ExitCode != 0)
				{
					if (File.Exists(generatedPdf))
						new MessageFrame(this, "Phinite/LaTeX warning", "Minor errors in LaTeX execution",
							"PDF file was created, but there were some errors and the result may not look as good as expected."
							).ShowDialog();
					else
					{
						new MessageFrame(this, "Phinite/LaTeX error", "Severe errors in LaTeX execution",
							"LaTeX failed to create the PDF file due to some critical errors. Read log to diagnose a problem."
							).ShowDialog();
						SetUIState(UIState.PdfGenerationError);
						return;
					}
				}
				else
				{
					try { File.Delete(filename + ".log"); }
					catch (IOException) { }
					try { File.Delete(filename + ".aux"); }
					catch (IOException) { }
				}

				if (useSystemDefaultPdfViewer)
					Process.Start(generatedPdf);
				else
					Process.Start(pdfViewerCommand, generatedPdf);
			}
			catch (Win32Exception)
			{
				new MessageFrame(this, "Phinite/LaTeX/PDF error", "Error in Phinite configuration",
							String.Format("There is no LaTeX executable at this path:\n\n{0}\n\nAnd/or there is no PDF viewer at this path:\n\n{1}", pdflatexCommand, pdfViewerCommand)
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

		private void OptionSettings_Click(object sender, RoutedEventArgs e)
		{
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

			new MessageFrame(this, "About Phinite", "Information about Phinite", s.ToString(), PhiImage.Source).ShowDialog();
		}

		private void OptionViewBA_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Process.Start("bysiekm-business-analysis.pdf");
			}
			catch (Win32Exception)
			{
				new MessageFrame(this, "Phinite", "Missing content", "business analysis file was not found").ShowDialog();
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
				new MessageFrame(this, "Phinite", "Missing content", "technical analysis file was not found").ShowDialog();
			}
		}

		private void OptionViewUserGuide_Click(object sender, RoutedEventArgs e)
		{
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
			stepByStep = false;
			SetUIState(UIState.ValidatingInputExpression);
			CallMethodInNewThread(ValidationWorker, "Validation");
		}

		private void OptionStepByStep_Click(object sender, RoutedEventArgs e)
		{
			stepByStep = true;
			SetUIState(UIState.ValidatingInputExpression);
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
			OptionNextStep_Click(sender, e);
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

		#region word input and evaluation screens handlers

		private void InputWord_TextChanged(object sender, TextChangedEventArgs e)
		{
			//if (uiState == UIState.ReadyForNewInputAfterInvalidInput)
			//	SetUIState(UIState.ReadyForRegexpInput);
		}

		private void OptionStepByStepEvaluation_Click(object sender, RoutedEventArgs e)
		{
			stepByStep = true;
			OptionEvalNextStep_Click(sender, e);
		}

		private void OptionImmediateEvaluation_Click(object sender, RoutedEventArgs e)
		{
			stepByStep = false;
			OptionEvalNextStep_Click(sender, e);
		}

		private void OptionEvalNextStep_Click(object sender, RoutedEventArgs e)
		{
			SetUIState(UIState.BusyEvaluating);
			CallMethodInNewThread(EvaluationStepWorker, "Evaluation");
		}

		private void OptionEvalAgain_Click(object sender, RoutedEventArgs e)
		{
			SetUIState(UIState.ReadyForNewWord);
		}

		private void OptionEvalFinalResult_Click(object sender, RoutedEventArgs e)
		{
			stepByStep = false;
			OptionEvalNextStep_Click(sender, e);
		}

		private void OptionEvalFinalize_Click(object sender, RoutedEventArgs e)
		{
			SetUIState(UIState.ReadyForRegexpInput);
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

			new MessageFrame(this, "Phinite information", "Regular expression input", s.ToString()).ShowDialog();
		}

		private void Info_InputWord(object sender, RoutedEventArgs e)
		{
			var s = new StringBuilder();

			s.Append("Enter some word.");
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

			new MessageFrame(this, "Phinite information", "Word input", s.ToString()).ShowDialog();
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
