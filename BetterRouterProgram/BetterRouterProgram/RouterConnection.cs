
using System;
using System.Threading;
using System.IO.Ports;
using System.Net;
using System.Net.NetworkInformation;

namespace BetterRouterProgram
{
    public class RouterConnection
    {
        static bool moveOn;
        static SerialPort serialPort;

        public static void Connect(string portName, string initPassword, string sysPassword, string routerID, string configDir)
        {
            
            try
            {
                StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
                Thread readThread = new Thread(Read);

                // Create a new SerialPort object with default settings.
                serialPort = new SerialPort(portName, 9600);

                // Set the read/write timeouts
                serialPort.ReadTimeout = 500;
                serialPort.WriteTimeout = 500;

                serialPort.Open();
                moveOn = true;
                readThread.Start();
                System.Diagnostics.Process.Start(configDir + "\\tftpd32.exe");

                ProgressWindow pw = new ProgressWindow();
                pw.Show();

                while (!moveOn)
                {
                    //serialPort.Write()   
                    System.Diagnostics.Process.Start(configDir + "\\tftpd32.exe");

                    Ping pingSender = new Ping();
                    IPAddress address = IPAddress.Loopback;
                    PingReply reply = pingSender.Send(address);

                    if (reply.Status == IPStatus.Success)
                    {
                       
                        Console.WriteLine("Address: {0}", reply.Address.ToString());
                        Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                        Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                        Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                        Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
                    }
                }

                readThread.Join();
                serialPort.Close();
            }

            //TODO: Better Exception Handling
            catch (System.IO.FileNotFoundException)
            {
                System.Windows.Forms.MessageBox.Show("Unable to locate the Specified File, please try again.");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                System.Windows.Forms.MessageBox.Show("Error: Could not find the TFTP Client executable in the folder specified. Please move the TFTP Application File (.exe) into the desired directory or choose a different directory and try again.");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Original Error: " + ex.Message + "\n" + ex.HelpLink);
            }

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
    }
}
