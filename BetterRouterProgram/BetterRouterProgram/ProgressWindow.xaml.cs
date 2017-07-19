
using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace BetterRouterProgram
{
  
    public partial class ProgressWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressWindow"/> class. 
        /// This is the window used to display all messages from the configuration process, including errors.
        /// </summary>
        public ProgressWindow()
        {
            InitializeComponent();
            progressBar.Value = 0.5;
        }

        /// <summary>
        /// If the window is closed by the user, make sure the COM port is freed
        /// </summary>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
        void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            SerialConnection.CloseConnection();
        }

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScrollView.ScrollToBottom();
        }
    }
}
