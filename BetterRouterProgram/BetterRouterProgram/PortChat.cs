
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Windows.Controls;
using System.Windows.Media;

namespace BetterRouterProgram
{
    public class PortChat
    {
        static bool moveOn;
        static SerialPort serialPort;
        static ComboBox portNameDD;

        public static void Run(MainWindow m)
        {
            portNameDD = m.portNameDD as ComboBox;
            portNameDD.Background = Brushes.LightGray;
            portNameDD.MaxWidth = (m.Width) / 7;
            portNameDD.MaxHeight = (m.Height) / 8;

            FillPortNames();

            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
            Thread readThread = new Thread(Read);

            // Create a new SerialPort object with default settings.
            serialPort = new SerialPort();

            // Set the read/write timeouts
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;

            serialPort.Open();
            moveOn = true;
            readThread.Start();

            while (!moveOn)
            {
                
            }

            readThread.Join();
            serialPort.Close();
        }

        public static void Read()
        {
            while (!moveOn)
            {
                try
                {
                    
                }
                catch (TimeoutException) {

                }
            }
        }

        // Display Port values and prompt user to enter a port.
        public static void FillPortNames()
        {

            //Console.WriteLine("Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                ComboBoxItem cBoxItem = new ComboBoxItem();
                cBoxItem.Content = s;
                portNameDD.Items.Add(cBoxItem);
            }

        }
    }
}
