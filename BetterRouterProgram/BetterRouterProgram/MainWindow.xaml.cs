
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using Microsoft.Win32;

namespace BetterRouterProgram
{

    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            this.portNameDD.Background = Brushes.LightGray;
            this.portNameDD.MaxWidth = (this.Width) / 7;
            this.portNameDD.MaxHeight = (this.Height) / 8;

            FillPortNames(this);
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

        private void browseFiles(object sender, RoutedEventArgs e)
        {

            String myStream = null;
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            fbd.Description = "Select the Directory holding the Configuration Files";
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
                    System.Windows.Forms.MessageBox.Show("Error: Could not read Folder from disk. Original error: " + ex.Message);
                }
            }
            else
            {
                //TODO: If the Dialog Fails, Handle
            }

            /*
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            //DialogResult.OK
            if (openFileDialog1.ShowDialog() == false)
            {
                try
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                            // Insert code to read the stream here.
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }*/

        }

        private void attemptConnection(object sender, RoutedEventArgs e)
        {
            string comPort = this.portNameDD.Text;
            string iString = this.initPassword.Text;
            string sString = this.sysPassword.Text;
            string routerID = this.routerID.Text;
            string configDir = this.filepathToolTip.Text;

            errorText.Text = "";

            //TODO: Make sure all fields are filled
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
            else
            {
                RouterConnection.Connect(comPort, iString, sString, routerID, configDir);
            }

        }
    }
}
