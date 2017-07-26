
using System;
using System.Threading;
using System.IO.Ports;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

//TODO  documentation, move login to functionutil
namespace BetterRouterProgram
{
    /// <summary>
    /// Runs the instructions necessary to program the router
    /// through the serial connection.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///     <listheader>
    ///         <description> Data contained in this class/description>
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
    /// </list>
    /// The neccessety of a BackgroundWorker caused <see cref="TransferWorkerDoWork"/>
    /// functionality to be in this clas rather than <see cref="FunctionUtil"/>.
    /// 
    /// The functionality of <see cref="Login"/> made it neccessary to put it in this class.
    /// </remarks>
    public class SerialConnection
    {
        /// <summary>
        /// Varialbe to store whether the user wants to reboot the router or not
        /// </summary>
        private static bool RebootStatus;

        /// <summary>
        /// Varialbe to store whether the user wants to rename the acl file to noacl
        /// </summary>
        private static bool RenameAcl;

        /// <summary>
        /// Variable to store whether the ethernet connection was lost
        /// </summary>
        private static bool ConnectionLost;
        
        /// <summary>
        /// Data structure used to quickly access router
        /// configuration settings.
        /// </summary>
        private static Dictionary<string, string> Settings;

        /// <summary>
        /// List of files to transfer to the router
        /// </summary>
        private static List <string> FilesToTransfer;
        
        /// <summary>
        /// List of IP addresses to set the psk for
        /// </summary>
        private static List <string> PskIPList;

        /// <summary>
        /// List of files to create a backup of
        /// </summary>
        private static List <string> FilesToCopy;

        /// <summary>
        /// SerialPort object use to communicate via serial with the router.
        /// </summary>
        private static SerialPort SerialPort;

        /// <summary>
        /// worker used to update ui asynchronous.
        /// </summary>
        private static BackgroundWorker TransferWorker;

        /// <summary>
        /// Class used to encaupsulate information to be passed to the <see cref="ProgressBar"/>
        /// </summary>
        private class ProgressMessage
        {
            public string Message { get; set; }
            public FunctionUtil.MessageType Type { get; set; }

