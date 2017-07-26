
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
        //reference used to update the MainWindow from FunctionUtil, where the Router IDs are updated
        private static MainWindow MWRef;

        Dictionary<string, List<string>> PskList;

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
            
            foreach(var line in ethernetInfo.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)) 
            {
                lineSecondHalf = line.Split(':')[1].Substring(1);
                if(line.TrimStart().StartsWith("IPv4 Address")) 
                {
                    m.hostIP.Text = lineSecondHalf;
                    break;
                }
                else if(lineSecondHalf.Equals("Media disconnected")) 
                {
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

                        //initializePSK list
                        PskList = new Dictionary<string, List<string>>();
                        PSKProfile.Items.Clear();
                        PSKValue.Clear();

                        if(File.Exists(fbd.SelectedPath + @"\PSK.cfg")) {
                            StreamReader pskFile = new StreamReader(fbd.SelectedPath + @"\PSK.cfg");
                            string line = "";
                            string currentID = "";
                            while((line = pskFile.ReadLine()) != null) 
                            {
                                if(line.StartsWith("[") && line.EndsWith("]")) 
                                {
                                    currentID = line.Substring(1, line.Length-2);
                                    PskList.Add(currentID, new List<string>());

                                    ComboBoxItem cBoxItem = new ComboBoxItem();
                                    cBoxItem.Content = currentID;
                                    PSKProfile.Items.Add(cBoxItem);
                                }
                                else if(line.Length > 1 && line.Contains("ADD")) 
                                {
                                    PskList[currentID].Add((new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")).Matches(line)[0].ToString());
                                }
                            }
                        }

                        //shortens the path for cleanliness
                        filepathText.Text = fbd.SelectedPath.Length > 35 ? fbd.SelectedPath.Substring(0, 35) + "..." : fbd.SelectedPath;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("Error: " + ex.Message);
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
            bool PSKCheck = false;

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
                    case "PSK.cfg":
                        PSKCheck = true;
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

            PSKProfile.IsEnabled = PSKCheck;
            PSKValue.IsEnabled = PSKCheck;
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
            errorText.Text = "";

            if (this.hostIP.Text.Equals(string.Empty))
            {
                errorText.Text = "Please fill in the host IP address";
            }
            else if (portNameDD.Text.Equals(string.Empty))
            {
                errorText.Text = "Please fill in the port number";
            }
            else if (sysPassword.Text.Equals(string.Empty))
            {
                errorText.Text = "Please fill in the system password";
            }
            else if (routerID_DD.Text.Equals(string.Empty))
            {
                errorText.Text = "Please select the router's ID";
            }
            else if (filepathToolTip.Text.Equals(string.Empty))
            {
                errorText.Text = "Please fill the configuration file directory";
            }
            else if (!File.Exists(filepathToolTip.Text + @"\tftpd32.exe"))
            {
                errorText.Text = "tftpd32.exe not found in directory";
            }
            else if (secretPassword.Equals(string.Empty))
            {
                errorText.Text = "Please select the router's secret";
            }
            else
            {

                if (PSKProfile.IsEnabled)
                {
                    if (PSKProfile.Equals(string.Empty))
                    {
                        errorText.Text = "Please select the PSK profile";
                        return;
                    }
                    else if (PSKValue.Equals(string.Empty))
                    {
                        errorText.Text = "Please select the PSK value";
                        return;
                    }
                }

                foreach (var file in Directory.GetFiles(filepathToolTip.Text, "*.cfg").Select(Path.GetFileName).ToArray()) 
                {
                    if(file.StartsWith(routerID_DD.Text) && !file.Contains("_acl")) 
                    {
                        routerID_DD.Text = file.Substring(0, file.Length - 4);
                        break;
                    }
                }

                SerialConnection.InitializeAndConnect(
                    GetCheckboxContents(TransferGrid),
                    GetCheckboxContents(CopyGrid),
                    PskList.Count != 0 ? PskList[PSKProfile.Text] : null,
                    RebootCheckbox.IsChecked.HasValue? RebootCheckbox.IsChecked.Value : false,
                    NoAclRename.IsChecked.HasValue? NoAclRename.IsChecked.Value : false,
                    portNameDD.Text, currentPassword.Text, sysPassword.Text, 
                    secretPassword.Text, routerID_DD.Text, filepathToolTip.Text,
                    this.hostIP.Text, PSKProfile.Text, PSKValue.Text, EthernetPort.Text
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
        }

        private List <string> GetCheckboxContents(Visual parent)
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
                            if (checkbox.Content.ToString().Equals("acl.cfg") && (bool)NoAclRename.IsChecked)
                            {
                                //this is done so that the acl file is correctly renamed
                                allChecks.Add("noacl.cfg");
                            }
                            else
                            {
                                allChecks.Add(checkbox.Content.ToString());
                            }
                        }
                    }

                }
            }

            return allChecks;
        }
    }
}


