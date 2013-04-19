using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Phinite
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window, INotifyPropertyChanged
	{
		internal readonly Properties.Settings DefaultSettings;

		public string DefaultInternalPdfViewer { get { return DefaultSettings.DefaultInternalPdfViewerCommand; } }

		public string DefaultInternalPdflatex { get { return DefaultSettings.DefaultInternalPdflatexCommand; } }

		public event PropertyChangedEventHandler PropertyChanged;

		public string Pdflatex
		{
			get { return pdflatex; }
			set
			{
				if (pdflatex == value)
					return;
				pdflatex = value;

				InvokePropertyChanged("Pdflatex");
			}
		}
		private string pdflatex;

		public string PdfViewer
		{
			get { return pdfViewer; }
			set
			{
				if (pdfViewer == value)
					return;
				pdfViewer = value;

				InvokePropertyChanged("PdfViewer");
			}
		}
		private string pdfViewer;

		public SettingsWindow()
		{
			DataContext = this;

			DefaultSettings = Properties.Settings.Default;

			InitializeComponent();

			Pdflatex = DefaultSettings.PdflatexCommand;
			PdfViewer = DefaultSettings.PdfViewerCommand;
		}

		private void InvokePropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

	}
}
