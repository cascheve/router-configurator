
using System;
using System.Threading;
using System.IO.Ports;

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
                    serialPort.Write("\r\n");
                    Thread.Sleep(500);

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
                System.Windows.Forms.MessageBox.Show("Original Error: " + ex.Message);
            }

        }

        public static void Read()
        {

            while (!moveOn)
            {
                try
                {
                    //serialPort.Read();
                }
                catch (TimeoutException) {

                }
            }
        }     
    }
}