            public ProgressMessage(string message = "", FunctionUtil.MessageType type = FunctionUtil.MessageType.Message)
            {
                Message = message;
                Type = type;
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
        /// Initializes the and connect.
        /// </summary>
        /// <param name="filesToTransfer">The files to transfer to the router.</param>
        /// <param name="filesToCopy">The files to copy from the primary to the secondary directory.</param>
        /// <param name="pskIPList">The list of ip addresses to set the psk for.</param>
        /// <param name="rebootStatus">if set to <c>true</c>, the router will reboot.</param>
        /// <param name="renameAcl">if set to <c>true</c>, the "acl.cfg" file will be renamed to "noacl.cfg".</param>
        /// <param name="settings">The list of connection and script settings</param>
        public static void InitializeAndConnect(List<string> filesToTransfer, List<string> filesToCopy, List<string> pskIPList, 
                                                bool rebootStatus, bool renameAcl, params string[] settings)
        {
            RebootStatus = rebootStatus;
            RenameAcl = renameAcl;
            FilesToTransfer = filesToTransfer;
            FilesToCopy = filesToCopy;
            PskIPList = pskIPList;

            try
            {
                if (InitializeConnection(settings, rebootStatus, pskIPList == null ? true : false))
                {
                    if (FunctionUtil.Login("root", GetSetting("current password")))
                    {
                        if (FunctionUtil.PingTest(settings[9])){
                            //this will run, and upon completion the worker will proceed with the remaining functions
                            TransferWorker.RunWorkerAsync();
                        }
                        else
                        {
                            FunctionUtil.ConfigurationFinished(RebootStatus, true);
                        }
                    }
                    else
                    {
                        FunctionUtil.UpdateProgress("Could not log into the Router." + 
                            "\nCheck your login information and try again.", FunctionUtil.MessageType.Error);
                        FunctionUtil.ConfigurationFinished(RebootStatus, true);
                    }
                }
                else
                {
                    FunctionUtil.UpdateProgress("A connection to the Serial Port could not be made." + 
                        "\nPlease check your connection and try again", FunctionUtil.MessageType.Error);
                    FunctionUtil.ConfigurationFinished(RebootStatus, true);
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                FunctionUtil.UpdateProgress("Unable to locate the Specified File, please try again.", FunctionUtil.MessageType.Error);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                FunctionUtil.UpdateProgress("Could not find the TFTP Client executable in the folder specified. "
                    +"\nPlease move the TFTP Application File (.exe) into the desired directory or choose a different directory and try again.", FunctionUtil.MessageType.Error);
            }
            catch (TimeoutException)
            {
                FunctionUtil.UpdateProgress("Connection Attempt timed out. \nCheck your Serial Connection and try again.", FunctionUtil.MessageType.Error);
                FunctionUtil.ConfigurationFinished(RebootStatus, true);
            }
            catch (Exception ex)
            {
                FunctionUtil.UpdateProgress(ex.Message, FunctionUtil.MessageType.Error);
                FunctionUtil.ConfigurationFinished(RebootStatus, true);
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
            FunctionUtil.UpdateProgress("Logging In", FunctionUtil.MessageType.Message);

            if (!SerialPort.IsOpen)
            {
                FunctionUtil.UpdateProgress("Login Unsuccessful: serial connection lost", FunctionUtil.MessageType.Error);
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
                FunctionUtil.UpdateProgress("Login Successful", FunctionUtil.MessageType.Success);
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
            ProgressMessage pm = new ProgressMessage("Transferring Configuration Files", FunctionUtil.MessageType.Message);
            TransferWorker.ReportProgress(0, pm);

            RunInstruction(@"cd a:\primary");

            try
            {
                foreach (var file in FilesToTransfer)
                {
                    if (file.Equals("boot.ppc"))
                    {
                        SerialPort.ReadTimeout = 30000;
                    }

                    string formattedHostFile = FormatHostFile(file);

                    Thread.Sleep(500);
                    pm.Message = $"Transferring File '{formattedHostFile}' as '{file}'";

                    TransferWorker.ReportProgress(0, pm);
                    Thread.Sleep(500);

                    //attempt to copy the files from the host to the machine
                    string message = RunInstruction(
                        String.Format("copy {0}:{1} {2}",
                        GetSetting("host ip address"),
                        formattedHostFile, file
                    ));

                    //update the progress window according to the file's transfer status
                    if (message.EndsWith("DOWN"))
                    {
                        pm.Message = $"Ethernet connection was lost. {formattedHostFile} could not be copied";
                        pm.Type =  FunctionUtil.MessageType.Error;
                        TransferWorker.ReportProgress(0, pm);
                        ConnectionLost = true;
                        return;
                    }
                    else if (message.Contains("File not found"))
                    {
                        pm.Message = $"{formattedHostFile} not found in host configuration directory";
                        pm.Type =  FunctionUtil.MessageType.Error;
                        TransferWorker.ReportProgress(0, pm);
                    }
                    else if (message.Contains("Cannot route"))
                    {
                        pm.Message = "Cannot connect to the Router via TFTP. \nCheck your ethernet connection and settings.";
                        pm.Type =  FunctionUtil.MessageType.Error;
                        TransferWorker.ReportProgress(0, pm);
                    }
                    else
                    {
                        pm.Message = $"{formattedHostFile} Successfully Transferred";
                        pm.Type =  FunctionUtil.MessageType.Success;
                        TransferWorker.ReportProgress(0, pm);
                    }
                }
            }
            catch (TimeoutException)
            {
                FunctionUtil.UpdateProgress("Connection Attempt timed out. \nCheck your Serial Connection and try again.", FunctionUtil.MessageType.Error);
                FunctionUtil.ConfigurationFinished(RebootStatus, true);
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

            FunctionUtil.UpdateProgress(pm.Message, pm.Type);

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
            try
            {
                if (ConnectionLost)
                {
                    FunctionUtil.UpdateProgress("Ethernet Connection has been Lost", FunctionUtil.MessageType.Error);
                    return;
                }

                FunctionUtil.CopyToSecondary(new List<string>(FilesToCopy));

                FunctionUtil.SetBootOrder();

                if (PskIPList != null)
                {
                    FunctionUtil.SetPsk(PskIPList);
                }

                //FunctionUtil.SetPassword(GetSetting("system password"));

                FunctionUtil.ConfigurationFinished(RebootStatus, false);
            }
            catch (TimeoutException)
            {
                FunctionUtil.UpdateProgress("Connection Attempt timed out. \nCheck your Serial Connection and try again.", FunctionUtil.MessageType.Error);
                FunctionUtil.ConfigurationFinished(RebootStatus, true);
            }

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
        private static string ReadResponse(char endChar)
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
        public static string RunInstruction(string instruction, char endChar = '#')
        {
            string retVal = "**Error: No Connection to the Serial Port";

            if (SerialPort.IsOpen)
            {
                ResetConnectionBuffers();
                SerialPort.Write(instruction + "\r\n");
                retVal = ReadResponse(endChar);
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

        public static void SetSerialPortTimeout(int timeout)
        {
            SerialPort.ReadTimeout = timeout;
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
        private static bool InitializeConnection(string[] settings, bool reboot, bool setPsk)
        {
            Settings = new Dictionary<string, string>()
            {
                {"port", settings[0]},
                {"current password", settings[1]},
                {"system password", settings[2]},
                {"secret", settings[3]},
                {"router ID", settings[4]},
                {"config directory", settings[5]},
                {"host ip address", settings[6]},
                {"psk ID", settings[7]},
                {"psk value", settings[8]}
            };

            FunctionUtil.InitializeProgress(settings[5] + @"\Logs\" + $"{settings[4]}_LOG.txt",
                reboot, setPsk);

            //a separate worker thread that takes care of the transferring of files.
            //this is done to allow responsive GUI updates.
            TransferWorker = new BackgroundWorker();
            TransferWorker.DoWork += TransferWorkerDoWork;
            TransferWorker.RunWorkerCompleted += TransferWorkerCompleted;
            TransferWorker.ProgressChanged += TransferWorkerProgressChanged;
            TransferWorker.WorkerReportsProgress = true;

             FunctionUtil.StartTftp();

            return InitializeSerialPort(settings[0]);
        }

        /// <summary>
        /// Formats the host file.
        /// Used in see <see cref="TransferWorkerDoWork"/>
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
                case "noacl.cfg":
                    return SerialConnection.GetSetting("router ID") + "_acl.cfg";
                case "xgsn.cfg":
                    return SerialConnection.GetSetting("router ID") + "_" + file;
                case "boot.cfg":
                    return SerialConnection.GetSetting("router ID") + ".cfg";
                default:
                    return "";
            }
        }        

    }

}