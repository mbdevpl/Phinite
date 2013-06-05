using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Phinite.Test
{
	public class PhiniteProcess
	{
		private ProcessStartInfo processInfo;

		private Process process;

		public PhiniteProcess()
		{
			processInfo = null;
			process = null;
		}

		/// <summary>
		/// Starts Phinite and waits 10 seconds for the program to load completely.
		/// </summary>
		/// <returns></returns>
		public bool Start()
		{
			char sep = Path.DirectorySeparatorChar;
			string dir = Directory.GetCurrentDirectory();
			Trace.WriteLine(String.Format("current directory: {0}", dir));

			string testpath = new StringBuilder().Append(sep).Append("src").Append(sep)
				.Append("TestResults").Append(sep).ToString();
			Trace.WriteLine(String.Format("looking for path part: {0}", testpath));
			if (!dir.Contains(testpath))
				return false;

			int index = dir.IndexOf(testpath);
			Trace.WriteLine(String.Format("index of this part is: {0}", index));
			if (index < 0)
				return false;

			string solutionPath = dir.Substring(0, index + 1);
			string binPath = new StringBuilder(solutionPath).Append("bin").Append(sep).ToString();
			Trace.WriteLine(String.Format("looking for Phinite.exe in: {0}", binPath));
			string exePath = String.Concat(binPath, "Phinite.exe");
			if (!File.Exists(exePath))
			{
				binPath = new StringBuilder(solutionPath).Append("src").Append(sep).Append("Phinite")
					.Append(sep).Append("bin").Append(sep).Append("Debug").Append(sep).ToString();
				Trace.WriteLine(String.Format("looking for Phinite.exe in: {0}", binPath));
				exePath = String.Concat(binPath, "Phinite.exe");
				if (!File.Exists(exePath))
					return false;
			}
			Trace.WriteLine(String.Format("starting program: {0}", exePath));

			processInfo = new ProcessStartInfo(exePath);

			process = Process.Start(processInfo);

			return process.WaitForInputIdle(10000);
		}

		/// <summary>
		/// Waits 10 seconds for Phinite to exit.
		/// </summary>
		/// <returns></returns>
		public bool WaitForExit()
		{
			if (process == null)
				return false;
			try
			{
				bool exited = process.WaitForExit(10000);
				if (exited)
					return true;
			}
			catch (Win32Exception)
			{
				return false;
			}
			catch (SystemException)
			{
				return false;
			}
			return false;
		}

	}
}
