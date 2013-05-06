using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phinite.Properties;

namespace Phinite
{
	/// <summary>
	/// Makes the Phinite settings management easier.
	/// 
	/// Settings for Phinite are stored in two places, typical for Windows applications:
	/// 1) Phinite.exe.config file in application directory,
	/// 2) user/AppData/Local/Phinite
	/// </summary>
	public class PhiniteSettings
	{

		internal Settings Settings { get { return settings; } }
		private Settings settings;

		public int LayoutCreationFrequency
		{
			get
			{
				switch (settings.LayoutCreationFrequencyInUse)
				{
					case 0: return settings.LayoutCreationFrequencyDefault;
					case 1: return settings.LayoutCreationFrequency;
					default: return -1;
				}
			}
		}

		public bool EnableAutoResolutionMode
		{
			get
			{
				switch (settings.EnableAutoResolutionModeInUse)
				{
					case 0: return settings.EnableAutoResolutionModeDefault;
					case 1: return settings.EnableAutoResolutionMode;
					default: return false;
				}
			}
		}

		public string Pdflatex
		{
			get
			{
				switch (settings.PdflatexInUse)
				{
					case 0: return settings.PdflatexInternal;
					case 1: return settings.PdflatexExternal;
					case 2: return settings.Pdflatex;
					default: return null;
				}
			}
		}

		public int PdflatexTimeout
		{
			get
			{
				switch (settings.PdflatexTimeoutInUse)
				{
					case 0: return settings.PdflatexTimeoutDefault;
					case 1: return settings.PdflatexTimeout;
					default: return -1;
				}
			}
		}

		public string PdfViewer
		{
			get
			{
				switch (settings.PdfViewerInUse)
				{
					case 0: return settings.PdfViewerInternal;
					case 1: return String.Empty;
					case 2: return settings.PdfViewer;
					default: return null;
				}
			}
		}

		internal PhiniteSettings(Settings settings)
		{
			this.settings = settings;
		}

		public PhiniteSettings(PhiniteSettings source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			settings = new Settings();

			settings.LayoutCreationFrequencyInUse = source.settings.LayoutCreationFrequencyInUse;
			settings.LayoutCreationFrequency = source.settings.LayoutCreationFrequency;

			settings.PdflatexInUse = source.settings.PdflatexInUse;
			settings.Pdflatex = source.settings.Pdflatex;

			settings.PdflatexTimeoutInUse = source.settings.PdflatexTimeoutInUse;
			settings.PdflatexTimeout = source.settings.PdflatexTimeout;

			settings.PdfViewerInUse = source.settings.PdfViewerInUse;
			settings.PdfViewer = source.settings.PdfViewer;
		}

		public void Save()
		{
			settings.Save();
		}

	}
}
