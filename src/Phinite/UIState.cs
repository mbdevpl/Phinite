
namespace Phinite
{
	/// <summary>
	/// List of states of UI. Each state carries information regarding status bar
	/// and disables/enables different UI elements for user interaction.
	/// States are also responsible for visibility of certain elements of UI.
	/// </summary>
	public enum UIState
	{
		Loading = 1 << 10,

		RegexpInputPhase = 1 << 11,
		ReadyForRegexpInput = RegexpInputPhase | 1,
		ReadyForNewInputAfterError = RegexpInputPhase | 1 << 1,
		ReadyForNewInputAfterAbortedComputation = RegexpInputPhase | 1 << 2,
		ReadyForNewInputAfterInvalidInput = RegexpInputPhase | 1 << 3,

		ValidationPhase = 1 << 12,
		ValidatingInputExpression = ValidationPhase | 1,

		ValidationResultsPhase = 1 << 13,
		ReadyForConstruction = ValidationResultsPhase | 1,

		ConstructionPhase = 1 << 14,
		BusyConstructing = ConstructionPhase | 1,
		WaitingForUserHelp = ConstructionPhase | 1 << 1,

		ConstructionStepResultsPhase = 1 << 15,
		ReadyForNextConstructionStep = ConstructionStepResultsPhase | 1,

		ConstructionResultsPhase = 1 << 16,
		ReadyForEvaluation = ConstructionResultsPhase | 1,
		BusyGeneratingLatex = ConstructionResultsPhase | 1 << 1,

		LatexResultPhase = 1 << 17,
		ReadyForLatexProcessing = LatexResultPhase | 1,
		BusyGeneratingPdf = LatexResultPhase | 1 << 1,
		PdfGenerated = LatexResultPhase | 1 << 2,
		PdfGenerationError = LatexResultPhase | 1 << 3,
		PdfGenerationTimeout = LatexResultPhase | 1 << 4,

		WordInputPhase = 1 << 18,
		ReadyForNewWord = WordInputPhase | 1,

		EvaluationPhase = 1 << 19,
		BusyEvaluating = EvaluationPhase | 1,
		ReadyForNextEvaluationStep = EvaluationPhase | 1 << 1,

		EvaluationResultsPhase = 1 << 20,
		WordWasAccepted = EvaluationResultsPhase | 1,
		WordWasRejected = EvaluationResultsPhase | 1 << 1,

		Invalid = 0
	}

}
