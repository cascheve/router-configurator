
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO.Ports;

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

        private void attemptConnection(object sender, RoutedEventArgs e)
        {
            string comPort = this.portNameDD.Text;
            string uString = this.username.Text;
            string pString = this.password.Text;


        }
    }
}
