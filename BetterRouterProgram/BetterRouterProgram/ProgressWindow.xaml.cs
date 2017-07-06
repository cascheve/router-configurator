﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
        public ProgressWindow()
        {
            InitializeComponent();
            progressBar.Value = 0.5;

        }

        private void HandleReboot(object sender, RoutedEventArgs e)
        {
            FunctionUtil.HandleReboot();
        }

        private void HandleDisconnect(object sender, RoutedEventArgs e)
        {
            //TODO: Handle a Disconnect
            //SerialConnection.CloseConnection();
            this.Close();
        }

        void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            SerialConnection.CloseConnection();
        }
    }
}
