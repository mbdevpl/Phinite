using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Phinite
{

	/// <summary>
	/// A Frame that is used to display messages in WPF style (not those default dialogs
	/// that look like straight from Windows 2000).
	/// 
	/// Interaction logic for SystemMessage.xaml
	/// </summary>
	public partial class MessageFrame : Window, INotifyPropertyChanged
	{

		private string windowTitle;
		public string WindowTitle
		{
			get { return windowTitle; }
			set
			{
				if (windowTitle == value)
					return;
				windowTitle = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("WindowTitle"));
			}
		}

		private string messageTitle;
		public string MessageTitle
		{
			get { return messageTitle; }
			set
			{
				if (messageTitle == value)
					return;
				messageTitle = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("MessageTitle"));
			}
		}

		private string messageText;
		public string MessageText
		{
			get { return messageText; }
			set
			{
				if (messageText == value)
					return;
				messageText = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("MessageText"));
			}
		}

		private ImageSource messageImage;
		public ImageSource MessageImage
		{
			get { return messageImage; }
			set
			{
				if (messageImage == value)
					return;
				messageImage = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("MessageImage"));
			}
		}

		private bool toggleImage;
		public bool ToggleImage
		{
			get { return toggleImage; }
			set
			{
				if (toggleImage == value)
					return;
				toggleImage = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("ToggleImage"));
			}
		}

		private bool toggleOk;
		public bool ToggleOk
		{
			get { return toggleOk; }
			set
			{
				if (toggleOk == value)
					return;
				toggleOk = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("ToggleOk"));
			}
		}

		private string textOk;
		public string TextOk
		{
			get { return textOk; }
			set
			{
				if (textOk == value)
					return;
				textOk = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("TextOk"));
			}
		}

		private bool toggleCancel;
		public bool ToggleCancel
		{
			get { return toggleCancel; }
			set
			{
				if (toggleCancel == value)
					return;
				toggleCancel = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("ToggleCancel"));
			}
		}

		private string textCancel;
		public string TextCancel
		{
			get { return textCancel; }
			set
			{
				if (textCancel == value)
					return;
				textCancel = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("TextCancel"));
			}
		}

		private bool toggleHelp;
		public bool ToggleHelp
		{
			get { return toggleHelp; }
			set
			{
				if (toggleHelp == value)
					return;
				toggleHelp = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("ToggleHelp"));
			}
		}

		private string textHelp;
		public string TextHelp
		{
			get { return textHelp; }
			set
			{
				if (textHelp == value)
					return;
				textHelp = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("TextHelp"));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public MessageFrame()
		{
			SetContent(String.Empty, String.Empty, String.Empty, null);
			SetButtons();

			DataContext = this;
			InitializeComponent();
		}

		public MessageFrame(Window owner, string windowTitle, string messageTitle, string messageText,
			ImageSource image = null,
			bool toggleOk = true, bool toggleCancel = false, bool toggleHelp = false,
			string captionOk = "Ok", string captionCancel = "Cancel", string captionHelp = "Help")
		{
			this.Owner = owner;
			SetContent(windowTitle, messageTitle, messageText, image);
			SetButtons(toggleOk, toggleCancel, toggleHelp, captionOk, captionCancel, captionHelp);

			DataContext = this;
			InitializeComponent();
		}

		public MessageFrame(Exception ex, bool toggleOk = true, bool toggleCancel = false,
				bool toggleHelp = false)
			: this(null, "Error information", String.Format("Exception was thrown: {0}", ex.GetType()),
				ex.ToString(), null, toggleOk, toggleCancel, toggleHelp)
		{
			//nothing needed here
		}

		//public SystemMessage(ActionResult actionResult, bool toggleOk = true,
		//        bool toggleCancel = false, bool toggleHelp = false)
		//    : this("Achievement GET!", actionResult.Title, actionResult.Desc, MemeType.FuckYea,
		//        toggleOk, toggleCancel, toggleHelp) {
		//    //nothing needed here
		//}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			this.Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}

		private void Help_Click(object sender, RoutedEventArgs e)
		{
			//this.DialogResult = false;
			//this.Close();
		}

		private void SetContent(string newWindowTitle, string newMessageTitle, string newMessageText,
				ImageSource image)
		{
			WindowTitle = newWindowTitle;
			MessageTitle = newMessageTitle;
			MessageText = newMessageText;
			if (image == null)
				ToggleImage = false;
			else
				ToggleImage = true;
			MessageImage = image;
		}

		private void SetButtons(bool toggleOk = true, bool toggleCancel = false, bool toggleHelp = false,
			string captionOk = "Ok", string captionCancel = "Cancel", string captionHelp = "Help")
		{
			ToggleOk = toggleOk;
			ToggleCancel = toggleCancel;
			ToggleHelp = toggleHelp;

			TextOk = captionOk;
			TextCancel = captionCancel;
			TextHelp = captionHelp;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			IconHelper.RemoveIcon(this);

			base.OnSourceInitialized(e);
		}

	}
}
