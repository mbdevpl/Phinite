using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phinite
{
	/// <summary>
	/// List of UI states. Each state carries information regarding status bar
	/// and disables/enables different UI elements for user interaction.
	/// </summary>
	public enum Status
	{
		NotComputing = 1,
		Ready = 1 + 2,

		Computing = 1024,
		ReadyForNextStep = 1024 + 2,
		AwaitingUserInput = 1024 + 2 + 4,
		Busy = 1024 + 8,
		ValidatingInput = 1024 + 8 + 16,

		/// <summary>
		/// Should never be set directly, indicates a GUI behaviour inconsistency
		/// and an unhandled status.
		/// </summary>
		Invalid = 0
	}
}
