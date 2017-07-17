
using System;
using System.Threading;
using System.IO.Ports;
using System.Collections.Generic;
using System.ComponentModel;

namespace BetterRouterProgram
{
    /// <summary>
    /// Runs the instructions necessary to program the router
    /// through the serial connection.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///     <listheader>
    ///         <description> Data ontained in this class/description>
    ///    </listheader>
    ///         <item>
    ///             <description>Serial connection information</description>
    ///         </item>\
    ///         <item>
    ///             <description>Settings for the router configuration</description>
    ///         </item>
    ///         <item>
    ///             <description>BackgroundWorker used for UI updates</description>
    ///         </item>
    ///</list>
    /// The neccessety of a BackgroundWorker caused <see cref="TransferWorkerDoWork"/>
    /// functionality to be in this clas rather than <see cref="FunctionUtil"/>.
    /// 
    /// The functionality of <see cref="Login"/> made it neccessary to put it in this class.
    /// </remarks>
    public class SerialConnection
    {
        /// <summary>
        /// SerialPort object use to communicate via serial with the router.
        /// </summary>
        private static SerialPort SerialPort = null;

        private static bool ConnectionLost = false;

        /// <summary>
        /// Data structure used to quickly access router
        /// configuration settings.
        /// </summary>
        private static Dictionary<string, string> Settings = null;

        /// <summary>
        /// List of files to transfer to the router
        /// </summary>
        private static List <string> FilesToTransfer = null;

        /// <summary>
        /// worker used to update ui asynchronous.
        /// </summary>
        private static BackgroundWorker TransferWorker = null;

        /// <summary>
        /// Class used to encaupsulate information to be passed to the <see cref="ProgressBar"/>
        /// </summary>
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

        /// <summary>
        /// Gets the setting value associated with the setting name.
        /// </summary>
        /// <param name="setting">The setting.</param>
        /// <returns> a string value associated 
        /// with the setting requested.</returns>
        public static string GetSetting(string setting)
        {
            return Settings[setting];
        }

