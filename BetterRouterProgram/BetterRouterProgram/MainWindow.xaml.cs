
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using Microsoft.Win32;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace BetterRouterProgram
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            portNameDD.Background = Brushes.LightGray;

            FillPortNames(this);
            FillTimeZones(this);
        }

        private static void FillPortNames(MainWindow m)
        {
            foreach (string s in SerialPort.GetPortNames())
            {
                ComboBoxItem cBoxItem = new ComboBoxItem();
                cBoxItem.Content = s;
                m.portNameDD.Items.Add(cBoxItem);
            }
        }

        private static void FillTimeZones(MainWindow m)
        {
            foreach (TimeZoneInfo z in TimeZoneInfo.GetSystemTimeZones())
            {
                ComboBoxItem tBoxItem = new ComboBoxItem();
                tBoxItem.Content = z.DisplayName;
                m.timeZoneDD.Items.Add(tBoxItem);
            }
        }

        private void FillID_DD(string directory)
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

        private void DepopulateID_DD()
        {
            routerID_DD.Items.Clear();
        }

        private void BrowseFiles(object sender, RoutedEventArgs e)
        {
            String myStream = null;
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            fbd.Description = "Select the directory holding the configuration (.cfg) files";
            fbd.ShowNewFolderButton = true;
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
                        DepopulateID_DD();
                        FillID_DD(fbd.SelectedPath);
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

        private void AttemptConnection(object sender, RoutedEventArgs e)
        {
            string comPort = this.portNameDD.Text;
            string iString = this.currentPassword.Text;
            string sString = this.sysPassword.Text;
            string routerID = this.routerID_DD.Text;
            string configDir = this.filepathToolTip.Text;
            string timezone = this.timeZoneDD.Text;
            string hostIP = this.hostIP.Text;
            errorText.Text = "";

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

                SerialConnection.Connect(comPort, iString, sString, routerID, 
                    configDir, timezone, hostIP,
                    new Dictionary<string, bool>()
                    {
                        {staticrp.Content.ToString(),
                            staticrp.IsChecked.HasValue ? staticrp.IsChecked.Value : false},
                        {antiacl.Content.ToString(),
                            antiacl.IsChecked.HasValue ? antiacl.IsChecked.Value : false},
                        {xgsn.Content.ToString(),
                            xgsn.IsChecked.HasValue ? xgsn.IsChecked.Value : false}
                    }
                );
            }
        }

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

        private void routerID_DD_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FunctionUtil.StopTftp();
        }
    }
}


