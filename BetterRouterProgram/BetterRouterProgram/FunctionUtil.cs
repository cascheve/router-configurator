using System.Collections.Generic;
using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BetterRouterProgram
{
    //TODO Password IA in first menu

    /// <summary>
    /// A collection of static functions used to interact with the Serial Connection. 
    /// This class acts as a toolbox for each operation required to configure the router
    /// </summary>
    public class FunctionUtil
    {
        /// <summary>
        /// Writer used to write to the log file for the current router
        /// </summary>
        private static double UpdateAmount;

        /// <summary>
        /// Progress Window reference to update on command
        /// </summary>
        private static ProgressWindow ProgressWindow;

        /// <summary>
        /// tftpd64 application reference to open and close on command
        /// </summary>
        //private static Process Tftp;

        /// <summary>
        /// Writer used to write to the log file for the current router
        /// </summary>
        private static StreamWriter LogFileWriter;

        /// <summary>
        /// Enum to determine the type of message coming through <see cref="UpdateProgress"/>
        /// </summary>
        public enum MessageType : int {
            Message,
            Success,
            Error
        };

        /// <summary>
        /// Initializes and shows the progress window, used to show the user how far along the program is.
        /// also used to show any errors that may arise.
        /// </summary>
        public static void InitializeProgress(string logFilePath, bool setPsk, bool reboot) {
            double instructionCount = 5;
            if(setPsk) 
            {
                instructionCount += 1;
            }
            if(reboot)
            {
                instructionCount += 1;
            }

            UpdateAmount = 100 / instructionCount;

            ProgressWindow = new ProgressWindow();
            ProgressWindow.progressBar.Value = 0;
            ProgressWindow.Topmost = true;
            ProgressWindow.Show();

            //Using this method of log keeping deletes all previous logging on the file, which may be useful IF there are long term errors
            /*if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath); laptop difference

            }*/

            ProgressWindow.LogLocation.ToolTip += logFilePath;

            LogFileWriter = File.AppendText(logFilePath);

            LogFileWriter.WriteLine($"\n{DateTime.Now.ToString()}--------------------\n");
            LogFileWriter.Flush();
        }

        /// <summary>
        /// Updates the progress window using the text provided and any changes in progress
        /// </summary>
        /// <param name="text">The message to be displayed in the textblock</param>
        /// <param name="setValue">The value to set the progress bar to</param>
        /// <param name="toAdd">The amount of progress to be added to the current progress level</param>
        public static void UpdateProgress(string message, MessageType type) {

            string progressUpdate = "";
            switch(type) {
                case MessageType.Message:
                    progressUpdate = "[Message]    ";
                    break;
                case MessageType.Success:
                    progressUpdate = "[Success]    ";
                    ProgressWindow.progressBar.Value += UpdateAmount;                    
                    break;
                case MessageType.Error:
                    progressUpdate = "[Error]      ";
                    break;
                default:
                    break;
            }

            progressUpdate += message;

            ProgressWindow.currentTask.Text += '\n' + progressUpdate;

            LogFileWriter.WriteLine(progressUpdate + "\n");   
            LogFileWriter.Flush();
        }

        /// <summary>
        /// Logs in with the specified username and password.
        /// </summary>
        /// <param name="username">The router username.</param>
        /// <param name="password">The current router password.</param>
        /// <returns> whether the login was successful or not.</returns>
        public static bool Login(string username, string password)
        {
            UpdateProgress("Logging In", MessageType.Message);
            
            Thread.Sleep(500);

            try {
                SerialConnection.RunInstruction("", ':');
                SerialConnection.RunInstruction(username, ':');
                SerialConnection.RunInstruction(password);
                UpdateProgress("Login Successful", MessageType.Success);
                return true;
            }
            catch(TimeoutException) {
                return false;
            }  
        }

        /// <summary>
        /// Pings the host machine (the user's computer) from the router to get a bearing on network capability and the connections
        /// </summary>
        /// <returns>Indicates whether or not the ping test successfully pinged the host</returns>
        public static bool PingTest(string routerPort) {
            UpdateProgress("Pinging Host Machine", MessageType.Message);

            //ProgressWindow.Hide();

            string hostIP = SerialConnection.GetSetting("host ip address");
            string routerIP = "";
            if(hostIP.Split('.')[3].Equals("1")) {
                routerIP = hostIP.Substring(0, hostIP.LastIndexOf('.') + 1) + "2";
            }
            else {
                routerIP = hostIP.Substring(0, hostIP.LastIndexOf('.') + 1) + "100";
            }

            // setd !1 -ip neta = 10.1.1.1 255.255.255.0
            // copy 10.1.1.2:boot.ppc a:/primary/boot.ppc
            string message = SerialConnection.RunInstruction($"setd !{routerPort} -ip neta = {routerIP} 255.255.255.0");

            if (message.Contains("exists"))
            {
                UpdateProgress("Router IP Address has already been set", MessageType.Message);
            }

            message = SerialConnection.RunInstruction($"ping {SerialConnection.GetSetting("host ip address")}");

            bool retVal = false;
            
            if (message.Contains("is alive")) {
                UpdateProgress("Ping Successful", MessageType.Success);
                retVal = true;
            }
	        else {
		        if(message.Contains("unreachable")){
                    UpdateProgress("Host IP Address Cannot be Reached", MessageType.Error);
                }
                else if(message.Contains("Request timed out")) {
                    UpdateProgress("Connection Attempt has Timed Out", MessageType.Error);
                }
                else {
                    UpdateProgress("Ping test failed", MessageType.Error);
                }
            }

            return retVal;
        }

        /// <summary>
        /// Copies the previously loaded file directory into a back-up directory in case of an error on the router
        /// </summary>
        public static void CopyToSecondary(List<string> filesToCopy) {

            //UpdateProgress("Copying files to Secondary Directory", MessageType.Message);

            //TODO switch between test and official
            string primaryDirectory = "a:/primary";
            string backupDirectory = "a:/secondary";
            string response = "";

            //SerialConnection.RunInstruction("cd a:/");
            SerialConnection.RunInstruction($"md {backupDirectory}");
            SerialConnection.RunInstruction($"cd {backupDirectory}");

            foreach (var file in filesToCopy)
            {
                //the timeout is adjusted for the much bigger file
                if (file.Equals("boot.ppc"))
                {
                    SerialConnection.SetSerialPortTimeout(30000);
                }
                else
                {
                    SerialConnection.SetSerialPortTimeout(17500);
                }

                UpdateProgress($"Creating backup of {file}", MessageType.Message);

                response = SerialConnection.RunInstruction($"copy {primaryDirectory}/{file} {backupDirectory}/{file}");

                if (response.Contains("not Found"))
                {
                    UpdateProgress($"Backup of {file} in {backupDirectory} could not be made", MessageType.Error);
                }
                else
                {
                    UpdateProgress($"Created backup of {file} in {backupDirectory}", MessageType.Success);
                }

            }

        }

        /// <summary>
        /// Sets the password of the router
        /// </summary>
        /// <param name="password">The password used at the corresponding site/zone of the system</param>
        public static void SetPassword(string password) {
            UpdateProgress("Setting Passwords", MessageType.Message);

            password = password.Trim(' ', '\t', '\r', '\n');

            if (password.Equals(string.Empty))
            {
                UpdateProgress("New Password was left empty. Skipping Step...", MessageType.Message);
            }
            else if (password.Equals(SerialConnection.GetSetting("current password")))
            {
                UpdateProgress("New Password cannot be set to the same value. Skipping Step...", MessageType.Message);
            }           
            else
            {
                string message = SerialConnection.RunInstruction(String.Format(
                    "SETDefault -SYS NMPassWord = \"{0}\" \"{1}\" \"{2}\"",
                    SerialConnection.GetSetting("current password"),
                    password, password
                ));

                if (message.Contains("Password changed"))
                {
                    UpdateProgress("Password Succesfully Changed", MessageType.Success);
                }
                else if (message.Contains("Invalid password"))
                {
                    //TODO add IA compliance to the first menu to eliminate this error
                    UpdateProgress("Password used doesn't meet requirements. Skipping Step...", MessageType.Error);
                    return;
                }
                else
                {
                    UpdateProgress($"{message.Substring(0, 50)}...", MessageType.Error);
                    return;
                }
            }

            if (SerialConnection.GetSetting("secret").Equals(string.Empty))
            {
                UpdateProgress("Secret was left empty. Skipping Step...", MessageType.Message);
            }
            else
            {
                SerialConnection.RunInstruction($"setd -ac secret = \"{SerialConnection.GetSetting("secret")}\"");
                UpdateProgress("Secret Password Set", MessageType.Success);
            }
        }

        /// <summary>
        /// Sets the psk addresses of the router
        /// </summary>
        /// <param name="ipList">list of the ips to set the psk for</param>
        public static void SetPsk(List<string> ipList) {
            UpdateProgress($"Setting PSKs for {SerialConnection.GetSetting("psk ID")}", MessageType.Message);

           foreach(var ip in ipList) 
           {
                SerialConnection.RunInstruction($"ADD -CRYPTO FipsPreShrdKey {ip.Trim()} \"{SerialConnection.GetSetting("psk value")}\" \"{SerialConnection.GetSetting("psk value")}\"");
           }

            UpdateProgress("PSKs Set", MessageType.Success);
        }

        /// <summary>
        /// Moves the files from the config directory to the /Completed directory
        /// </summary>
        private static void MoveCompletedFiles()
        {
            string routerID = SerialConnection.GetSetting("router ID");
            string configDir = SerialConnection.GetSetting("config directory");

            foreach (var file in Directory.GetFiles(configDir, "*.cfg").Select(Path.GetFileName))
            {
                if(file.StartsWith(routerID))
                {
                    File.Move(configDir + @"\" + file, configDir + @"\Completed\" + file);
                }
            }

            MainWindow.UpdateIDs();
        }

        public static void SetBootOrder()
        {

            SerialConnection.RunInstruction("sf 7", ')');

            SerialConnection.RunInstruction("2", ')');

            SerialConnection.RunInstruction("q");

            UpdateProgress("Boot Order Set", FunctionUtil.MessageType.Success);

        }

        /// <summary>
        /// Prompts and enables the user to reboot the router
        /// </summary>
        public static void ConfigurationFinished(bool reboot, bool error) 
        {
            if (!error)
            {
                if (reboot)
                {
                    UpdateProgress("Reboot command sent", MessageType.Success);
                    SerialConnection.RunInstruction("rb");
                }
                else
                {
                    UpdateProgress("Reboot command not sent", MessageType.Message);
                }

                UpdateProgress("All Processes Complete", MessageType.Message);
                MoveCompletedFiles();
            }
            else
            {
                UpdateProgress("An Error occurred during runtime. The router will not be rebooted.", MessageType.Message);
            }

            SerialConnection.CloseConnection();

            //give the user time to read the update messages
            Thread.Sleep(500);

            //close the progress window and filewriter
            ProgressWindow.Close();
            LogFileWriter.Close();
        }

        ///// <summary>
        ///// Called when the reference to the TFTP process is closed, which is required for file transfer
        ///// </summary>
        ///// <param name="sender">The sender of the close event</param>
        ///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //private static void OnTftpExit(object sender, EventArgs e)
        //{
        //    System.Windows.Forms.MessageBox.Show("The TFTP Application was closed. This may cause errors in File Transfer.");
        //    Tftp = null;

        //    //if the directory was renamed, set the name right again
        //    if (Directory.Exists(@"C:\Motorola\SDM3000\Common\temp_TFTP"))
        //    {
        //        Process.Start("CMD.exe", @"/C cd C:\Motorola\SDM3000\Common\ & rename temp_TFTP TFTP");
        //        Thread.Sleep(250);
        //    }
        //}

        /// <summary>
        /// Starts the TFTP application from the configuration folder
        /// </summary>
        public static void StartTftp()
        {
            //this folder was found to cause errors when attempting to use TFTP in the program's context. Renaming clears the issue.
            //if (Directory.Exists(@"C:\Motorola\SDM3000\Common\TFTP"))
            //{
            //    Process.Start("CMD.exe", @"/C cd C:\Motorola\SDM3000\Common\ & rename TFTP temp_TFTP");
            //    Thread.Sleep(250);
            //}

            TFTPServer.RunServer(SerialConnection.GetSetting("config directory"));

            ////if the reference to TFTP is null (There is no relevant instance open) create a new one
            //if (Tftp == null)
            //{
            //    Tftp = new Process();
            //    Tftp.EnableRaisingEvents = true;
            //    Tftp.Exited += OnTftpExit;
            //    Tftp.StartInfo.Arguments = @"C:\";
            //    Tftp.StartInfo.FileName = SerialConnection.GetSetting("config directory") + @"\tftpd64.exe";
            //    Tftp.StartInfo.WorkingDirectory = SerialConnection.GetSetting("config directory");
            //    Tftp.Start();
            //}

            ////if the directory was renamed, set the name right again
            //if (Directory.Exists(@"C:\Motorola\SDM3000\Common\temp_TFTP"))
            //{
            //    Process.Start("CMD.exe", @"/C cd C:\Motorola\SDM3000\Common\ & rename temp_TFTP TFTP");
            //    Thread.Sleep(250);
            //}

        }

        ///// <summary>
        ///// Stops the TFTP application. This is done as a convenience for the User.
        ///// </summary>
        //public static void StopTftp()
        //{

        //    if (Tftp != null)
        //    {
        //        Tftp.CloseMainWindow();
        //        Tftp.Close();
        //        Tftp = null;
        //    }
        //}
    }
}