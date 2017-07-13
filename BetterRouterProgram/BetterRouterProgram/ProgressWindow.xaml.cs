using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BetterRouterProgram
{
  
    public partial class ProgressWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressWindow"/> class.
        /// </summary>
        public ProgressWindow()
        {
            InitializeComponent();
            progressBar.Value = 0.5;
        }

        /// <summary>
        /// Handles the reboot.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void HandleReboot(object sender, RoutedEventArgs e)
        {
            FunctionUtil.HandleReboot();
            Thread.Sleep(750);
            HandleDisconnect(sender, e);
        }

        /// <summary>
        /// Handles the disconnect.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void HandleDisconnect(object sender, RoutedEventArgs e)
        {
            SerialConnection.CloseConnection();

            Thread.Sleep(250);

            //close the window
            Close();
        }

        //if the window is clsoed by the user
        /// <summary>
        /// Handles the Closing event of the DataWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
        void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            SerialConnection.CloseConnection();
        }
    }
}
