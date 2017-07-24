
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
        private static MainWindow MWRef= null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// This includes filling the port and timezone DropDown lists
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            portNameDD.Background = Brushes.LightGray;

            FillPortNames(this);
            FillHostIP(this);

            MWRef = this;
        }

        /// <summary>
        /// Programmatically locates the available COM ports on the host computer and fills the DropDown list
        /// </summary>
        /// <param name="m">A reference to the window object</param>
        private static void FillPortNames(MainWindow m)
        {
            m.portNameDD.Items.Clear();

            foreach (string s in SerialPort.GetPortNames())
            {
                ComboBoxItem cBoxItem = new ComboBoxItem();
                cBoxItem.Content = s;
                m.portNameDD.Items.Add(cBoxItem);
            }
        }

        /// <summary>
        /// Gets the IP address and fills in the text block with the address
        /// </summary>
        /// <param name="m">Reference to 'this' main window</param>
        private static void FillHostIP(MainWindow m)
        {
            Process pProcess = new Process();
            pProcess.StartInfo.FileName = "ipconfig";
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.Start();

            // split the output around the newlines
            string output = pProcess.StandardOutput.ReadToEnd();
            pProcess.WaitForExit();
            Thread.Sleep(200);

            //get the ethernet adapter ip address
            m.hostIP.Text = "0.0.0.0";
            string lineSecondHalf = "";
            int start = output.IndexOf("Ethernet adapter Ethernet:") + "Ethernet adapter Ethernet:".Length;
            string ethernetInfo =  output.Substring(start).Split(new string[] {"\r\n\r\n"}, 2, StringSplitOptions.RemoveEmptyEntries)[0];
            
            foreach(var line in ethernetInfo.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)) {
                lineSecondHalf = line.Split(':')[1].Substring(1);
                if(line.TrimStart().StartsWith("IPv4 Address")) {
                    m.hostIP.Text = lineSecondHalf;
                    break;
                }
                else if(lineSecondHalf.Equals("Media disconnected")) {
                    break;
                }
            }
        }

        /// <summary>
        /// Updates the ID list for the routers in the current directory.
        /// Called on <see cref="MoveCompletedFiles"/>
        /// </summary>
        public static void UpdateIDs()
        {
            MWRef.PopulateIDs(SerialConnection.GetSetting("config directory"));
        }

        /// <summary>
        /// Populates the ID list using the files found inside the configuration directory
        /// </summary>
        /// <param name="directory">The directory.</param>
        private void PopulateIDs(string directory)
        {
            routerID_DD.Items.Clear();

            Dictionary<string, int> configFiles = new Dictionary<string, int>();
            string currentID = "";
            ComboBoxItem cBoxItem = null;

            foreach (var file in Directory.GetFiles(directory, "*.cfg").Select(Path.GetFileName))
            {
                if(file.StartsWith("z0") || file.StartsWith("cen"))
                {
                    currentID = file.Split('_')[0];
                    try 
                    {
                        if((configFiles[currentID] == 2 && currentID.Contains("ggsn"))
                            || configFiles[currentID] == 1) 
                        {
                            cBoxItem = new ComboBoxItem();
                            cBoxItem.Content = currentID;
                            routerID_DD.Items.Add(cBoxItem);
                        }
                        else {
                            configFiles[currentID]++;
                        }
                    }
                    catch(KeyNotFoundException) 
                    {
                        configFiles.Add(currentID, 1);
                    }
                }
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

            foreach (var file in Directory.GetFiles(filepathToolTip.Text, "*.cfg").Select(Path.GetFileName))
            {
                if (file.StartsWith(routerID_DD.Text) && file.Contains("_xgsn"))
                {
                    xgsnCheck = true;
                }
            }

            xgsn_transfer.IsEnabled = xgsnCheck;
            xgsn_transfer.IsChecked = xgsnCheck;
            xgsn_copy.IsEnabled = xgsnCheck;
            xgsn_copy.IsChecked = false;
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
                        PopulateIDs(fbd.SelectedPath);
                        UpdateFileOptions();

                        //creates /done and /logs directories
                        if (!Directory.Exists(fbd.SelectedPath + @"\Completed")) 
                        {
                            Directory.CreateDirectory(fbd.SelectedPath + @"\Completed");
                        }
                        if (!Directory.Exists(fbd.SelectedPath + @"\Logs")) 
                        {
                            Directory.CreateDirectory(fbd.SelectedPath + @"\Logs");
                        }

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
            bool ppcCheck = false;

            foreach (var file in Directory.GetFiles(filepathToolTip.Text).Select(Path.GetFileName))
            {
                switch(file)
                {
                    case "staticRP.cfg":
                        staticrpCheck = true;
                        break;
                    case "antiacl.cfg":
                        antiaclCheck = true;
                        break;
                    case "boot.ppc":
                        ppcCheck = true;
                        break;
                    default:
                        break;
                }
            }

            staticrp_transfer.IsEnabled = staticrpCheck;
            staticrp_transfer.IsChecked = staticrpCheck;
            staticrp_copy.IsEnabled = staticrpCheck;
            staticrp_copy.IsChecked = false;

            antiacl_transfer.IsEnabled = antiaclCheck;
            antiacl_transfer.IsChecked = antiaclCheck;
            antiacl_copy.IsEnabled = antiaclCheck;
            antiacl_copy.IsChecked = false;

            ppc_transfer.IsEnabled = ppcCheck;
            ppc_transfer.IsChecked = ppcCheck;
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
            string comPort = portNameDD.Text;
            string iString = currentPassword.Text;
            string sString = sysPassword.Text;
            string routerID = routerID_DD.Text;
            string configDir = filepathToolTip.Text;
            string secret = secretPassword.Text;
            string hostIP = this.hostIP.Text;
            errorText.Text = "";

            //TODO: passwords must comply with IA structure

            if (hostIP.Equals(string.Empty))
            {
                errorText.Text = "Please fill in the host IP address";
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
            else if (secret.Equals(string.Empty))
            {
                errorText.Text = "Please select the router's secret";
            }
            else
            {
                foreach(var file in Directory.GetFiles(configDir, "*.cfg").Select(Path.GetFileName).ToArray()) {
                    if(file.StartsWith(routerID) && !file.Contains("_acl")) {
                        routerID = file.Substring(0, file.Length - 4);
                        break;
                    }
                }

                //List<string> filesToTransfer = new List<string>();
                //List<string> filesToCopy = new List<string>();
                //string currentCheckBox = "";

                //foreach (Control c in this.Controls)
                //{
                //    if (c is CheckBox)
                //    {
                //        if (true/*TODO check if c is checked*/)
                //        {
                //            currentCheckBox = c.Name.ToString();
                //            if (currentCheckBox.EndsWith("copy"))
                //            {
                //                filesToCopy.Add(currentCheckBox);
                //            }
                //            else if (currentCheckBox.EndsWith("transfer"))
                //            {
                //                filesToTransfer.Add(currentCheckBox);
                //            }
                //        }
                //    }
                //}

                //TODO: Iterate over all checkboxes or sets of CBs and add them to respective lists if checked

                SerialConnection.InitializeAndConnect(
                    new Dictionary<string, bool>()
                    {
                        {staticrp_transfer.Content.ToString(),
                            staticrp_transfer.IsChecked.HasValue ? staticrp_transfer.IsChecked.Value : false},
                        {antiacl_transfer.Content.ToString(),
                            antiacl_transfer.IsChecked.HasValue ? antiacl_transfer.IsChecked.Value : false},
                        {xgsn_transfer.Content.ToString(),
                            xgsn_transfer.IsChecked.HasValue ? xgsn_transfer.IsChecked.Value : false},
                        {ppc_transfer.Content.ToString(),
                            ppc_transfer.IsChecked.HasValue ? ppc_transfer.IsChecked.Value : false},
                        {cfg_transfer.Content.ToString(),
                            cfg_transfer.IsChecked.HasValue ? cfg_transfer.IsChecked.Value : false},
                        {acl_transfer.Content.ToString(),
                            acl_transfer.IsChecked.HasValue ? acl_transfer.IsChecked.Value : false}
                    },
                    new Dictionary<string, bool>()
                    {
                        {staticrp_copy.Content.ToString(),
                            staticrp_copy.IsChecked.HasValue ? staticrp_copy.IsChecked.Value : false},
                        {antiacl_copy.Content.ToString(),
                            antiacl_copy.IsChecked.HasValue ? antiacl_copy.IsChecked.Value : false},
                        {xgsn_copy.Content.ToString(),
                            xgsn_copy.IsChecked.HasValue ? xgsn_copy.IsChecked.Value : false},
                        {ppc_copy.Content.ToString(),
                            ppc_copy.IsChecked.HasValue ? ppc_copy.IsChecked.Value : false},
                        {cfg_copy.Content.ToString(),
                            cfg_copy.IsChecked.HasValue ? cfg_copy.IsChecked.Value : false},
                        {acl_copy.Content.ToString(),
                            acl_copy.IsChecked.HasValue ? acl_copy.IsChecked.Value : false}
                    },
                    rebootCheckbox.IsChecked.HasValue ? rebootCheckbox.IsChecked.Value : false,
                    comPort, iString, sString, secret, 
                    routerID, configDir, hostIP
                );
            }
        }

        /// <summary>
        /// Refreshes the host IP address.
        /// Called by the user when they change their IP address
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refreshIP_Click(object sender, RoutedEventArgs e)
        {
            FillHostIP(this);
        }

        private void refreshPorts_Click(object sender, RoutedEventArgs e)
        {
            FillPortNames(this);

            foreach (var checkbox in GetCheckboxContents(TransferGrid))
            {
                currentPassword.Text += checkbox.ToString() + '\n';
            }
        }

        private static List <string> GetCheckboxContents(Visual parent)
        {
            List<string> allChecks = new List<string>();

            if (parent != null)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);

                for (int i = 0; i < count; i++)
                {
                    // Retrieve child visual at specified index value.
                    Visual child = VisualTreeHelper.GetChild(parent, i) as Visual;

                    if (child != null)
                    {
                        System.Windows.Controls.CheckBox checkbox = (System.Windows.Controls.CheckBox)child;

                        if ((bool)checkbox.IsChecked)
                        {
                            allChecks.Add(checkbox.Content.ToString());
                        }
                    }

                }
            }

            return allChecks;
        }
    }
}


