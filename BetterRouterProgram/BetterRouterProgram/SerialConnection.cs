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
        private static Dictionary<string, string> Settings = null;
        private static List <string> FilesToTransfer = null;
        private static BackgroundWorker TransferWorker = null;

        private class ProgressMessage
        {
            public string MessageString { get; set; }
            public double AmountToAdd { get; set; }

            public ProgressMessage(string message = "", double amountToAdd = 0)
            {
                MessageString = message;
                AmountToAdd = amountToAdd;
            }
        }

        public static string GetSetting(string setting)
        {
            return Settings[setting];
        }

        public static void InitializeAndConnect(Dictionary<string, bool> extraFilesToTransfer, params string[] settings)
        {
            try
            {
                if (InitializeConnection(extraFilesToTransfer, settings))
                {
                    //Login("root", currentPassword)
                    if (Login("root", "P25LACleco2016!"))
                    {
                        if (FunctionUtil.PingTest()){
                            //this will run, and upon completion the worker will proceed with the remaining functions
                            TransferWorker.RunWorkerAsync();
                        }
                        else
                        {
                            FunctionUtil.PromptDisconnect();
                        }
                    }
                    else
                    {
                        FunctionUtil.UpdateProgressWindow("There was an Error logging into the Router. \nCheck your login information and try again.");
                        CloseConnection();
                    }
                }
                else
                {
                    FunctionUtil.UpdateProgressWindow("There was an Error establishing a connection to the Serial Port. \nPlease check your connection and try again");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                FunctionUtil.UpdateProgressWindow("Unable to locate the Specified File, please try again.");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                FunctionUtil.UpdateProgressWindow("Error: Could not find the TFTP Client executable in the folder specified. \nPlease move the TFTP Application File (.exe) into the desired directory or choose a different directory and try again.");
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

        private static bool Login(string username, string password)
        {
            FunctionUtil.UpdateProgressWindow("Logging In");

            if (!SerialPort.IsOpen)
            {
                FunctionUtil.UpdateProgressWindow("**Login Unsuccessful**", FunctionUtil.Progress.None);
                return false;
            }
            
            Thread.Sleep(500);

            SerialPort.Write("\r\n");
            Thread.Sleep(500);

            SerialPort.Write(username + "\r\n");
            Thread.Sleep(500);

            SerialPort.Write(password + "\r\n");
            Thread.Sleep(500);

            //if the serial connection fails using the username and password specified
            if (ReadResponse('#').Length > 0) {
                FunctionUtil.UpdateProgressWindow("Login Successful", FunctionUtil.Progress.Login);
                return true;
            }
            
            return false;
        }

        private static void TransferWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            double totalProgress = 50;

            ProgressMessage pm = new ProgressMessage("Transferring Configuration Files");
            TransferWorker.ReportProgress(0, pm);

            //TODO: change back after testing
            RunInstruction(@"cd a:\test3");

            int i = 0;

            foreach (var file in FilesToTransfer)
            {
                Thread.Sleep(500);
                pm.MessageString = $"Transferring File: {FormatHostFile(file)} -> {file}";
                Thread.Sleep(500);
                TransferWorker.ReportProgress(0, pm);
                Thread.Sleep(500);

                //attempt to copy the files from the host to the machine
                string message = RunInstruction(
                    String.Format("copy {0}:{1} {2}",
                    GetSetting("host ip address"),
                    FormatHostFile(file), file
                ));

                //update the progress window according to the file's transfer status
                if (message.Contains("File not found"))
                {
                    pm.MessageString = $"Error: {FormatHostFile(file)} not found in host configuration directory";
                    TransferWorker.ReportProgress(0, pm);
                }
                else if (message.Contains("Cannot route"))
                {
                    pm.MessageString = "Cannot connect to the Router via TFTP. \nCheck your ethernet connection.";
                    TransferWorker.ReportProgress(0, pm);
                }
                else
                {
                    pm.MessageString = $"{FormatHostFile(file)} Successfully Transferred";
                    pm.AmountToAdd = ((totalProgress) / FilesToTransfer.Count) * (++i);

                    TransferWorker.ReportProgress(0, pm);
                }
            }
        }

        private static void TransferWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressMessage pm = e.UserState as ProgressMessage;

            FunctionUtil.UpdateProgressWindow(
                pm.MessageString, 
                FunctionUtil.Progress.TransferFilesStart, 
                pm.AmountToAdd
            );

            Thread.Sleep(200);
        }

        private static void TransferWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //FunctionUtil.CopyToSecondary();

            //FunctionUtil.SetTime(GetSetting("timezone"));

            //FunctionUtil.SetPassword(GetSetting("system password"));

            FunctionUtil.PromptReboot();

            FunctionUtil.PromptDisconnect();
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

        private static bool InitializeSerialPort(string comPort)
        {
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

        private static bool InitializeConnection(Dictionary<string, bool> extraFilesToTransfer, string[] settings)
        {
            Settings = new Dictionary<string, string>()
            {
                {"port", settings[0]},
                {"initial password", settings[1]},
                {"system password", settings[2]},
                {"router ID", settings[3]},
                {"config directory", settings[4]},
                {"timezone", settings[5]},
                {"host ip address", settings[6]}
            };

            FunctionUtil.InitializeProgressWindow();

            TransferWorker = new BackgroundWorker();

            try 
            {
                FunctionUtil.StartTftp();
                    
                //a separate worker thread that takes care of the transferring of files
                //this is done to allow responsive GUI updates
                TransferWorker.DoWork += TransferWorkerDoWork;
                TransferWorker.RunWorkerCompleted += TransferWorkerCompleted;
                TransferWorker.ProgressChanged += TransferWorkerProgressChanged;
                TransferWorker.WorkerReportsProgress = true;

                SetFilesToTransfer(extraFilesToTransfer);
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

            return InitializeSerialPort(settings[0]);
        }

        private static void SetFilesToTransfer(Dictionary<string, bool> extraFilesToTransfer)
        {
            FilesToTransfer = new List<string>();
            //FilesToTransfer.Add("boot.ppc");
            FilesToTransfer.Add("boot.cfg");
            FilesToTransfer.Add("acl.cfg");

            foreach (var file in extraFilesToTransfer.Keys)
            {
                if(extraFilesToTransfer[file])
                {
                    FilesToTransfer.Add(file);
                }
            }
        }

        private static string FormatHostFile(string file) {
            file = file.Trim();

            switch(file) {
                case "staticRP.cfg":
                case "antiacl.cfg":
                case "boot.ppc":
                    return file;
                case "acl.cfg":
                case "xgsn.cfg":
                    return SerialConnection.GetSetting("router ID") + "_" + file;
                case "boot.cfg":
                    return SerialConnection.GetSetting("router ID") + ".cfg";;
                default:
                    return "";
            }
        }
    }

}