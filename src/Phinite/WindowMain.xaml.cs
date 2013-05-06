using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Phinite
{
	/// <summary>
	/// Main frame of Phinite.
	/// 
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class WindowMain : Window, INotifyPropertyChanged
	{
		private PhiniteSettings settings;

		public event PropertyChangedEventHandler PropertyChanged;

		private object regexpAndFsmLock = new object();
		private RegularExpression regexp;
		private FiniteStateMachine fsm;

		private object uiStateLock = new object();
		private UIState uiState;

		private int computationSessionId;
		private int thisComputationSessionId;

		private bool stepByStep;

		/// <summary>
		/// Text visible in the status bar.
		/// </summary>
		public string StatusText
		{ get { return statusText; } set { this.ChangeProperty(PropertyChanged, ref statusText, value, "StatusText"); } }
		private string statusText = String.Empty;

		#region fields for fsm construction phase

		private String currentExample = App.DefaultExample;

		/// <summary>
		/// Plain text input to be converted to a regular expression
		/// </summary>
		public string InputRegexpText
		{ get { return inputRegexpText; } set { this.ChangeProperty(PropertyChanged, ref inputRegexpText, value, "InputRegexpText"); } }
		private string inputRegexpText = App.ExpressionExamples[App.DefaultExample];

		/// <summary>
		/// Plain text that represents a preprocessed (validated and optimized) regular expression.
		/// </summary>
		public string ValidatedRegexpText
		{ get { return validatedRegexpText; } set { this.ChangeProperty(PropertyChanged, ref validatedRegexpText, value, "ValidatedRegexpText"); } }
		private string validatedRegexpText = String.Empty;

		private int layoutAge;
		private FiniteStateMachineLayout fsmLayout;

		public List<Tuple<RegularExpression, string, string>> LabeledExpressionsData
		{ get { return labeledExpressionsData; } set { this.ChangeProperty(PropertyChanged, ref labeledExpressionsData, value, "LabeledExpressionsData"); } }
		private List<Tuple<RegularExpression, string, string>> labeledExpressionsData;

		public List<Tuple<RegularExpression, string, string, string, RegularExpression>> TransitionsData
		{ get { return transitionsData; } set { this.ChangeProperty(PropertyChanged, ref transitionsData, value, "TransitionsData"); } }
		private List<Tuple<RegularExpression, string, string, string, RegularExpression>> transitionsData;

		public string LatexOutputText
		{ get { return latexOutputText; } set { this.ChangeProperty(PropertyChanged, ref latexOutputText, value, "LatexOutputText"); } }
		private string latexOutputText = "";

		public int StatesLeftCount
		{ get { return statesLeftCount; } set { this.ChangeProperty(PropertyChanged, ref statesLeftCount, ref value, "StatesLeftCount"); } }
		private int statesLeftCount;

		public int TransitionsLeftCount
		{ get { return transitionsLeftCount; } set { this.ChangeProperty(PropertyChanged, ref transitionsLeftCount, ref value, "TransitionsLeftCount"); } }
		private int transitionsLeftCount;

		public int StatesLabeledCount
		{ get { return statesLabeledCount; } set { this.ChangeProperty(PropertyChanged, ref statesLabeledCount, ref value, "StatesLabeledCount"); } }
		private int statesLabeledCount;

		public int TransitionsLabeledCount
		{ get { return transitionsLabeledCount; } set { this.ChangeProperty(PropertyChanged, ref transitionsLabeledCount, ref value, "TransitionsLabeledCount"); } }
		private int transitionsLabeledCount;

		#endregion

		#region fields for word evaluation phase

		/// <summary>
		/// Plain text to be evaluated as an input word for a finite-state machine.
		/// </summary>
		public string InputWordText
		{ get { return inputWordText; } set { this.ChangeProperty(PropertyChanged, ref inputWordText, value, "InputWordText"); } }
		private string inputWordText = App.WordExamples[App.DefaultExample];

		public string ProcessedWordFragmentText
		{ get { return processedWordFragmentText; } set { this.ChangeProperty(PropertyChanged, ref processedWordFragmentText, value, "ProcessedWordFragmentText"); } }
		private string processedWordFragmentText = String.Empty;

		public string RemainingWordFragmentText
		{ get { return remainingWordFragmentText; } set { this.ChangeProperty(PropertyChanged, ref remainingWordFragmentText, value, "RemainingWordFragmentText"); } }
		private string remainingWordFragmentText = String.Empty;

		public string CurrentStateText
		{ get { return currentStateText; } set { this.ChangeProperty(PropertyChanged, ref currentStateText, value, "CurrentStateText"); } }
		private string currentStateText = String.Empty;

		#endregion

		public WindowMain()
		{
			computationSessionId = 0;
			layoutAge = 0;

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
					OptionSettings.IsEnabled = true;

					switch (newUiState)
					{
						case UIState.ReadyForNewInputAfterInvalidInput:
							{
								InvalidExpressionBorder.BorderBrush = Brushes.Red;
								OptionStepByStep.IsEnabled = false;
								OptionImmediate.IsEnabled = false;
							} break;
						default:
							{
								InvalidExpressionBorder.BorderBrush = Brushes.Transparent;
								OptionStepByStep.IsEnabled = true;
								OptionImmediate.IsEnabled = true;
							} break;
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

					switch (newUiState)
					{
						case UIState.BusyConstructing:
							{
								OptionSettings.IsEnabled = false;

								OptionAbort.IsEnabled = true;
								OptionNextStep.IsEnabled = false;
								OptionFinalResult.IsEnabled = false;
								OptionLatex.IsEnabled = false;
								OptionEvaluate.IsEnabled = false;
							} break;
						case UIState.WaitingForUserHelp:
							{
								OptionSettings.IsEnabled = false;

								OptionAbort.IsEnabled = false;
								OptionNextStep.IsEnabled = false;
								OptionFinalResult.IsEnabled = false;
								OptionLatex.IsEnabled = false;
								OptionEvaluate.IsEnabled = false;
							} break;
						default:
							{
								OptionSettings.IsEnabled = true;
							} break;
					}
				}
				else if ((newUiState & UIState.ConstructionStepResultsPhase) > 0)
				{
					SetUIVisibility(AreaForMachineCreation);

					if (newUiState == UIState.ReadyForNextConstructionStep)
					{
						OptionSettings.IsEnabled = true;

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

					switch (newUiState)
					{
						case UIState.ReadyForEvaluation:
							{
								OptionSettings.IsEnabled = true;

								OptionAbort.IsEnabled = true;
								OptionNextStep.IsEnabled = false;
								OptionFinalResult.IsEnabled = false;
								OptionLatex.IsEnabled = true;
								OptionEvaluate.IsEnabled = true;
							} break;
						case UIState.BusyGeneratingLatex:
							{
								OptionSettings.IsEnabled = false;

								OptionAbort.IsEnabled = false;
								OptionNextStep.IsEnabled = false;
								OptionFinalResult.IsEnabled = false;
								OptionLatex.IsEnabled = false;
								OptionEvaluate.IsEnabled = false;
							} break;
					}
				}
				else if ((newUiState & UIState.LatexResultPhase) > 0)
				{
					SetUIVisibility(AreaForGeneratedLatex);

					switch (newUiState)
					{
						case UIState.BusyGeneratingPdf:
							{
								OptionSettings.IsEnabled = false;

								OptionBackToInput.IsEnabled = false;
								OptionBackToFSM.IsEnabled = false;
								OptionGeneratePDF.IsEnabled = false;
								OptionEvaluateFromLatex.IsEnabled = false;
							} break;
						default:
							{
								OptionSettings.IsEnabled = true;

								OptionBackToInput.IsEnabled = true;
								OptionBackToFSM.IsEnabled = true;
								OptionGeneratePDF.IsEnabled = true;
								OptionEvaluateFromLatex.IsEnabled = true;
							} break;
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

					switch (newUiState)
					{
						case UIState.BusyEvaluating:
							{
								OptionSettings.IsEnabled = false;

								OptionEvalAbort.IsEnabled = true;
								OptionEvalNextStep.IsEnabled = false;
								OptionEvalFinalResult.IsEnabled = false;
								OptionEvalAgain.IsEnabled = false;
								OptionEvalFinalize.IsEnabled = false;
							} break;
						case UIState.ReadyForNextEvaluationStep:
							{
								OptionSettings.IsEnabled = true;

								OptionEvalAbort.IsEnabled = true;
								OptionEvalNextStep.IsEnabled = true;
								OptionEvalFinalResult.IsEnabled = true;
								OptionEvalAgain.IsEnabled = true;
								OptionEvalFinalize.IsEnabled = false;
							} break;
					}
				}
				else if ((newUiState & UIState.EvaluationResultsPhase) > 0)
				{
					SetUIVisibility(AreaForWordEvaluation);

					MenuMain.IsEnabled = true;
					MenuExamples.IsEnabled = false;

					switch (newUiState)
					{
						case UIState.WordWasAccepted:
						case UIState.WordWasRejected:
							{
								OptionSettings.IsEnabled = true;

								OptionEvalAbort.IsEnabled = false;
								OptionEvalNextStep.IsEnabled = false;
								OptionEvalFinalResult.IsEnabled = false;
								OptionEvalAgain.IsEnabled = true;
								OptionEvalFinalize.IsEnabled = true;
							} break;
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

		private bool CheckIfComputationAbortedAndDealWithIt(int checkedSessionId, params object[] objectsThatMustNotBeNull)
		{
			bool allNotNull = true;
			foreach (var o in objectsThatMustNotBeNull)
				if (o == null)
				{
					allNotNull = false;
					break;
				}

			if (computationSessionId == checkedSessionId && allNotNull)
				return false;

			if (thisComputationSessionId != computationSessionId)
			{
				thisComputationSessionId = 0;
				regexp = null;
				fsm = null;

				SetUIState(UIState.ReadyForNewInputAfterAbortedComputation);
			}
			return true;
		}

		private void WindowInitializationWorker()
		{
			Dispatcher.BeginInvoke((Action)delegate
			{
				foreach (var example in App.ExpressionExamples)
				{
					var item = new MenuItem();
					item.Header = String.Format("{0}, \"{1}\"", example.Key, example.Value);
					item.Click += OptionExample_Click;
					MenuExamples.Items.Add(item);
				}
			});

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
			int sessionId = 0;
			try
			{
				lock (regexpAndFsmLock)
				{
					thisComputationSessionId = ++computationSessionId;
					sessionId = thisComputationSessionId;

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
				ShowMessageFrame("Phinite/Regexp error", "Regular expression validation failed",
					String.Format("There was some unexpected error while evaluating the input: {0}."
					+ " The error is not caused by the fact that the expression is invalid.", e.Message),
					true);
				SetUIState(UIState.ReadyForNewInputAfterError);
				return;
			}

			lock (regexpAndFsmLock)
			{
				if (CheckIfComputationAbortedAndDealWithIt(sessionId, regexp))
					return;

				ValidatedRegexpText = regexp.ToString();
			}

			LabeledExpressionsData = null;
			TransitionsData = null;

			if (currentExample == null || !inputRegexpText.Equals(App.ExpressionExamples[currentExample]))
			{
				currentExample = null;
				InputWordText = String.Empty;
			}

			try
			{
				lock (regexpAndFsmLock)
				{
					if (CheckIfComputationAbortedAndDealWithIt(sessionId, regexp))
						return;

					fsm = new FiniteStateMachine(regexp);

					layoutAge = settings.LayoutCreationFrequency;
				}
			}
			catch (Exception e)
			{
				ShowMessageFrame("Phinite/FSM error", "Finite-state machine was not created",
					String.Format("There was some unexpected error while creating the finite-state machine: {0}.", e.Message),
					true);
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

		/// <summary>
		/// Performs a single step of fsm construction, and then
		/// puts intermediate results into AreaForMachineCreation.
		/// 
		/// </summary>
		private void ConstructionStepWorker()
		{
			int sessionId = 0;
			lock (regexpAndFsmLock)
			{
				sessionId = thisComputationSessionId;
			}
			bool constructionStepResult = false;
			try
			{
				lock (regexpAndFsmLock)
				{
					if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
						return;

					constructionStepResult = fsm.Construct(1, !settings.EnableAutoResolutionMode);
				}
			}
			catch (Exception e)
			{
				ShowMessageFrame("Phinite/FSM error", "Finite-state machine was not created",
					String.Format("There was some unexpected error while creating the finite-state machine: {0}.", e.Message),
					true);
				SetUIState(UIState.ReadyForNewInputAfterError);
				return;
			}

			if (!ShowCurrentConstructionState(sessionId))
				return; // cannot show construction state, abort

			if (constructionStepResult == false)
			{
				WindowUserHelp windowUserHelp = null;

				Dispatcher.Invoke((Action)delegate
				{
					lock (regexpAndFsmLock)
					{
						if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
							return;

						windowUserHelp = new WindowUserHelp(regexpAndFsmLock, fsm);
					}

					windowUserHelp.Owner = this;
					windowUserHelp.Closed += WindowUserHelp_Closed;

					SetUIState(UIState.WaitingForUserHelp);

					windowUserHelp.Show();
				});

				return;
			}

			lock (regexpAndFsmLock)
			{
				if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
					return;

				if (fsm.IsConstructionFinished())
				{
					//fsmLayout = layout;

					Dispatcher.BeginInvoke((Action)delegate
					{
						lock (regexpAndFsmLock)
						{
							if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
								return;
						}
						// draw the fsm behind word input controls
						fsmLayout.Draw(WordInputBackgroundCanvas);
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

		/// <summary>
		/// Return value indicates whether the state is shown or not.
		/// </summary>
		/// <returns>false if the construction process was interrupted,
		/// true if the current construction state was correctly shown</returns>
		private bool ShowCurrentConstructionState(int sessionId)
		{
			ReadOnlyCollection<RegularExpression> accepting = null;
			ReadOnlyCollection<RegularExpression> states = null;
			ReadOnlyCollection<MachineTransition> transitions = null;
			ReadOnlyCollection<RegularExpression> latestStates = null;
			ReadOnlyCollection<MachineTransition> latestTransitions = null;
			FiniteStateMachineLayout layout = null;
			bool constructionFinished = false;
			bool layoutIsTooOld = false;
			lock (regexpAndFsmLock)
			{
				if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
					return false;

				accepting = fsm.AcceptingStates;
				states = fsm.States;
				transitions = fsm.Transitions;

				constructionFinished = fsm.IsConstructionFinished();
				layoutIsTooOld = layoutAge == settings.LayoutCreationFrequency;

				if (stepByStep || layoutIsTooOld || constructionFinished)
				{

					latestStates = fsm.LatestStates;
					latestTransitions = fsm.LatestTransitions;

					// draw new layout only if it differs from the last one
					if (latestStates.Count != 0 || latestTransitions.Count != 0)
					{
						layout = new FiniteStateMachineLayout(fsm);
						fsmLayout = layout;
					}
				}
			}

			//do not create everytime in case of "immediate solution", rather every N steps
			if (stepByStep || layoutIsTooOld || constructionFinished)
			{
				//TODO: abort layout creation on computation abort
				if (layout == null)
					layout = fsmLayout;
				else
					layout.Create();

				Dispatcher.BeginInvoke((Action)delegate
				{
					lock (regexpAndFsmLock)
					{
						if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
							return;
					}
					layout.Draw(ConstructedMachineCanvas, latestStates, latestTransitions);
				});
			}

			// layoutAge access must also be synchronized due to case when:
			//  user aborts computation while the layout is being created
			if (!stepByStep)
			{
				lock (regexpAndFsmLock)
				{
					if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
						return false;

					if (layoutAge < settings.LayoutCreationFrequency)
						++layoutAge;
					else
						layoutAge = 1;
				}
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
				if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
					return false;

				StatesLeftCount = fsm.RemainingStatesCount;
				TransitionsLeftCount = fsm.RemainingTransitionsCount;
				StatesLabeledCount = fsm.LabeledStatesCount;
				TransitionsLabeledCount = fsm.LabeledTransitionsCount;
			}

			return true;
		}

		private void EvaluationStepWorker()
		{
			int sessionId = 0;
			lock (regexpAndFsmLock)
			{
				sessionId = thisComputationSessionId;
			}
			bool finished = false;
			int previous = -1;
			int state = -1;

			lock (regexpAndFsmLock)
			{
				if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
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
					if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
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
			int sessionId = 0;
			lock (regexpAndFsmLock)
			{
				sessionId = thisComputationSessionId;
			}
			RegularExpression r = null;
			FiniteStateMachine f = null;

			lock (regexpAndFsmLock)
			{
				if (CheckIfComputationAbortedAndDealWithIt(sessionId, regexp, fsm))
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

			string latexExecutable = settings.Pdflatex;
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
					ShowMessageFrame("Phinite/LaTeX error", "Error while starting LaTeX",
						String.Format("Unable to start LaTeX with this command:\n\n{0} {1}", latexExecutable, latexOptions),
						true);
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
					if (p.WaitForExit(1000 * settings.PdflatexTimeout))
						break;

					Dispatcher.Invoke((Action)delegate
					{
						var dialog = new MessageFrame(this, "Phinite/LaTeX error", "LaTeX timeout",
								String.Format(timeoutMessageFormat, settings.PdflatexTimeout, settings.PdflatexTimeout),
								null, true, true, false, "Yes", "No");
						if (dialog.ShowDialog() != true)
							userWaitsForTimeout = false;
					});

					if (userWaitsForTimeout)
						continue;

					SetUIState(UIState.PdfGenerationTimeout);
					return;
				}

				if (p.ExitCode != 0)
				{
					if (File.Exists(generatedPdf))
						ShowMessageFrame("Phinite/LaTeX warning", "Minor errors in LaTeX execution",
							"PDF file was created, but there were some errors and the result may not look as good as expected.",
							true);
					else
					{
						ShowMessageFrame("Phinite/LaTeX error", "Severe errors in LaTeX execution",
							"LaTeX failed to create the PDF file due to some critical errors. Read log to diagnose a problem.",
							true);
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

				if (settings.PdfViewer.Length > 0)
					Process.Start(settings.PdfViewer, generatedPdf);
				else
					Process.Start(generatedPdf);
			}
			catch (Win32Exception)
			{
				ShowMessageFrame("Phinite/LaTeX/PDF error", "Error in Phinite configuration",
								String.Format("There is no LaTeX executable at this path:\n\n{0}\n\n"
									+ "And/or there is no PDF viewer at this path:\n\n{1}",
									settings.Pdflatex, settings.PdfViewer), true);
				SetUIState(UIState.PdfGenerationError);
				return;
			}
			catch (ThreadStateException)
			{
				// silent catch
			}

			SetUIState(UIState.PdfGenerated);
		}

		private bool? ShowMessageFrame(string windowTitle, string messageTitle, string messageText, bool inNewThread)
		{
			if (inNewThread)
			{
				bool? result = null;
				Dispatcher.Invoke((Action)delegate
				{
					result = new MessageFrame(this, windowTitle, messageTitle, messageText).ShowDialog();
				});
				return result;
			}
			else
				return new MessageFrame(this, windowTitle, messageTitle, messageText).ShowDialog();
		}

		#region main menu handlers

		private void OptionExample_Click(object sender, RoutedEventArgs e)
		{
			if (sender is HeaderedItemsControl == false)
				return;
			var item = (HeaderedItemsControl)sender;
			var itemHeaderString = item.Header.ToString();

			currentExample = itemHeaderString.Substring(0, itemHeaderString.IndexOf(", \""));
			InputRegexpText = App.ExpressionExamples[currentExample];
			InputWordText = App.WordExamples[currentExample];
		}

		private void OptionSettings_Click(object sender, RoutedEventArgs e)
		{
			var backup = new PhiniteSettings(settings);
			var s = new WindowSettings(settings);
			s.Owner = this;
			if (s.ShowDialog() != true)
				settings = backup;
		}

		private void OptionAbout_Click(object sender, RoutedEventArgs e)
		{
			StringBuilder s = new StringBuilder();

			s.AppendLine("Implemented in WPF by Mateusz Bysiek.");
			s.AppendLine();

			s.AppendLine("To work properly, this application needs:")
				.AppendLine("- Windows 7 operating system")
				.AppendLine("- .NET 4.0 framework, full profile")
				.AppendLine("- any LaTeX distibution with required packages")
				.AppendLine("- any PDF viewer");
			s.AppendLine();

			s.AppendLine("LaTeX packages required:")
				.AppendLine("- l3kernel")
				.AppendLine("- preprint")
				.AppendLine("- pgf")
				.AppendLine("- hm")
				.AppendLine("- hs")
				.AppendLine("- xcolor");
			s.AppendLine();

			s.Append("Modern multi-core processor is recommended,")
				.AppendLine(" also up to several GB of memory\nis needed in case of some complicated expressions.");

			new MessageFrame(this, "About Phinite", "Information about Phinite", s.ToString(), PhiImage.Source).ShowDialog();
			//ShowMessageFrame("Phinite", "Missing content", "technical analysis file was not found", false);
		}

		private void OptionViewBA_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Process.Start("bysiekm-business-analysis.pdf");
			}
			catch (Win32Exception)
			{
				ShowMessageFrame("Phinite", "Missing content", "business analysis file was not found", false);
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
				ShowMessageFrame("Phinite", "Missing content", "technical analysis file was not found", false);
			}
		}

		private void OptionViewUserGuide_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Process.Start("bysiekm-phinite-userguide.pdf");
			}
			catch (Win32Exception)
			{
				ShowMessageFrame("Phinite", "Missing content", "user guide file was not found", false);
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
				thisComputationSessionId = 0;
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

			ShowMessageFrame("Phinite information", "Regular expression input", s.ToString(), false);
		}

		private void Info_ParseTree(object sender, RoutedEventArgs e)
		{
			var s = new StringBuilder();

			s.Append("This screen presents a parse tree and two versions of the input regular expression above it.");
			s.Append(" If you check that the second, (\"").Append("Validated and optimized input")
				.Append("\"), has the same meaning as you intended, you may safely continue.");
			s.Append("\n\n");

			s.Append("If not, please cancel the computation and enter the expression in such way that it is properly understood by the program.");

			s.Append(" Please remember to follow the rules of regular expression operators precedence, use special symbols properly, etc.");

			ShowMessageFrame("Phinite information", "Parse tree: first interpretation of the input", s.ToString(), false);
		}

		private void Info_MachineConstruction(object sender, RoutedEventArgs e)
		{
			var s = new StringBuilder();

			s.Append("Use buttons on the left side to control the construction process.");
			s.Append("\n\n");
			s.Append("When the construction is complete, you may go right to word evaluation screen,");
			s.Append(" or before that stop for a moment to view a PDF with construction results report.");
			s.Append("\n\n");
			s.Append("To do the former, select \"Go to word evaluation\", and to do the latter select \"Generate LaTeX code\".");

			ShowMessageFrame("Phinite information", "Finite-state machine construction", s.ToString(), false);
		}

		private void Info_Latex(object sender, RoutedEventArgs e)
		{
			var s = new StringBuilder();

			s.Append("");
			s.Append("");

			ShowMessageFrame("Phinite information", "Regular expression input", s.ToString(), false);
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

			ShowMessageFrame("Phinite information", "Word input", s.ToString(), false);
		}

		#endregion

		private void WindowMain_Loaded(object sender, RoutedEventArgs e)
		{
			var sett = Properties.Settings.Default;
			settings = new PhiniteSettings(sett);
			sett.SettingsSaving += Default_SettingsSaving;
		}

		private void Default_SettingsSaving(object sender, CancelEventArgs e)
		{
			var sett = Properties.Settings.Default;
		}

		void WindowUserHelp_Closed(object sender, EventArgs e)
		{
			if (sender is WindowUserHelp == false)
				return;

			var w = (WindowUserHelp)sender;

			if (w.ResolvedEquivalent.HasValue == false)
			{
				OptionAbort_Click(sender, null);
				return;
			}

			if (stepByStep)
			{
				ShowCurrentConstructionState(thisComputationSessionId);

				SetUIState(UIState.ReadyForNextConstructionStep);
				return;
			}

			OptionNextStep_Click(sender, null);
		}

		private void WindowMain_Closing(object sender, CancelEventArgs e)
		{
			SetUIState(UIState.Loading);

			settings.Save();

			lock (regexpAndFsmLock)
			{
				fsm = null;
				regexp = null;
			}
		}

	}
}
