using System.ComponentModel;
using System.Windows;

namespace Phinite
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window, INotifyPropertyChanged
	{
		private PhiniteSettings phi;

		public event PropertyChangedEventHandler PropertyChanged;

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
			}
		}

		public SettingsWindow(PhiniteSettings settings)
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

	}
}
