using System;

using System.Threading;
using System.IO.Ports;

namespace BetterRouterProgram
{
    public class SerialConnection
    {
        static SerialPort serialPort = null;
        static bool moveOn = false;

        //public static SerialPort getInstance()
        //{
        //    if (serialPort == null)
        //    {
        //        InitializeSerialPort();
        //    }

        //    return serialPort;
        //}

        public static void Connect(string portName, string initPassword, string sysPassword, string routerID, string configDir)
        {

            try
            {
                //Thread readThread = new Thread(ReadFromConnection);

                InitializeSerialPort(portName);

                moveOn = true;
                //readThread.Start();
                System.Diagnostics.Process.Start(configDir + "\\tftpd32.exe");

                ProgressWindow pw = new ProgressWindow();
                pw.Show();

                ReadFromConnection("");

                Thread.Sleep(1000);

                while (!moveOn)
                {
                    //WriteToConnection
       
                }

                //readThread.Join();
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

        public static void InitializeSerialPort(string comPort) {
            serialPort = new SerialPort(comPort, 9600);

            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;

            serialPort.Open();

            char[] buffer = { '\r', '\n' };

            serialPort.Write(buffer, 0, 2);
        }

        public static string ReadFromConnection(string endChar) {
            string message = "";

            char[] buffer = new char[16];
            serialPort.Read(buffer, 0, 16);

            message = string(buffer);

            return message;

        }

        public static void WriteToConnection(string message) {

        }

    }
}