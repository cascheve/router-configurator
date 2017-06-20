
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
        static ComboBox baudRateDD;

        public static void Run(MainWindow m)
        {
            portNameDD = m.portNameDD as ComboBox;
            portNameDD.Background = Brushes.LightGray;
            portNameDD.MaxWidth = (m.Width) / 7;
            portNameDD.MaxHeight = (m.Height) / 15;


            baudRateDD = m.baudRateDD as ComboBox;
            baudRateDD.Background = Brushes.LightGray;
            baudRateDD.MaxWidth = (m.Width) / 7;
            baudRateDD.MaxHeight = (m.Height) / 15;


            string name;
            string message;
            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
            Thread readThread = new Thread(Read);

            // Create a new SerialPort object with default settings.
            serialPort = new SerialPort();

            // Allow the user to set the appropriate properties.
            serialPort.PortName = SetPortName(serialPort.PortName);
            serialPort.BaudRate = SetPortBaudRate(serialPort.BaudRate);
            serialPort.Parity = SetPortParity(serialPort.Parity);
            serialPort.DataBits = SetPortDataBits(serialPort.DataBits);
            serialPort.StopBits = SetPortStopBits(serialPort.StopBits);
            serialPort.Handshake = SetPortHandshake(serialPort.Handshake);

            // Set the read/write timeouts
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;

            serialPort.Open();
            moveOn = true;
            readThread.Start();

            Console.Write("Name: ");
            name = Console.ReadLine();

            Console.WriteLine("Type QUIT to exit");

            while (!moveOn)
            {
                message = Console.ReadLine();

                if (stringComparer.Equals("quit", message))
                {
                    moveOn = false;
                }
                else
                {
                    serialPort.WriteLine(
                        String.Format("<{0}>: {1}", name, message));
                }
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
                    string message = serialPort.ReadLine();
                    Console.WriteLine(message);
                }
                catch (TimeoutException) { }
            }
        }

        // Display Port values and prompt user to enter a port.
        public static string SetPortName(string defaultPortName)
        {

            string portName = defaultPortName;

            //Console.WriteLine("Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                //Console.WriteLine("   {0}", s);
                ComboBoxItem cBoxItem = new ComboBoxItem();
                cBoxItem.Content = s;
                portNameDD.Items.Add(cBoxItem);
            }

            //Console.Write("Enter COM port value (Default: {0}): ", defaultPortName);
            //portName = Console.ReadLine();
            //portName = "COM2";

            if (portName == "" || !(portName.ToLower()).StartsWith("com"))
            {
                portName = defaultPortName;
            }
            return portName;
        }

        // Display BaudRate values and prompt user to enter a value.
        public static int SetPortBaudRate(int defaultPortBaudRate)
        {
            string baudRate = "9600";

            //Console.Write("Baud Rate(default:{0}): ", defaultPortBaudRate);
            //baudRate = Console.ReadLine();

            //if (baudRate == "")
            //{
            //    baudRate = defaultPortBaudRate.ToString();
            //}

            return int.Parse(baudRate);
        }

        // Display PortParity values and prompt user to enter a value.
        public static Parity SetPortParity(Parity defaultPortParity)
        {
            string parity = "";

            //Console.WriteLine("Available Parity options:");
            //foreach (string s in Enum.GetNames(typeof(Parity)))
            //{
            //    Console.WriteLine("   {0}", s);
            //}

            //Console.Write("Enter Parity value (Default: {0}):", defaultPortParity.ToString(), true);
            //parity = Console.ReadLine();

            if (parity == "")
            {
                parity = defaultPortParity.ToString();
            }

            return (Parity)Enum.Parse(typeof(Parity), parity, true);
        }
        // Display DataBits values and prompt user to enter a value.
        public static int SetPortDataBits(int defaultPortDataBits)
        {
            string dataBits = "";

            //Console.Write("Enter DataBits value (Default: {0}): ", defaultPortDataBits);
            //dataBits = Console.ReadLine();

            if (dataBits == "")
            {
                dataBits = defaultPortDataBits.ToString();
            }

            return int.Parse(dataBits.ToUpperInvariant());
        }

        // Display StopBits values and prompt user to enter a value.
        public static StopBits SetPortStopBits(StopBits defaultPortStopBits)
        {
            string stopBits = "";

            //Console.WriteLine("Available StopBits options:");
            //foreach (string s in Enum.GetNames(typeof(StopBits)))
            //{
            //    Console.WriteLine("   {0}", s);
            //}

            //Console.Write("Enter StopBits value (None is not supported and \n" +
            // "raises an ArgumentOutOfRangeException. \n (Default: {0}):", defaultPortStopBits.ToString());
            //stopBits = Console.ReadLine();

            if (stopBits == "")
            {
                stopBits = defaultPortStopBits.ToString();
            }

            return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
        }

        public static Handshake SetPortHandshake(Handshake defaultPortHandshake)
        {
            string handshake = "";

            //Console.WriteLine("Available Handshake options:");
            //foreach (string s in Enum.GetNames(typeof(Handshake)))
            //{
            //    Console.WriteLine("   {0}", s);
            //}

            //Console.Write("Enter Handshake value (Default: {0}):", defaultPortHandshake.ToString());
            //handshake = Console.ReadLine();

            if (handshake == "")
            {
                handshake = defaultPortHandshake.ToString();
            }

            return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
        }
    }
}
