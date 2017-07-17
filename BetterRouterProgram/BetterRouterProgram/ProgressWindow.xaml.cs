
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
        /// Sends a reboot message to the router and closes the Serial connection
        /// </summary>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void HandleReboot(object sender, RoutedEventArgs e)
        {
            FunctionUtil.HandleReboot();
            Thread.Sleep(750);
            HandleDisconnect(sender, e);
        }

        /// <summary>
        /// Disconnects the host from the router
        /// </summary>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void HandleDisconnect(object sender, RoutedEventArgs e)
        {
            SerialConnection.CloseConnection();

            Thread.Sleep(250);

            //close the window
            Close();
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
