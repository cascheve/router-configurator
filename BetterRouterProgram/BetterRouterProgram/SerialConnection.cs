using System;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;
using System.Collections.Generic;

namespace BetterRouterProgram
{
    /*TODO
        - add ipaddr to dict
        - go over ping_test logic
        - do checkbox stuff
        - go over python script and look at where prompts are needed
     */
    public class SerialConnection
    {
        private static SerialPort SerialPort = null;
        private static string ConfigurationDirectory = "";
        private static Dictionary<string, string> Settings = null;

        public static void Connect(string portName, string initPassword, string sysPassword, 
            string routerID, string configDir, string timezone, 
            string hostIpAddr, Dictionary<string, bool> filesToTransfer)
        {
            Settings = new Dictionary<string, string>()
            {
                {"port", portName},
                {"initial password", initPassword},
                {"system password", sysPassword},
                {"router ID", routerID},
                {"config directory", configDir},
                {"timezone", timezone},
                {"host ip address", hostIpAddr}
            };

            try
            {
                ConfigurationDirectory = configDir;

                FunctionUtil.SetFilesToTransfer(filesToTransfer);

                InitializeSerialPort(portName);


                ProgressWindow pw = new ProgressWindow();
                pw.Show();
                FunctionUtil.InitializeProgressWindow(ref pw);

                if (FunctionUtil.Login("root", "P25CityX2015!"))
                {
                    //FunctionUtil.StartTftp();

                    //FunctionUtil.PingTest();

                    //FunctionUtil.TransferFiles();

                    //FunctionUtil.CopyToSecondary();

                    //FunctionUtil.SetTime(timezone);

                    //FunctionUtil.SetPassword("P25CityX2015!");

                    //FunctionUtil.PromptReboot();

                }

                FunctionUtil.PromptDisconnect();

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

        //TODO: make a bool guy
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
            if (SerialPort.IsOpen)
            {
                Thread.Sleep(500);

                SerialPort.Write("\r\n");
                Thread.Sleep(500);

                SerialPort.Write(username + "\r\n");
                Thread.Sleep(500);

                SerialPort.Write(password + "\r\n");
                Thread.Sleep(500);

                //If the next line output by the router ends with the # char, we know login was successful
                if (ReadResponse('#').Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

    }
}