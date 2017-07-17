
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace BetterRouterProgram
{
    public partial class MainWindow : Window
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// This includes filling the port and timezone DropDown lists
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            portNameDD.Background = Brushes.LightGray;

            FillPortNames(this);
            FillTimeZones(this);
        }

        /// <summary>
        /// Programmatically locates the available COM ports on the host computer and fills the DropDown list
        /// </summary>
        /// <param name="m">A reference to the window object</param>
        private static void FillPortNames(MainWindow m)
        {
            foreach (string s in SerialPort.GetPortNames())
            {
                ComboBoxItem cBoxItem = new ComboBoxItem();
                cBoxItem.Content = s;
                m.portNameDD.Items.Add(cBoxItem);
            }
        }

        /// <summary>
        /// Fills the time zones DropDown with all the possible timezones the router may be located in
        /// </summary>
        /// <param name="m">The m.</param>
        private static void FillTimeZones(MainWindow m)
        {
            foreach (TimeZoneInfo z in TimeZoneInfo.GetSystemTimeZones())
            {
                ComboBoxItem tBoxItem = new ComboBoxItem();
                tBoxItem.Content = z.DisplayName;
                m.timeZoneDD.Items.Add(tBoxItem);
            }
        }

        /// <summary>
        /// Depopulates the router ID list in case of a new configuration directory being selected
        /// </summary>
        private void DepopulateIDs()
        {
            routerID_DD.Items.Clear();
        }

        /// <summary>
        /// Populates the ID list using the files found inside the configuration directory
        /// </summary>
        /// <param name="directory">The directory.</param>
        private void PopulateIDs(string directory)
        {
            List<string> validRouterIDs = 
                (from file in Directory.GetFiles(directory, "*.cfg").Select(Path.GetFileName).ToArray()
                where file.Contains("z0") || file.Contains("cen")
                orderby file ascending
                select file.Split('_')[0]).Distinct().ToList();

            foreach (var id in validRouterIDs)
            {
                ComboBoxItem cBoxItem = new ComboBoxItem();
                cBoxItem.Content = id;
                routerID_DD.Items.Add(cBoxItem);
            }
        }

        /// <summary>
        /// When the configuration folder is changed, locate the configuration files
        /// </summary>
        /// <param name="sender">The sender of the selection change</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void RouterIDSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (filepathToolTip.Text.Equals(string.Empty) || routerID_DD.Text.Equals(string.Empty))
            {
                return;
            }

            bool xgsnCheck = false;

            foreach (var file in Directory.GetFiles(filepathToolTip.Text, "*.cfg").Select(Path.GetFileName).ToArray())
            {
                if (file.StartsWith(routerID_DD.Text) && file.Contains("_xgsn"))
                {
                    xgsnCheck = true;
                }
            }

            xgsn.IsEnabled = xgsnCheck;
            xgsn.IsChecked = false;
        }

        /// <summary>
        /// Browses for the configuration directory containing the configuration files and TFTP application
        /// </summary>
        /// <param name="sender">The sender of the browse event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void BrowseFiles(object sender, RoutedEventArgs e)
        {
            String myStream = null;
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            fbd.Description = "Select the directory holding the configuration (*.cfg) files and the TFTP Application.";
            fbd.ShowNewFolderButton = false;
            errorText.Text = fbd.SelectedPath;
             
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    if (!string.IsNullOrEmpty(myStream = fbd.SelectedPath))
                    {
                        //Selected Path is the Absolute path selected (as a string)
                        filepathToolTip.Text = fbd.SelectedPath;

                        //refill the router ID list with valid router IDs
                        DepopulateIDs();
                        PopulateIDs(fbd.SelectedPath);
                        UpdateFileOptions();

                        //shortens the path for cleanliness
                        filepathText.Text = fbd.SelectedPath.Length > 35? 
                            fbd.SelectedPath.Substring(0, 35) + "..." : fbd.SelectedPath;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("Error selecting the given folder: " + ex.Message);
                }
            }
            else
            {
                if (filepathToolTip.Text.Equals(""))
                {
                    System.Windows.Forms.MessageBox.Show("Please Select a Configuration Folder.");
                }
            }
        }

        /// <summary>
        /// Updates the file options based on the currently chosen configuration directory
        /// </summary>
        private void UpdateFileOptions()
        {
            bool staticrpCheck = false;
            bool antiaclCheck = false;

            foreach (var file in Directory.GetFiles(filepathToolTip.Text, "*.cfg").Select(Path.GetFileName).ToArray())
            {
                if (file.StartsWith("staticRP"))
                {
                    staticrpCheck = true;
                }
                else if (file.StartsWith("antiacl"))
                {
                    antiaclCheck = true;
                }
            }

            staticrp.IsEnabled = staticrpCheck;
            staticrp.IsChecked = false;

            antiacl.IsEnabled = antiaclCheck;
            antiacl.IsChecked = false;
        }

        /// <summary>
        /// Called when the window is closing, used to clean up the TFTP application
        /// </summary>
        /// <param name="sender">The sender of the close event</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void OnWindowClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FunctionUtil.StopTftp();
        }

        /// <summary>
        /// Attempts to connect to the router using the information provided by the user. 
        /// Spawns a progress window that thereby notifies the user of all successes and failures.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void AttemptConnection(object sender, RoutedEventArgs e)
        {
            string comPort = this.portNameDD.Text;
            string iString = this.currentPassword.Text;
            string sString = this.sysPassword.Text;
            string routerID = this.routerID_DD.Text;
            string configDir = this.filepathToolTip.Text;
            string timezone = this.timeZoneDD.Text;

            //string hostIP = "10.10.10.100";
            string hostIP = this.hostIP.Text;

            errorText.Text = "";

            if (hostIP.Equals(string.Empty))
            {
                errorText.Text = "Please fill in the host IP address";
            }
            else if ((new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")).Matches(hostIP).Count != 1)
            {
                errorText.Text = "Invalid IP address format";
            }
            else if (comPort.Equals(string.Empty))
            {
                errorText.Text = "Please fill in the port number";
            }
            else if (sString.Equals(string.Empty))
            {
                errorText.Text = "Please fill in the system password";
            }
            else if (routerID.Equals(string.Empty))
            {
                errorText.Text = "Please select the router's ID";
            }
            else if (configDir.Equals(string.Empty))
            {
                errorText.Text = "Please fill the configuration file directory";
            }
            else if (!File.Exists(configDir + @"\tftpd32.exe"))
            {
                errorText.Text = "tftpd32.exe not found in directory";
            }
            else if (timezone.Equals(string.Empty))
            {
                errorText.Text = "Please select a time zone";
            }
            else
            {
                foreach(var file in Directory.GetFiles(configDir, "*.cfg").Select(Path.GetFileName).ToArray()) {
                    if(file.StartsWith(routerID) && !file.Contains("_acl")) {
                        routerID = file.Substring(0, file.Length - 4);
                        break;
                    }
                }

                SerialConnection.InitializeAndConnect(
                    new Dictionary<string, bool>()
                    {
                        {staticrp.Content.ToString(),
                            staticrp.IsChecked.HasValue ? staticrp.IsChecked.Value : false},
                        {antiacl.Content.ToString(),
                            antiacl.IsChecked.HasValue ? antiacl.IsChecked.Value : false},
                        {xgsn.Content.ToString(),
                            xgsn.IsChecked.HasValue ? xgsn.IsChecked.Value : false}
                    },
                    comPort, iString, sString, routerID, 
                    configDir, timezone, hostIP
                );
            }
        }
    }
}


