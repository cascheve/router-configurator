using System;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;

namespace BetterRouterProgram
{
    public class SerialConnection
    {
        private static SerialPort SerialPort = null;

        public static void Connect(string portName, string initPassword, string sysPassword, string routerID, string configDir)
        {
            try
            {
                //Thread readThread = new Thread(ReadFromConnection);

                InitializeSerialPort(portName);

                FunctionUtil.StartTftp(configDir);

                ProgressWindow pw = new ProgressWindow();
                pw.Show();
                FunctionUtil.InitializeProgressWindow(pw);

                FunctionUtil.SetTime();
                FunctionUtil.PromptReboot();

                //CloseConnection()
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

        private static void InitializeSerialPort(string comPort) {
            SerialPort = new SerialPort(comPort, 9600);

            SerialPort.ReadTimeout = 500;
            SerialPort.WriteTimeout = 500;

            SerialPort.Open();
        }

        public static void CloseConnection() {
            SerialPort.Close();

            FunctionUtil.StopTftp();
        }

        private static void ResetConnectionBuffers() {
            SerialPort.DiscardInBuffer();
            SerialPort.DiscardOutBuffer();
        }

        private static string ReadResponse(char endChar) {
            char currentResponse = ' ';
            string response = "";

            while (true){
                currentResponse = (char)(SerialPort.ReadChar());
                response += currentResponse;

		        if(currentResponse == endChar){
			        break;
                }   
            }

            return response;
        }

        public static string RunInstruction(string instruction) {
            ResetConnectionBuffers();
	        SerialPort.Write(instruction + "\r\n");
	        string message = ReadResponse('#');
	
	        return message;
        }

        public static bool Login(string username, string password) {
            SerialPort.Write("\r\n");
            Thread.Sleep(500);

            SerialPort.Write(username + "\r\n");
            Thread.Sleep(500);

            SerialPort.Write(password + "\r\n");
            Thread.Sleep(500);

            bool retVal = false;

            //If the next line output by the router ends with the # char, we know login was successful
            if (ReadResponse('#').Length > 0) {
                return true;
            }

            return false;
        }

    }
}