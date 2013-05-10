using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
		/// <summary>
		/// Application settings.
		/// </summary>
		public PhiniteSettings Settings { get { Settings_Initialize(); return settings; } }
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

		/// <summary>
		/// expression, label, remarks
		/// </summary>
		public List<Tuple<RegularExpression, string, string>> LabeledExpressionsData
		{ get { return labeledExpressionsData; } set { this.ChangeProperty(PropertyChanged, ref labeledExpressionsData, value, "LabeledExpressionsData"); } }
		private List<Tuple<RegularExpression, string, string>> labeledExpressionsData;

		/// <summary>
		/// initial expression, initial label, set of letters, resulting label, resulting expression
		/// </summary>
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

		private static void CallMethodInNewThread(ThreadStart method, string name)
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
				a3.Create(-1, ref computationSessionId);

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

			Dispatcher.Invoke((Action)delegate
			{
				foreach (DataGridColumn column in DataGridForStates.Columns)
					column.Width = DataGridLength.SizeToHeader;
				foreach (DataGridColumn column in DataGridForStates.Columns)
					column.Width = DataGridLength.Auto;

				foreach (DataGridColumn column in DataGridForTransitions.Columns)
					column.Width = DataGridLength.SizeToHeader;
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

							fsm.RefineSimilarities();

							windowUserHelp = new WindowUserHelp(settings, regexpAndFsmLock, fsm);
						}
						windowUserHelp.Owner = this;

						windowUserHelp.Closed += WindowUserHelp_Closed;

						SetUIState(UIState.WaitingForUserHelp);
					});

				Dispatcher.BeginInvoke((Action)delegate
					{
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
			bool recreateLayout = false;

			lock (regexpAndFsmLock)
			{
				if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
					return false;

				accepting = fsm.AcceptingStates;
				states = fsm.States;
				transitions = fsm.Transitions;

				recreateLayout = fsm.IsConstructionFinished() || (layoutAge == settings.LayoutCreationFrequency);

				if (stepByStep || recreateLayout)
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
			if (stepByStep || recreateLayout)
			{
				if (layout == null)
					layout = fsmLayout;
				// session is needed to abort layout creation on computation abort
				else if (!layout.Create(sessionId, ref thisComputationSessionId))
				{
					if (CheckIfComputationAbortedAndDealWithIt(sessionId, fsm))
						return false;
				}

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

			// tables
			if (stepByStep || recreateLayout)
			{
				RefillLabeledExpressionsData(states, accepting);
				RefillTransitionsData(states, transitions);
			}

			// status below tables
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

		private void RefillLabeledExpressionsData(IEnumerable<RegularExpression> states,
			ICollection<RegularExpression> accepting)
		{
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
		}

		private void RefillTransitionsData(IList<RegularExpression> states,
			IEnumerable<MachineTransition> transitions)
		{
			var data2 = new List<Tuple<RegularExpression, string, string, string, RegularExpression>>();
			foreach (var transition in transitions)
			{
				data2.Add(new Tuple<RegularExpression, string, string, string, RegularExpression>(
					states[transition.Item1], String.Format("q{0}", transition.Item1),
					String.Join(", ", transition.Item2),
					String.Format("q{0}", transition.Item3), states[transition.Item3]));
			}

			TransitionsData = data2;
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

			// TODO: move these to lock
			var states = fsm.States;
			var accepting = fsm.AcceptingStates;
			var initial = fsm.InitialState;
			var transitions = fsm.Transitions;

			var locations = fsmLayout.Locations;
			var angles = fsmLayout.Angles;

			string epsilon = RegularExpression.TagsStrings[InputSymbolTag.EmptyWord];

			var s1 = new StringBuilder();
			int i = 0;
			foreach (var state in states)
			{
				s1.Append(App.Template_ReportState);

				bool cond1 = state == initial;
				bool cond2 = accepting.Any(x => x == state);

				s1.Replace("[data:label]", i.ToString());
				s1.Replace("[data:remarks]", String.Format("{0}{1}{2}", cond1 ? "initial state" : "",
					cond1 && cond2 ? " , " : "", cond2 ? "accepting state" : ""));
				s1.Replace("[data:regexp]", state.ToString());

				++i;
			}

			var s2 = new StringBuilder();
			foreach (var transition in transitions)
			{
				s2.Append(App.Template_ReportTransition);

				s2.Replace("[data:label1]", transition.InitialStateId.ToString());
				s2.Replace("[data:letters]", String.Join(@", \; ", transition.Letters));
				s2.Replace("[data:label2]", transition.ResultingStateId.ToString());
				s2.Replace("[data:regexp1]", states[transition.InitialStateId].ToString());
				s2.Replace("[data:regexp2]", states[transition.ResultingStateId].ToString());
			}

			StringBuilder s3 = new StringBuilder();

			s3.AppendLine(@"  [->,>=stealth',shorten >=1pt,auto,transform shape]");
			s3.AppendLine();

			i = 0;
			foreach (var state in states)
			{
				s3.AppendLine(App.Template_ReportGraphState);

				s3.Replace("[data:x]", (locations[i].X / 35).ToString());
				s3.Replace("[data:y]", (-locations[i].Y / 35).ToString());

				s3.Replace("[data:flags]", String.Format("{0}state{1}",
					i == 0 ? "initial," : "", accepting.Contains(state) ? ",accepting" : ""));
				s3.Replace("[data:label]", i.ToString());

				++i;
			}
			s3.AppendLine();

			s3.AppendLine(@"    \path");
			i = 0;
			foreach (var transition in transitions)
			{
				s3.AppendLine(App.Template_ReportGraphTransition);

				int index1 = transition.InitialStateId;
				int index2 = transition.ResultingStateId;

				double angle1 = -angles[i].Item1 + 90;
				double angle2 = -angles[i].Item2 + 90;

				bool loop = index2 - index1 == 0;

				s3.Replace("[data:label1]", index1.ToString());
				s3.Replace("[data:label2]", index2.ToString());

				s3.Replace("[data:angle1]", (angle1 + (loop ? 20 : 0)).ToString());
				s3.Replace("[data:angle2]", (angle2 - (loop ? 20 : 0)).ToString());
				s3.Replace("[data:flags]", loop ? ",loop" : "");
				s3.Replace("[data:letters]", String.Join(",", transition.Letters));
				s3.Replace("[data:lettersflags]", (loop && ((angle1 < 0 && angle1 >= -180) || (angle1 >= 180 && angle1 < 360))) ? ",below" : ",above");

				++i;
			}

			s3.AppendLine(@"    ;");

			StringBuilder s = new StringBuilder(App.Template_Report);

			s.Replace("[data:inputoriginal]", inputRegexpText.Replace(epsilon, @"\epsilon"));
			s.Replace("[data:inputoptimized]", regexp.ToString().Replace(epsilon, @"\epsilon"));
			s.Replace("[data:tablestates]", s1.ToString());
			s.Replace("[data:tabletransitions]", s2.ToString());
			s.Replace("[data:graph]", s3.ToString());

			LatexOutputText = s.ToString();

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

			PdfGenerator generator = new PdfGenerator(settings.Pdflatex, settings.PdfViewer);

			try
			{
				generator.Start(generatedTex, generatedPdf);

				while (!generator.WaitForExit(settings.PdflatexTimeout))
				{
					bool userWaitsForTimeout = true;
					var timeoutMessageFormat = new StringBuilder()
						.Append("It takes more than {0} seconds to build PDF file.")
						.Append(" Do you want Phinite to wait for another {1} seconds to finish?")
						.Append(" If not, the PDF file will not be opened automatically, even when it is finally created.")
						.ToString();

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
				}
			}
			catch (ArgumentException e)
			{
				ShowMessageFrame("Phinite error", (string)e.Data["title"], (string)e.Data["text"], true);
				SetUIState(UIState.PdfGenerationError);
				return;
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

			//InputRegexpDocument.Blocks.Clear();
			//InputRegexpDocument.Blocks.Add(new Paragraph(new Run(inputRegexpText)));
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
			new MessageFrame(this, "About Phinite", App.Name, App.Text_About, PhiImage.Source).ShowDialog();
		}

		private void OptionViewBA_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Process.Start("bysiekm-business-analysis.pdf");
			}
			catch (Win32Exception)
			{
				ShowMessageFrame("Phinite", "Missing content", "Business analysis file was not found.", false);
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
				ShowMessageFrame("Phinite", "Missing content", "Technical analysis file was not found.", false);
			}
		}

		private void OptionViewUserGuide_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Process.Start("phinite-userguide.pdf");
			}
			catch (Win32Exception)
			{
				ShowMessageFrame("Phinite", "Missing content", "User guide file was not found, it will be now automatically generated.", false);

				StringBuilder s = new StringBuilder(App.Template_UserGuide);

				s.Replace("[data:regexpinput]", App.Latex_RegexpInput);
				s.Replace("[data:parsetree]", App.Latex_ParseTree);
				s.Replace("[data:construction]", App.Latex_Construction);
				s.Replace("[data:userhelp]", App.Latex_UserHelp);
				s.Replace("[data:report]", App.Latex_Report);
				s.Replace("[data:wordinput]", App.Latex_WordInput);
				s.Replace("[data:evaluation]", App.Latex_Evaluation);

				var text = s.ToString();

				string filename = "phinite-userguide";

				string generatedTex = filename + ".tex";
				string generatedTexDir = Environment.CurrentDirectory.Replace('\\', '/');
				string generatedPdf = filename + ".pdf";

				File.AppendAllText(generatedTex, text);

				PdfGenerator generator = new PdfGenerator(settings.Pdflatex, settings.PdfViewer);

				try
				{
					generator.Start(generatedTex, generatedPdf);

					while (!generator.WaitForExit(settings.PdflatexTimeout))
					{
						bool userWaitsForTimeout = true;
						var timeoutMessageFormat = new StringBuilder()
							.Append("It takes more than {0} seconds to build PDF file.")
							.Append(" Do you want Phinite to wait for another {1} seconds to finish?")
							.Append(" If not, the PDF file will not be opened automatically, even when it is finally created.")
							.ToString();

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
					}
				}
				catch (ArgumentException ex)
				{
					ShowMessageFrame("Phinite error", (string)ex.Data["title"], (string)ex.Data["text"], true);
				}

				try { File.Delete(generatedTex); }
				catch (IOException) { }

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

			// change to binding if possible
			//if(InputRegexpDocument != null)
			//	InputRegexpText = new TextRange(InputRegexpDocument.ContentStart, InputRegexpDocument.ContentEnd).Text;
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

		private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (sender == null || sender is DataGrid == false)
				return;
			if (e == null || e.OriginalSource is DependencyObject == false)
				return;

			int row;
			int column;

			((DataGrid)sender).FindElementLocation((DependencyObject)e.OriginalSource, out column, out row);

			PartialExpression parseTree = null;
			if (ReferenceEquals(sender, DataGridForStates))
			{
				if (column != 2)
					return;

				parseTree = LabeledExpressionsData[row].Item1.ParseTree;
			}
			else if (ReferenceEquals(sender, DataGridForTransitions))
			{
				if (column != 3 && column != 4)
					return;

				if (column == 3)
					parseTree = TransitionsData[row].Item1.ParseTree;
				else
					parseTree = TransitionsData[row].Item5.ParseTree;
			}

			if (parseTree == null)
				return;

			e.Handled = true;

			new WindowSimpleCanvas(parseTree).ShowDialog();
		}

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
			ShowMessageFrame("Phinite information", "Regular expression input", App.Text_RegexpInput, false);
		}

		private void Info_ParseTree(object sender, RoutedEventArgs e)
		{
			ShowMessageFrame("Phinite information", "Parse tree: first interpretation of the input", App.Text_ParseTree, false);
		}

		private void Info_MachineConstruction(object sender, RoutedEventArgs e)
		{
			ShowMessageFrame("Phinite information", "Finite-state machine construction", App.Text_Construction, false);
		}

		private void Info_Latex(object sender, RoutedEventArgs e)
		{
			ShowMessageFrame("Phinite information", "Using LaTeX to generate PDF report", App.Text_Report, false);
		}

		private void Info_InputWord(object sender, RoutedEventArgs e)
		{
			ShowMessageFrame("Phinite information", "Word input", App.Text_WordInput, false);
		}

		private void Info_Evaluation(object sender, RoutedEventArgs e)
		{
			ShowMessageFrame("Phinite information", "Word evaluation", App.Text_Evaluation, false);
		}

		#endregion

		private void WindowMain_Loaded(object sender, RoutedEventArgs e)
		{
			Settings_Initialize();
		}

		private void Settings_Initialize()
		{
			if (settings == null)
			{
				var sett = Properties.Settings.Default;
				settings = new PhiniteSettings(sett);
				sett.SettingsSaving += Settings_Saving;
			}
		}

		private void Settings_Saving(object sender, CancelEventArgs e)
		{
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
