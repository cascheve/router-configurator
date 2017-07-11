
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
    //TODO: For the file checkboxes at the bottom, verify that the files exist before allowing the user to select the files

    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            portNameDD.Background = Brushes.LightGray;

            FillPortNames(this);
            FillTimeZones(this);

        }

        public static void FillPortNames(MainWindow m)
        {

            //Console.WriteLine("Available Ports:");
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

        //fill the router d dropdown list with router IDs
        private void FillID_DD(string directory)
        {
            string[] tempFiles = Directory.GetFiles(directory);
            List<string> configFiles = new List<string>();
            
            foreach (string t in tempFiles)
            {
                configFiles.Add(Path.GetFileName(t));
            }

            //use LINQueries to select only valid router IDs
            List<string> validRouterIDs = 
                (from file in configFiles
                where file.Contains("z0") || file.Contains("cen")
                orderby file ascending
                select file.Split('_')[0]).Distinct().ToList();


            foreach (string c in validRouterIDs)
            {
                ComboBoxItem cBoxItem = new ComboBoxItem();
                cBoxItem.Content = c;
                routerID_DD.Items.Add(cBoxItem);
            }
        }

        private void DepopulateID_DD()
        {
            routerID_DD.Items.Clear();
        }

        private void browseFiles(object sender, RoutedEventArgs e)
        {

            String myStream = null;
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            fbd.Description = "Select the Directory holding the Configuration (.cfg) Files";
            fbd.ShowNewFolderButton = true;
            errorText.Text = fbd.SelectedPath;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //Get folder name
                try
                {
                    if ((myStream = fbd.SelectedPath) != null && (myStream != ""))
                    {
                        //Selected Path is the Absolute path selected (as a string)
                        filepathToolTip.Text = fbd.SelectedPath;

                        //refill the router ID list with valid router IDs
                        DepopulateID_DD();
                        FillID_DD(fbd.SelectedPath);

                        //shortens the path for cleanliness
                        if (fbd.SelectedPath.Length > 35)
                        {
                            filepathText.Text = fbd.SelectedPath.Substring(0, 35) + "...";
                        }
                        else
                        {
                            filepathText.Text = fbd.SelectedPath;
                        }
                    }
                }
                catch (Exception ex)
                {
                    //TODO: Better Exception Handling
                    System.Windows.Forms.MessageBox.Show("Error selecting the given folder: " + ex.Message);
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("There was an Error Displaying the application window. Please exit and try again.");
            }

        }

        private void attemptConnection(object sender, RoutedEventArgs e)
        {
            string comPort = this.portNameDD.Text;
            string iString = this.currentPassword.Text;
            string sString = this.sysPassword.Text;
            string routerID = this.routerID_DD.Text;
            string configDir = this.filepathToolTip.Text;
            string timezone = this.timeZoneDD.Text;
            string hostIP = "10.1.1.2";

            errorText.Text = "";

            if (comPort.Equals(""))
            {
                errorText.Text = "Please fill in the Port Number";
            }
            else if (sString.Equals(""))
            {
                errorText.Text = "Please fill in the System Password";
            }
            else if (routerID.Equals(""))
            {
                errorText.Text = "Please fill in the Router's ID";

            }
            else if (configDir.Equals(""))
            {
                errorText.Text = "Please fill the Configuration File Directory";
            }
            else if (timezone.Equals(""))
            {
                errorText.Text = "Please select a Time Zone";
            }
            else
            {
                //getting full router id
                string[] configFiles = Directory.GetFiles(configDir, "*.cfg");
                foreach(var file in configFiles) {
                    if(file.StartsWith(routerID) && !file.Contains("_acl")) {
                        routerID = file.Substring(0, file.Length - ".cfg".Length);
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
    }
}


