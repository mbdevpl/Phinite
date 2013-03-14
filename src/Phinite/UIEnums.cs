using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phinite
{
	/// <summary>
	/// List of UI elements' visibility configuration variants for the main frame.
	/// </summary>
	public enum UIVisibility
	{
		None,
		Input,
		IntermediateResult,
		UserHelp,
		FinalResult
	}

	/// <summary>
	/// List of UI elements' availability configuration variants for the main frame.
	/// </summary>
	public enum UIEnabled
	{
		Yes,
		No
	}
}
