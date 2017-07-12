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
        private static BackgroundWorker transferWorker = new BackgroundWorker();
        private static ProgressWindow pw = new ProgressWindow();

        public class progressMessage
        {
            public string message;
            public double amountToAdd;
        }

        public static void Connect(string portName, string currentPassword, string systemPassword,
            string routerID, string configDir, string timezone,
            string hostIpAddr, Dictionary<string, bool> filesToTransfer)
        {
            Settings = new Dictionary<string, string>()
            {
                {"port", portName},
                {"initial password", currentPassword},
                {"system password", systemPassword},
                {"router ID", routerID},
                {"config directory", configDir},
                {"timezone", timezone},
                {"host ip address", hostIpAddr}
            };

            pw.Show();
            FunctionUtil.InitializeProgressWindow(ref pw);

            try
            {
                //TODO: make the reference to the TFTP window more resilient
                FunctionUtil.StartTftp();

                //sets the progress window to the top, "most visible" element
                pw.Topmost = true;

                //a separate worker thread that takes care of the transferring of files
                //this is done to allow responsive GUI updates
                transferWorker.DoWork += transferWorker_DoWork;
                transferWorker.RunWorkerCompleted += transferWorker_RunWorkerCompleted;
                transferWorker.ProgressChanged += transferWorker_ProgressChanged;
                transferWorker.WorkerReportsProgress = true;

                ConfigurationDirectory = configDir;

                FunctionUtil.SetFilesToTransfer(filesToTransfer);

                if (InitializeSerialPort(portName))
                {
                    //FunctionUtil.Login("root", currentPassword)
                    if (FunctionUtil.Login("root", ""))
                    {
                        //TODO: Make Pingtest another backgroundWorker? -> cannot transfer files otherwise (no idea why)
                        FunctionUtil.PingTest();

                        //this will run, and upon completion the worker will proceed with the remaining functions
                        transferWorker.RunWorkerAsync();

                    }
                    else
                    {
                        FunctionUtil.UpdateProgressWindow("There was an Error logging into the Router. Check your login information and try again.");
                    }
                }
                else
                {
                    FunctionUtil.UpdateProgressWindow("There was an Error establishing a connection to the Serial Port. Please check your connection and try again");
                }
            }

            catch (System.IO.FileNotFoundException)
            {
                FunctionUtil.UpdateProgressWindow("Unable to locate the Specified File, please try again.");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                FunctionUtil.UpdateProgressWindow("Error: Could not find the TFTP Client executable in the folder specified. Please move the TFTP Application File (.exe) into the desired directory or choose a different directory and try again.");
            }
            catch (TimeoutException)
            {
                FunctionUtil.UpdateProgressWindow("Connection Attempt timed out. \nCheck your Serial Connection and try again.");
            }
            catch (Exception ex)
            {
                FunctionUtil.UpdateProgressWindow($"Original Error: {ex.Message}");
                CloseConnection();
            }

        }

        private static void transferWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressMessage pm = e.UserState as progressMessage;

            FunctionUtil.UpdateProgressWindow(
                pm.message, 
                FunctionUtil.Progress.TransferFilesStart, 
                pm.amountToAdd
                );
        }

        private static void transferWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // run all background tasks here

            double totalProgress = 50;

            progressMessage pm = new progressMessage();
            pm.message = "Transferring Configuration Files";
            pm.amountToAdd = 0;

            transferWorker.ReportProgress(0, pm);

            RunInstruction(@"cd a:\test\test1");

            int i = 0;
            string hostFile = "";

           
            foreach (var file in FunctionUtil.FilesToTransfer)
            {

                hostFile = FunctionUtil.FormatHostFile(file);

                pm.message = $"Transferring File: {hostFile} -> {file}";
                transferWorker.ReportProgress(0, pm);

                string message = RunInstruction(String.Format("copy {0}:{1} {2}",
                    GetSetting("host ip address"),
                    hostFile, file
                ));

                if (message.Contains("File not found"))
                {
                    pm.message = $"Error: {hostFile} not found in host configuration directory";
                    transferWorker.ReportProgress(0, pm);
                }

                else if (message.Contains("Cannot route"))
                {
                    pm.message = "Cannot connect to the Router via TFTP. \nCheck your ethernet connection.";
                    transferWorker.ReportProgress(0, pm);
                }

                else
                {
                    
                    pm.message = $"{hostFile} Successfully Transferred";
                    pm.amountToAdd = (((double)totalProgress) / FunctionUtil.FilesToTransfer.Count) * (++i);
                    
                    transferWorker.ReportProgress(0, pm);
                }
            }
        }

        private static void transferWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //FunctionUtil.CopyToSecondary();

            //FunctionUtil.SetTime(timezone);

            //FunctionUtil.SetPassword(systemPassword);

            FunctionUtil.PromptReboot();

            FunctionUtil.PromptDisconnect();

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