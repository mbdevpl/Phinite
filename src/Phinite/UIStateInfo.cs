using System.Collections.Generic;

namespace Phinite
{

	public static class UIStateInfo
	{
		public static readonly Dictionary<UIState, string> Status;

		static UIStateInfo()
		{
			Status = new Dictionary<UIState, string>
			{
				{UIState.Loading, "loading"},

				// RegexpInputPhase
				{UIState.ReadyForRegexpInput, "ready"},
				{UIState.ReadyForNewInputAfterError, "computation aborted due to an error; ready for new input"},
				{UIState.ReadyForNewInputAfterAbortedComputation, "computation aborted by the user; ready for new input"},
				{UIState.ReadyForNewInputAfterInvalidInput, "input expression is invalid; ready for new input"},

				// ValidationPhase
				{UIState.ValidatingInputExpression, "validating input expression"},

				// ValidationResultsPhase
				{UIState.ReadyForConstruction, "ready for machine construction"},

				// ConstructionPhase
				{UIState.BusyConstructing, "busy"},

				// ConstructionStepResultsPhase
				{UIState.ReadyForNextConstructionStep, "ready for next step of construction"},

				// ConstructionResultsPhase
				{UIState.ReadyForEvaluation, "ready for word evaluation"},
				{UIState.BusyGeneratingLatex, "generating latex"},

				// LatexResultPhase
				{UIState.ReadyForLatexProcessing, "ready"},
				{UIState.BusyGeneratingPdf, "generating pdf"},
				{UIState.PdfGenerated, "pdf generated succesfully; ready for other tasks"},
				{UIState.PdfGenerationError, "errors in pdf generation; ready for other tasks"},
				{UIState.PdfGenerationTimeout, "pdf generation timeout; ready for other tasks"},

				// WordInputPhase
				{UIState.ReadyForNewWord, "ready"},

				// EvaluationPhase
				{UIState.BusyEvaluating, "busy"},
				{UIState.ReadyForNextEvaluationStep, "ready for next step"},

				// EvaluationResultsPhase
				{UIState.WordWasAccepted, "word was accepted"},
				{UIState.WordWasRejected, "word was rejected"},

				{UIState.Invalid, "invalid"}
			};
		}

	}

}
