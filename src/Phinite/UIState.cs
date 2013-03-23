using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phinite
{
	/// <summary>
	/// List of states of UI. Each state carries information regarding status bar
	/// and disables/enables different UI elements for user interaction.
	/// States are also responsible for visibility of certain elements of UI.
	/// </summary>
	public enum UIState : ulong
	{
		Loading = 1 << 20,

		RegexpInputPhase = 1 << 21,
		ReadyForRegexpInput = RegexpInputPhase | 1,
		ReadyForNewInputAfterError = RegexpInputPhase | 1 << 1,
		ReadyForNewInputAfterAbortedComputation = RegexpInputPhase | 1 << 2,
		ReadyForNewInputAfterInvalidInput = RegexpInputPhase | 1 << 3,

		ValidationPhase = 1 << 22,
		ValidatingInputExpression = ValidationPhase | 1,

		ValidationResultsPhase = 1 << 23,
		ReadyForConstruction = ValidationResultsPhase | 1,

		ConstructionPhase = 1 << 24,
		BusyConstructing = ConstructionPhase | 1,

		ConstructionStepResultsPhase = 1 << 25,
		ReadyForNextConstructionStep = ConstructionStepResultsPhase | 1,

		ConstructionResultsPhase = 1 << 26,
		ReadyForEvaluation = ConstructionResultsPhase | 1,
		BusyGeneratingLatex = ConstructionResultsPhase | 1 << 1,

		LatexResultPhase = 1 << 27,
		ReadyForLatexProcessing = LatexResultPhase | 1,
		BusyGeneratingPdf = LatexResultPhase | 1 << 1,
		PdfGenerated = LatexResultPhase | 1 << 2,
		PdfGenerationError = LatexResultPhase | 1 << 3,

		WordInputPhase = 1 << 28,
		ReadyForNewWord = WordInputPhase | 1,

		EvaluationPhase = 1 << 29,
		BusyEvaluating = EvaluationPhase | 1,
		ReadyForNextEvaluationStep = EvaluationPhase | 1 << 1,

		EvaluationResultsPhase = 1 << 30,
		WordWasAccepted = EvaluationResultsPhase | 1,
		WordWasRejected = EvaluationResultsPhase | 1 << 1,

		Invalid = 0
	}

}
