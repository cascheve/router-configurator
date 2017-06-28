using System;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;
using System.Collections.Generic;

namespace BetterRouterProgram
{
    /*
        - add ipaddr to dict
        - go over ping_test logic
        - do checkbox stuff
     */
    public class SerialConnection
    {
        private static SerialPort SerialPort = null;
        private static string ConfigurationDirectory = "";
        private static Dictionary<string, string> Settings = null;

        public static void Connect(string portName, string initPassword, string sysPassword, 
            string routerID, string configDir, string timezone)
        {
            Settings = new Dictionary<string, string>()
            {
                {"port", portName},
                {"initial password", initPassword},
                {"system password", sysPassword},
                {"router ID", routerID},
                {"config directory", configDir},
                {"timezone", timezone},
            };

            try
            {
                ConfigurationDirectory = configDir;

                InitializeSerialPort(portName);

                //FunctionUtil.StartTftp();

                ProgressWindow pw = new ProgressWindow();
                pw.Show();
                FunctionUtil.InitializeProgressWindow(ref pw);

                //change this to root , syspassword/initpassword
                FunctionUtil.Login("root", "P25CityX2016!");

                //FunctionUtil.SetPassword("P25CityX2015!");

                //FunctionUtil.SetTime(timezone);
                //FunctionUtil.PromptReboot();

                //CloseConnection();
            }

            //TODO: Better Exception Handling
            catch (System.IO.FileNotFoundException)
            {
                System.Windows.Forms.MessageBox.Show("Unable to locate the Specified File, please try again.");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                System.Windows.Forms.MessageBox.Show("Error: Could not find the TFTP Client executable in the folder specified. "
                + "Please move the TFTP Application File (.exe) into the desired directory or choose a different directory and try again.");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Original Error: " + ex.Message);
                CloseConnection();
            }

        }

        public static string GetSetting(string setting) {
            return Settings[setting];
        }

        private static void InitializeSerialPort(string comPort) {
            SerialPort = new SerialPort(comPort, 9600);

            SerialPort.ReadTimeout = 500;
            SerialPort.WriteTimeout = 500;

            SerialPort.Open();
        }

        public static void CloseConnection() {

            if (SerialPort.IsOpen)
            {
                SerialPort.Close();
            }

            FunctionUtil.StopTftp();
        }

        private static void ResetConnectionBuffers() {
            if (SerialPort.IsOpen)
            {
                SerialPort.DiscardInBuffer();
                SerialPort.DiscardOutBuffer();
            }
            //TODO: Handle else case -> throw exception??
        }

        private static string ReadResponse(char endChar = '#') {
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
            //TODO: is there a connection?
            ResetConnectionBuffers();
	        SerialPort.Write(instruction + "\r\n");
            return ReadResponse();
        }

        public static bool Login(string username, string password) {
            //TODO: is there a connection?
            Thread.Sleep(500);

            SerialPort.Write("\r\n");
            Thread.Sleep(500);

            SerialPort.Write(username + "\r\n");
            Thread.Sleep(500);

            SerialPort.Write(password + "\r\n");
            Thread.Sleep(500);

            //If the next line output by the router ends with the # char, we know login was successful
            if (ReadResponse('#').Length > 0) {
                return true;
            }

            return false;
        }

    }
}