using System;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;

namespace BetterRouterProgram
{
    public class SerialConnection
    {
        private static SerialPort SerialPort = null;
        private static string ConfigurationDirectory = "";
        private static Dictionary<string, string> Settings = null;
        private static BackgroundWorker bw = new BackgroundWorker();

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

            ProgressWindow pw = new ProgressWindow();
            pw.Show();
            FunctionUtil.InitializeProgressWindow(ref pw);

            try
            {
                FunctionUtil.StartTftp();

                ConfigurationDirectory = configDir;

                FunctionUtil.SetFilesToTransfer(filesToTransfer);

                if (InitializeSerialPort(portName))
                {

                    if (FunctionUtil.Login("root", ""))
                    {

                        //FunctionUtil.PingTest();

                        //FunctionUtil.SetTime(timezone);

                        //FunctionUtil.SetPassword("P25CityX2015!");

                        FunctionUtil.TransferFiles();

                        //FunctionUtil.CopyToSecondary();
                            
                        //FunctionUtil.PromptReboot();

                    }
                    else
                    {
                        pw.currentTask.Text += "\nThere was an Error logging into the Router. Check your login information and try again.";
                    }

                    FunctionUtil.PromptDisconnect();
                }
                else
                {
                    pw.currentTask.Text += "\nThere was an Error establishing a connection to the Serial Port. Please check your connection and try again";
                }
            }

            catch (System.IO.FileNotFoundException)
            {
                pw.currentTask.Text += "\nUnable to locate the Specified File, please try again.";
            }
            catch (System.ComponentModel.Win32Exception)
            {
                pw.currentTask.Text += "\nError: Could not find the TFTP Client executable in the folder specified. Please move the TFTP Application File (.exe) into the desired directory or choose a different directory and try again.";
            }
            catch (TimeoutException)
            {
                pw.currentTask.Text += "\nConnection Attempt timed out. \nCheck your Serial Connection and try again.";
            }
            catch (Exception ex)
            {
                pw.currentTask.Text += "\nOriginal Error: " + ex.Message;
                CloseConnection();
            }

        }

        public static string GetSetting(string setting)
        {
            return Settings[setting];
        }

        private static bool InitializeSerialPort(string comPort)
        {
            //copy 10.10.10.100:/eos_enc_cd_16.7.1.22/router_setup_template.txt boot.txt
            try
            {
                SerialPort = new SerialPort(comPort, 9600);

                SerialPort.ReadTimeout = 50000;
                SerialPort.WriteTimeout = 500;

                SerialPort.Open();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void CloseConnection()
        {

            if (SerialPort.IsOpen)
            {
                SerialPort.Close();
            }
        }

        private static void ResetConnectionBuffers()
        {
            if (SerialPort.IsOpen)
            {
                SerialPort.DiscardInBuffer();
                SerialPort.DiscardOutBuffer();
            }
        }

        private static string ReadResponse(char endChar = '#')
        {
            char currentResponse = ' ';
            string response = "";

            while (true)
            {
                currentResponse = (char)(SerialPort.ReadChar());
                response += currentResponse;

                if (currentResponse == endChar)
                {
                    break;
                }
            }

            return response;
        }

        public static string RunInstruction(string instruction)
        {

            string retVal = "Unable to Run: No Connection to the Serial Port";

            if (SerialPort.IsOpen)
            {
                ResetConnectionBuffers();
                SerialPort.Write(instruction + "\r\n");
                retVal = ReadResponse();
            }

            return retVal;
        }

        public static bool Login(string username, string password)
        {
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