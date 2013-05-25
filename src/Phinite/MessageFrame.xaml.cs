using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using MBdev.Extensions;

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
		/// <summary>
		/// Title of the message frame.
		/// </summary>
		public string WindowTitle
		{ get { return windowTitle; } set { this.ChangeProperty(PropertyChanged, ref windowTitle, value, "WindowTitle"); } }
		private string windowTitle;

		/// <summary>
		/// Title of the message, printed inside the frame right above the message.
		/// </summary>
		public string MessageTitle
		{ get { return messageTitle; } set { this.ChangeProperty(PropertyChanged, ref messageTitle, value, "MessageTitle"); } }
		private string messageTitle;

		/// <summary>
		/// Text of the message.
		/// </summary>
		public string MessageText
		{ get { return messageText; } set { this.ChangeProperty(PropertyChanged, ref messageText, value, "MessageText"); } }
		private string messageText;

		/// <summary>
		/// Image put on the left-hand side of the message text.
		/// </summary>
		public ImageSource MessageImage
		{ get { return messageImage; } set { this.ChangeProperty(PropertyChanged, ref messageImage, value, "MessageImage"); } }
		private ImageSource messageImage;

		public bool ToggleImage
		{ get { return toggleImage; } set { this.ChangeProperty(PropertyChanged, ref toggleImage, ref value, "ToggleImage"); } }
		private bool toggleImage;

		public bool ToggleOk
		{ get { return toggleOk; } set { this.ChangeProperty(PropertyChanged, ref toggleOk, ref value, "ToggleOk"); } }
		private bool toggleOk;

		public string TextOk
		{ get { return textOk; } set { this.ChangeProperty(PropertyChanged, ref textOk, value, "TextOk"); } }
		private string textOk;

		public bool ToggleCancel
		{ get { return toggleCancel; } set { this.ChangeProperty(PropertyChanged, ref toggleCancel, ref value, "ToggleCancel"); } }
		private bool toggleCancel;

		/// <summary>
		/// Text printed on the 'Cancel' button.
		/// </summary>
		public string TextCancel
		{ get { return textCancel; } set { this.ChangeProperty(PropertyChanged, ref textCancel, value, "TextCancel"); } }
		private string textCancel;

		public bool ToggleHelp
		{ get { return toggleHelp; } set { this.ChangeProperty(PropertyChanged, ref toggleHelp, ref value, "ToggleHelp"); } }
		private bool toggleHelp;

		public string TextHelp
		{ get { return textHelp; } set { this.ChangeProperty(PropertyChanged, ref textHelp, value, "TextHelp"); } }
		private string textHelp;

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

		private void SetButtons(bool newToggleOk = true, bool newToggleCancel = false, bool newToggleHelp = false,
			string captionOk = "Ok", string captionCancel = "Cancel", string captionHelp = "Help")
		{
			ToggleOk = newToggleOk;
			ToggleCancel = newToggleCancel;
			ToggleHelp = newToggleHelp;

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
