
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
        //TODO requires firewall be down or allow it; windows firewall inclusion?
        //TODO Allow program to run https://support.symantec.com/en_US/article.TECH104526.html
        //TODO Account for K Core issues -talk to Roy
        //TODO adapt to HP Switch  Configurator

        //reference used to update the MainWindow from FunctionUtil, where the Router IDs are updated once files have been moved
        private static MainWindow MWRef;

        //used to hold the PSK router keys and the corresponding commands for the particular router
        Dictionary<string, List<string>> PskList;

        ///<summary>
        ///Initializes a new instance of the <see cref="MainWindow"/> class.
        ///The COM ports available to the user and the host IP address are filled on startup
        ///A reference to this window is made, effectively making this a basic singleton
        ///</summary>
        public MainWindow()
        {
            InitializeComponent();

            FillPortNames(this);
            FillHostIP(this);

            MWRef = this;
        }        

        ///<summary>
        ///Programmatically locates the available COM ports on the host computer and fills the DropDown list
        ///</summary>
        ///<param name="m">A reference to the window object</param>
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

        ///<summary>
        ///Gets the Host's Ethernet Adapter IP address and fills in the text block with that address
        ///</summary>
        ///<param name="m">Reference to 'this' main window</param>
        private static void FillHostIP(MainWindow m)
        {
            //The simplest way found to get the Ethernet Adapter IP is to 
            //start a new command line window and use the stdout from the ipconfig command
            Process pProcess = new Process();
            pProcess.StartInfo.FileName = "ipconfig";
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.Start();

            string output = pProcess.StandardOutput.ReadToEnd();
            pProcess.WaitForExit();
            Thread.Sleep(200);

            //this default IP will yield a bad connection if a connection is attempted 
            //the following lines split the output from ipconfig into parts that contain the IPv4 address and remaining characters
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

        ///<summary>
        ///A helper function to update the List of IDs from outside of this class
        ///Called on <see cref="MoveCompletedFiles"/> to indicate that files have been moved and should not be selectable
        ///</summary>
        public static void UpdateIDs()
        {
            MWRef.PopulateIDs(SerialConnection.GetSetting("config directory"));
        }

        ///<summary>
        ///Updates the List of IDs using the configuration directory selected
        ///</summary>
        ///<param name="directory">The configuration directory holding the desired files</param>
        private void PopulateIDs(string directory)
        {
            routerID_DD.Items.Clear();

            List<string> routerIDs = new List <string>(Directory.GetFiles(directory, "*.cfg").Select(Path.GetFileName));

            string fileName = "";
            string currentID = "";
            string lastID = "";
            ComboBoxItem cBoxItem = null;

            for (int i = 0; i < routerIDs.Count; i++)
            {
                fileName = routerIDs[i];

                if (fileName.StartsWith("z0") || fileName.StartsWith("cen") || fileName.StartsWith("cs"))
                {
                    currentID = fileName.Split('_')[0];
                    if (currentID.Contains(".cfg"))
                    {
                        currentID = currentID.Remove(currentID.Length - 4, 4);
                    }

                    if (fileName.Contains("ggsn") && i + 2 < routerIDs.Count && routerIDs[i + 1].Split('_')[0].Equals(currentID) && routerIDs[i + 2].Split('_')[0].Equals(currentID))
                    {
                        cBoxItem = new ComboBoxItem();
                        cBoxItem.Content = currentID;
                        routerID_DD.Items.Add(cBoxItem);
                        i += 2;
                        continue;
                    }
                    if (fileName.Contains("acl") && currentID.Equals(lastID) && !lastID.Contains("ggsn"))
                    {
                        cBoxItem = new ComboBoxItem();
                        cBoxItem.Content = lastID;
                        routerID_DD.Items.Add(cBoxItem);
                    }

                    lastID = currentID;
                }
            }

            if (cBoxItem != null)
            {
                cBoxItem.IsSelected = true;
            }
            else
            {
                throw new Exception("The selected folder does not contain a valid set of files.");
            }
            //((ComboBoxItem)routerID_DD.Items[0]).IsSelected = true;
        }

        ///<summary>
        ///When the Router ID selected has been changed determine if there are XGSN files that can be selected
        ///</summary>
        ///<param name="sender">The sender of the selection change</param>
        ///<param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void RouterIDSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (filepathToolTip.Text.Equals(string.Empty) || routerID_DD.Text.Equals(string.Empty))
            {
                return;
            }


            bool xgsnCheck = false;

            foreach (var file in Directory.GetFiles(filepathToolTip.Text, "*.cfg").Select(Path.GetFileName))
            {
                //TODO: if e.AddedItems.Length < 1 --> continue
                if (e.AddedItems.Count < 1)
                {
                    break;
                }

                if (e.AddedItems[0].ToString().Split(':').Length > 1)
                {
                    if (file.StartsWith(e.AddedItems[0].ToString().Split(':')[1].Trim()) && file.Contains("_xgsn"))
                    {
                        xgsnCheck = true;
                        break;
                    }
                }
            }

            xgsn_transfer.IsEnabled = xgsnCheck;
            xgsn_transfer.IsChecked = xgsnCheck;
            xgsn_copy.IsEnabled = xgsnCheck;
            xgsn_copy.IsChecked = xgsnCheck;
        }

        ///<summary>
        ///Directs the user to browse for the configuration directory containing the configuration files and TFTP application
        ///</summary>
        ///<param name="sender">The sender of the browse event</param>
        ///<param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void BrowseFiles(object sender, RoutedEventArgs e)
        {
            String myStream = null;
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            fbd.Description = "Select the directory holding the configuration (*.cfg) files and .acl files";
            fbd.ShowNewFolderButton = false;
            //errorText.Text = fbd.SelectedPath;
             
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

                        //creates /Completed and /Logs directories for progress logging
                        if (!Directory.Exists(fbd.SelectedPath + @"\Completed")) 
                        {
                            Directory.CreateDirectory(fbd.SelectedPath + @"\Completed");
                        }
                        if (!Directory.Exists(fbd.SelectedPath + @"\Logs")) 
                        {
                            Directory.CreateDirectory(fbd.SelectedPath + @"\Logs");
                        }

                        //initialize the PSK list with IDs and their commands
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
                    if (ex.Message.Contains("reference"))
                    {
                        System.Windows.Forms.MessageBox.Show("Error: The selected directory is invalid.");
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Error: " + ex.Message);
                    }
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

        ///<summary>
        ///Updates the files that can be selected based on the currently chosen directory
        ///</summary>
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
            staticrp_copy.IsChecked = staticrpCheck;

            antiacl_transfer.IsEnabled = antiaclCheck;
            antiacl_transfer.IsChecked = antiaclCheck;
            antiacl_copy.IsEnabled = antiaclCheck;
            antiacl_copy.IsChecked = antiaclCheck;

            ppc_transfer.IsEnabled = ppcCheck;
            ppc_transfer.IsChecked = ppcCheck;

            PSKProfile.IsEnabled = PSKCheck;
            PSKValue.IsEnabled = PSKCheck;
        }

        ///<summary>
        ///Called when the window is closing, used to clean up the tftpd64 application
        ///</summary>
        ///<param name="sender">The sender of the close event</param>
        ///<param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void OnWindowClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //FunctionUtil.StopTftp();
        }

        ///<summary>
        ///Attempts to connect to the router using the information provided by the user. 
        ///Spawns a progress window that then updates the user on all successes and failures.
        ///</summary>
        ///<param name="sender">The sender.</param>
        ///<param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void AttemptConnection(object sender, RoutedEventArgs e)
        {
            errorText.Text = "";

            if (this.hostIP.Text.Equals("0.0.0.0"))
            {
                errorText.Text = "Please adjust your Ethernet Adapter settings";
            }
            else if (portNameDD.Text.Equals(string.Empty))
            {
                errorText.Text = "Please fill in the port number";
            }
            //else if (sysPassword.Text.Equals(string.Empty))
            //{
            //    errorText.Text = "Please fill in the system password";
            //}
            else if (routerID_DD.Text.Equals(string.Empty))
            {
                errorText.Text = "Please select the router's ID";
            }
            else if (filepathToolTip.Text.Equals(string.Empty))
            {
                errorText.Text = "Please fill the configuration file directory";
            }
            //else if (!File.Exists(filepathToolTip.Text + @"\tftpd64.exe"))
            //{
            //    errorText.Text = "tftpd64.exe not found in selected directory";
            //}
            //else if (secretPassword.Equals(string.Empty))
            //{
            //    errorText.Text = "Please select the router's secret";
            //}
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

                string routerID = "";

                //Before making the connection, adjust the router ID displayed so that it can be properly used in the connection
                foreach (var file in Directory.GetFiles(filepathToolTip.Text, "*.cfg").Select(Path.GetFileName).ToArray()) 
                {
                    if(file.StartsWith(routerID_DD.Text) && !file.Contains("_acl")) 
                    {
                        routerID = file.Substring(0, file.Length - 4);
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
                    secretPassword.Text, routerID, filepathToolTip.Text,
                    this.hostIP.Text, PSKProfile.Text, PSKValue.Text, EthernetPort.Text
                );
            }
        }

        ///<summary>
        ///Can be called by the user if they change their IP address
        ///</summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        private void refreshIP_Click(object sender, RoutedEventArgs e)
        {
            FillHostIP(this);
        }

        private void refreshPorts_Click(object sender, RoutedEventArgs e)
        {
            FillPortNames(this);
        }

        /// <summary>
        /// Gets the file checkbox contents to be transferred/copied to the router
        /// </summary>
        /// <param name="parent">The grid (or general visual component) that the checkboxes reside in</param>
        /// <returns></returns>
        private List <string> GetCheckboxContents(Visual parent)
        {
            List<string> allChecks = new List<string>();

            if (parent != null)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);

                for (int i = 0; i < count; i++)
                {
                    //Retrieve child visual (checkbox) at the specified index value.
                    Visual child = VisualTreeHelper.GetChild(parent, i) as Visual;

                    if (child != null)
                    {
                        System.Windows.Controls.CheckBox checkbox = (System.Windows.Controls.CheckBox)child;

                        if ((bool)checkbox.IsChecked)
                        {
                            if (checkbox.Content.ToString().Equals("acl.cfg") && (bool)NoAclRename.IsChecked)
                            {
                                //this is done so that the acl file is correctly renamed for K core systems
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


