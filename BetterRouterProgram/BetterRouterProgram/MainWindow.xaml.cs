﻿
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
            fbd.RootFolder = Environment.SpecialFolder.Personal;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //Get folder name
                try
                {
                    if ((myStream = fbd.SelectedPath) != null)
                    {
                        //read the stream
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
            string uString = this.initPassword.Text;
            string pString = this.sysPassword.Text;
            string routerID = this.routerID.Text;
            string configFP = this.browseText.Text;

       

        }
    }
}