        /// <summary>
        /// Initializes the serial connection and connects to the router
        /// </summary>
        /// <param name="extraFilesToTransfer">The extra files to transfer.</param>
        /// <remarks>'Extra' meaning other files that arent mandatory (e.g. xgsn, staticRP, antiacl)</remarks>
        /// <param name="settings">The settings for the router configuration.</param>
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
                        FunctionUtil.PromptDisconnect();
                    }
                }
                else
                {
                    FunctionUtil.UpdateProgressWindow("There was an Error establishing a connection to the Serial Port. \nPlease check your connection and try again");
                    FunctionUtil.PromptDisconnect();
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
                FunctionUtil.PromptDisconnect();
            }
            catch (Exception ex)
            {
                FunctionUtil.UpdateProgressWindow($"Original Error: {ex.Message}");
                FunctionUtil.PromptDisconnect();
            }
        }

        /// <summary>
        /// Logins with the specified username and password.
        /// </summary>
        /// <param name="username">The router username.</param>
        /// <param name="password">The current router password.</param>
        /// <returns> whether the login was successful or not.</returns>
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

        /// <summary>
        /// Transfers files to the router as a BackgroundWorker.
        /// This is where the router instructions are run and handled.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// Event handler: parameters need to stay constant.
        /// Transfer files being an async function, it needs to update the UI on the background.
        /// This is the only way to get it to work smoothly.
        /// </remarks>
        private static void TransferWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            double totalProgress = 50;

            ProgressMessage pm = new ProgressMessage("Transferring Configuration Files");
            TransferWorker.ReportProgress(0, pm);

            //TODO: change back to primary after testing
            RunInstruction(@"cd a:\test3");

            int i = 0;

            SerialPort.ReadTimeout = 50000;

            foreach (var file in FilesToTransfer)
            {
                Thread.Sleep(500);
                pm.MessageString = $"Transferring File '{FormatHostFile(file)}' as '{file}'";
                TransferWorker.ReportProgress(0, pm);
                Thread.Sleep(500);

                //attempt to copy the files from the host to the machine
                string message = RunInstruction(
                    String.Format("copy {0}:{1} {2}",
                    GetSetting("host ip address"),
                    FormatHostFile(file), file
                ));

                //update the progress window according to the file's transfer status
                if(message.EndsWith("DOWN"))
                {
                    pm.MessageString = $"Error: The ethernet connection was lost. {FormatHostFile(file)} could not be copied";
                    TransferWorker.ReportProgress(0, pm);
                    ConnectionLost = true;
                    return;
                }
                else if (message.Contains("File not found"))
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

            SerialPort.ReadTimeout = 750;
        }

        /// <summary>
        /// Updates the ProgressBar on a progress 
        /// change called by <see cref="TransferWorker.ReportProgress"/>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProgressChangedEventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// Event handler: parameters need to stay constant.
        /// This is only for handling TransferFiles functionality's progress change.
        /// </remarks>
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

        /// <summary>
        /// This function is run when <see cref="TransferWorkerDoWork"/> is completed.
        /// This then runs the rest of the functions to configure the router
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        /// /// <remarks>
        /// Event handler: parameters need to stay constant.
        /// This is only for handling TransferFiles functionality's completion.
        /// </remarks>
        private static void TransferWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (!ConnectionLost)
            {
                FunctionUtil.PromptDisconnect();
                return;
            }

            FunctionUtil.CopyToSecondary(new List<string>(FilesToTransfer));

            FunctionUtil.SetTime(GetSetting("timezone"));

            //FunctionUtil.SetPassword(GetSetting("system password"));

            FunctionUtil.PromptReboot();

            FunctionUtil.PromptDisconnect();
        }

        /// <summary>
        /// Closes the <see cref="SerialPort"/> connection.
        /// </summary>
        public static void CloseConnection()
        {
            if (SerialPort.IsOpen)
            {
                SerialPort.Close();
            }
        }

        /// <summary>
        /// Resets the connection buffers for <see cref="SerialPort"/>.
        /// </summary>
        /// <remarks> 
        /// This method is only called in the <see cref="RunInstruction"/> function
        /// </remarks>
        private static void ResetConnectionBuffers()
        {
            if (SerialPort.IsOpen)
            {
                SerialPort.DiscardInBuffer();
                SerialPort.DiscardOutBuffer();
            }
        }

        /// <summary>
        /// Reads the response from the input buffer.
        /// </summary>
        /// <param name="endChar">Character that terminates the input buffer listener.</param>
        /// <returns>The whole response from the input buffer</returns>
        /// <remarks> 
        /// This method is only called in the <see cref="RunInstruction"/> function.
        /// The whole message returned is just after a run instruction.
        /// This means that the message is the response from the instruction.
        /// </remarks>
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
                    return response;
                }
                else if(response.EndsWith("DOWN"))
                {
                    return response;
                }
            }
        }

        /// <summary>
        /// Sends the instruction to the output buffer, 
        /// which sends it to the router to be run there.
        /// </summary>
        /// <param name="instruction">The instruction to be run.</param>
        /// <returns> the response from the router after the instruction is run.</returns>
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

        /// <summary>
        /// Initializes the serial port/connection.
        /// </summary>
        /// <param name="comPort">The COM port.</param>
        /// <returns> Whether the connection was able to be opened</returns>
        private static bool InitializeSerialPort(string comPort)
        {
            try
            {
                SerialPort = new SerialPort(comPort, 9600);
                SerialPort.ReadTimeout = 750;
                SerialPort.WriteTimeout = 500;
                SerialPort.Open();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Initializes the connection.
        /// </summary>
        /// <param name="extraFilesToTransfer">The extra files to transfer.</param>
        /// <param name="settings">The settings.</param>
        /// <returns></returns>
        /// <remarks> Named this metod with a lack of a better one. 
        /// If a better name comes up, please rename it here and <see cref"InitializeConnection"/>
        /// </remarks>
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

        /// <summary>
        /// Creates the list of all the files to transfer.
        /// </summary>
        /// <param name="extraFilesToTransfer">The extra files to transfer.</param>
        /// <remarks>'Extra' meaning other files that arent mandatory (e.g. xgsn, staticRP, antiacl)</remarks>
        private static void SetFilesToTransfer(Dictionary<string, bool> extraFilesToTransfer)
        {
            //TODO: uncomment boot.ppc after testing
            FilesToTransfer = new List<string>();
            FilesToTransfer.Add("boot.ppc");
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

        /// <summary>
        /// Formats the host file.
        /// Used in see <cref="TransferWorkerDoWork"/>
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>The host file as a string in the correct format.</returns>
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