using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Phinite
{
	public sealed class PdfGenerator : IDisposable
	{
		private static readonly string PdflatexOptions = new StringBuilder()
			.Append(@"-shell-escape ")
			.Append(@"-interaction=batchmode ")
			//.Append(@"-interaction=nonstopmode ")
			.ToString();

		private string pdflatexExecutable;

		private string pdfViewerExecutable;

		private string texInput;

		private Process p;

		private string pdfOutputFileLocation;

		public PdfGenerator(string pdflatexExecutable, string pdfViewerExecutable)
		{
			this.pdflatexExecutable = pdflatexExecutable;
			//this.pdflatexTimeout = pdflatexTimeout;
			this.pdfViewerExecutable = pdfViewerExecutable;
		}

		public void Dispose()
		{
			if (p != null)
				p.Dispose();
		}

		private void Reset()
		{
			Dispose();
			p = null;
			pdfOutputFileLocation = null;
		}

		public void LoadInputFromString(string texInput)
		{
			Reset();
			this.texInput = texInput;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="texInputFileLocation"></param>
		/// <param name="pdfOutputFileLocation"></param>
		/// <exception cref="ArgumentException"></exception>
		public void Start(string texInputFileLocation, string pdfOutputFileLocation)
		{

			string pdflatexArgs = new StringBuilder().Append(PdflatexOptions)
				.Append('"').Append(texInputFileLocation).Append('"').ToString();

			this.p = new Process();
			p.StartInfo = new ProcessStartInfo(pdflatexExecutable, pdflatexArgs);
			this.pdfOutputFileLocation = pdfOutputFileLocation;

			try
			{
				if (!p.Start())
				{
					ArgumentException thrown = new ArgumentException();
					thrown.Data.Add("title", "Error while starting LaTeX");
					thrown.Data.Add("text", String.Format("Unable to start LaTeX with this command: \"{0} {1}\"",
						pdflatexExecutable, pdflatexArgs));
					throw thrown;
				}
			}
			catch (Win32Exception e)
			{
				ArgumentException thrown = new ArgumentException(null, e);
				thrown.Data.Add("title", "Error in Phinite configuration");
				thrown.Data.Add("text", String.Format("There is no LaTeX executable at this path: \"{0}\"", pdflatexExecutable));
				throw thrown;
			}
		}

		/// <summary>
		/// Blocks until PDF file is created or timeout happens.
		/// </summary>
		/// <param name="pdflatexTimeout"></param>
		/// <returns>false if timeout happened</returns>
		/// <exception cref="ArgumentException"></exception>
		public bool WaitForExit(int pdflatexTimeout)
		{
			try
			{
				if (p == null)
					throw new ArgumentNullException("p");
				if (pdfOutputFileLocation == null)
					throw new ArgumentNullException("pdfOutputFileLocation");

				if (!p.WaitForExit(1000 * pdflatexTimeout))
					return false;

				if (p.ExitCode != 0)
				{
					ArgumentException thrown = new ArgumentException();
					if (File.Exists(pdfOutputFileLocation))
					{
						OpenOutputFile();

						thrown.Data.Add("title", "Minor errors in LaTeX execution");
						thrown.Data.Add("text",
							"PDF file was created, but there were some errors and the result may not look as good as expected.");
					}
					else
					{
						thrown.Data.Add("title", "Severe errors in LaTeX execution");
						thrown.Data.Add("text",
							"LaTeX failed to create the PDF file due to some critical errors. Read log to diagnose a problem.");
					}
					throw thrown;
				}

				OpenOutputFile();
			}
			catch (Win32Exception e)
			{
				ArgumentException thrown = new ArgumentException(null, e);
				thrown.Data.Add("title", "Error in Phinite configuration");
				thrown.Data.Add("text", String.Format("There is no PDF viewer at this path:\n\n{0}", pdfViewerExecutable));
				throw thrown;
			}
			catch (ArgumentNullException e)
			{
				ArgumentException thrown = new ArgumentException(null, e);
				thrown.Data.Add("title", "Error in PDF generator usage");
				thrown.Data.Add("text", "The generator is not running nor it is in finished state.");
				throw thrown;
			}
			finally
			{
				string filename = pdfOutputFileLocation.Substring(0, pdfOutputFileLocation.LastIndexOf('.') + 1);
				try { File.Delete(filename + "log"); }
				catch (IOException) { }
				try { File.Delete(filename + "aux"); }
				catch (IOException) { }
			}

			Reset();

			return true;
		}

		private void OpenOutputFile()
		{
			if (pdfViewerExecutable.Length > 0)
				Process.Start(pdfViewerExecutable, pdfOutputFileLocation);
			else
				Process.Start(pdfOutputFileLocation);
		}

	}
}
