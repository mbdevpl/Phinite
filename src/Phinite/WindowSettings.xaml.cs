using System;
using System.ComponentModel;
using System.Windows;

namespace Phinite
{
	/// <summary>
	/// Enables the user to change application settings.
	/// 
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class WindowSettings : Window, INotifyPropertyChanged
	{
		private PhiniteSettings phi;

		public event PropertyChangedEventHandler PropertyChanged;

		public int LayoutCreationFrequencyInUse
		{
			get { return phi.Settings.LayoutCreationFrequencyInUse; }
			set
			{
				if (phi.Settings.LayoutCreationFrequencyInUse == value)
					return;
				phi.Settings.LayoutCreationFrequencyInUse = value;
				InvokePropertyChanged("LayoutCreationFrequencyInUse");
			}
		}

		public int LayoutCreationFrequencyDefault { get { return phi.Settings.LayoutCreationFrequencyDefault; } }

		public decimal LayoutCreationFrequency
		{
			get { return phi.Settings.LayoutCreationFrequency; }
			set
			{
				if (phi.Settings.LayoutCreationFrequency == (int)value)
					return;
				phi.Settings.LayoutCreationFrequency = (int)value;
				InvokePropertyChanged("LayoutCreationFrequency");
				LayoutCreationFrequencyInUse = 1;
			}
		}

		public int EnableAutoResolutionModeInUse
		{
			get { return phi.Settings.EnableAutoResolutionModeInUse; }
			set
			{
				if (phi.Settings.EnableAutoResolutionModeInUse == value)
					return;
				phi.Settings.EnableAutoResolutionModeInUse = value;
				InvokePropertyChanged("EnableAutoResolutionModeInUse");
			}
		}

		public bool EnableAutoResolutionModeDefault { get { return phi.Settings.EnableAutoResolutionModeDefault; } }

		public bool EnableAutoResolutionMode
		{
			get { return phi.Settings.EnableAutoResolutionMode; }
			set
			{
				if (phi.Settings.EnableAutoResolutionMode == value)
					return;
				phi.Settings.EnableAutoResolutionMode = value;
				InvokePropertyChanged("EnableAutoResolutionMode");
				EnableAutoResolutionModeInUse = 1;
			}
		}

		public int PdflatexInUse
		{
			get { return phi.Settings.PdflatexInUse; }
			set
			{
				if (phi.Settings.PdflatexInUse == value)
					return;
				phi.Settings.PdflatexInUse = value;
				InvokePropertyChanged("PdflatexInUse");
			}
		}

		public string PdflatexInternal { get { return phi.Settings.PdflatexInternal; } }

		public string PdflatexExternal { get { return phi.Settings.PdflatexExternal; } }

		public string Pdflatex
		{
			get { return phi.Settings.Pdflatex; }
			set
			{
				if (phi.Settings.Pdflatex.Equals(value))
					return;
				phi.Settings.Pdflatex = value;
				InvokePropertyChanged("Pdflatex");
				PdflatexInUse = 2;
			}
		}

		public int PdflatexTimeoutInUse
		{
			get { return phi.Settings.PdflatexTimeoutInUse; }
			set
			{
				if (phi.Settings.PdflatexTimeoutInUse == value)
					return;
				phi.Settings.PdflatexTimeoutInUse = value;
				InvokePropertyChanged("PdflatexTimeoutInUse");
			}
		}

		public int PdflatexTimeoutDefault { get { return phi.Settings.PdflatexTimeoutDefault; } }

		public decimal PdflatexTimeout
		{
			get { return phi.Settings.PdflatexTimeout; }
			set
			{
				if (phi.Settings.PdflatexTimeout == (int)value)
					return;
				phi.Settings.PdflatexTimeout = (int)value;
				InvokePropertyChanged("PdflatexTimeout");
				PdflatexTimeoutInUse = 1;
			}
		}

		public int PdfViewerInUse
		{
			get { return phi.Settings.PdfViewerInUse; }
			set
			{
				if (phi.Settings.PdfViewerInUse == value)
					return;
				phi.Settings.PdfViewerInUse = value;
				InvokePropertyChanged("PdfViewerInUse");
			}
		}

		public string PdfViewerInternal { get { return phi.Settings.PdfViewerInternal; } }

		public string PdfViewer
		{
			get { return phi.Settings.PdfViewer; }
			set
			{
				if (phi.Settings.PdfViewer.Equals(value))
					return;
				phi.Settings.PdfViewer = value;
				InvokePropertyChanged("PdfViewer");
				PdfViewerInUse = 2;
			}
		}

		public WindowSettings(PhiniteSettings settings)
		{
			phi = settings;

			DataContext = this;

			InitializeComponent();
		}

		private void InvokePropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		private void ButtonOk_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void ButtonCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			IconHelper.RemoveIcon(this);

			base.OnSourceInitialized(e);
		}

	}
}
