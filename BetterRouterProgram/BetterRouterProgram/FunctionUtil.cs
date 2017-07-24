using System.Collections.Generic;
using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Linq;

// TODO: find a place to runs set psk commands
namespace BetterRouterProgram
{

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
        /// tftpd32 application reference to open and close on command
        /// </summary>
        private static Process Tftp;

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

            UpdateAmount = 100/instructionCount;

            ProgressWindow = new ProgressWindow();
            ProgressWindow.progressBar.Value = 0;
            ProgressWindow.Topmost = true;
            ProgressWindow.Show();

            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            LogFileWriter = File.AppendText(logFilePath);
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
                    progressUpdate = "[Message]  ";
                    break;
                case MessageType.Success:
                    progressUpdate = "[Success]  ";

                    break;
                case MessageType.Error:
                    progressUpdate = "[Error]    ";
                    break;
                default:
                    break;
            }

            progressUpdate += message;

            ProgressWindow.currentTask.Text += '\n' + progressUpdate;
            ProgressWindow.progressBar.Value += UpdateAmount;

            LogFileWriter.WriteLine(progressUpdate + "\n");   
            LogFileWriter.Flush();
        }

        /// <summary>
        /// Pings the host machine (the user's computer) from the router to get a bearing on network capability and the connections
        /// </summary>
        /// <returns>Indicates whether or not the ping test successfully pinged the host</returns>
        public static bool PingTest() {
            UpdateProgress("Pinging Host Machine", MessageType.Message);

            string hostIP = SerialConnection.GetSetting("host ip address");
            string routerIP = "";
            if(hostIP.Split('.')[3].Equals("1")) {
                routerIP = hostIP.Substring(0, hostIP.LastIndexOf('.')+1) + "2";
            }
            else {
                routerIP = hostIP.Substring(0, hostIP.LastIndexOf('.')+1) + "1";
            }
            
            //TODO: uncomment after testing
            //SerialConnection.RunInstruction($"setd !{routerport#} -ip neta = {routerIP} 255.255.255.0");
            
            string message = SerialConnection.RunInstruction($"ping {SerialConnection.GetSetting("host ip address")}");

            bool retVal = false;
            
            if (message.Contains("is alive")) {
                UpdateProgress("Ping Successful", MessageType.Success);
                retVal = true;
            }
	        else {
		        if(message.Contains("Host unreachable")){
                    UpdateProgress("Host IP Address Cannot be Reached", MessageType.Error);
                }
                else if(message.Contains("Request timed out")) {
                    UpdateProgress("Connection Attempt has Timed Out", MessageType.Error);
                }
                else {
                    UpdateProgress($"Ping testing failed with message: {message}", MessageType.Error);
                }
            }

            return retVal;
        }

        /// <summary>
        /// Copies the previously loaded file directory into a back-up directory in case of an error on the router
        /// </summary>
        public static void CopyToSecondary(List<string> filesToCopy) {
            UpdateProgress("Creating Back-Up Directory", MessageType.Message);

            //TODO change test5 after testing
            string backupDirectory = "a:/test5";
            string response = "";

            //SerialConnection.RunInstruction("cd a:/");
            SerialConnection.RunInstruction($"md {backupDirectory}");
            SerialConnection.RunInstruction($"cd {backupDirectory}");

            foreach (var file in filesToCopy)
            {
                //TODO change test3 after testing
                response = SerialConnection.RunInstruction($"copy a:/test3/{file} {backupDirectory}/{file}");

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
            UpdateProgress("Setting Password", MessageType.Message);

            password = password.Trim(' ', '\t', '\r', '\n');
            
            if(password.Equals(SerialConnection.GetSetting("current password"))){
                UpdateProgress("Password cannot be set to the same value. Skipping Step...", MessageType.Error);
                return;
            }

            string message = SerialConnection.RunInstruction(String.Format(
                "SETDefault -SYS NMPassWord = \"{0}\" \"{1}\" \"{2}\"",
                SerialConnection.GetSetting("current password"),
                password, password
            ));

            if (message.Contains("Password changed")) {
                UpdateProgress("Password Succesfully Changed", MessageType.Success);
            }
            else if (message.Contains("Invalid password")) {
                
                UpdateProgress("Password used doesn't meet requirements. Skipping Step...", MessageType.Error);
                return;
            }
	        else {
                UpdateProgress($"{message.Substring(0, 50)}...", MessageType.Error);
                return;
            }

            SerialConnection.RunInstruction($"setd -ac secret = \"{SerialConnection.GetSetting("secret")}\"");
            UpdateProgress("Secret Password Set", MessageType.Success, Progress);
        }

        /// <summary>
        /// Sets the psk addresses of the router
        /// </summary>
        /// <param name="ipList">list of the ips to set the psk for</param>
        public static void SetPsk(List<string> ipList) {
            UpdateProgress($"Setting PSKs for {/*TODO insert setting here*/}", MessageType.Message);

           foreach(var ip in ipList) 
           {
                SerialConnection.RunInstruction($"ADD -CRYPTO FipsPreShrdKey {ip.Trim()} \"{/*psk*/}\" \"{/*psk*/}\"");
           }

            UpdateProgress("PSKs Set", MessageType.Success, Progress);
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
                Thread.Sleep(1000);
            }

            SerialConnection.CloseConnection();

            //close the progress window and filewriter
            ProgressWindow.Close();
            LogFileWriter.Close();
        }

        /// <summary>
        /// Called when the reference to the TFTP process is closed, which is required for file transfer
        /// </summary>
        /// <param name="sender">The sender of the close event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private static void OnTftpExit(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("The TFTP Application was closed. This may cause errors in File Transfer.");
            Tftp = null;

            //if the directory was renamed, set the name right again
            if (Directory.Exists(@"C:\Motorola\SDM3000\Common\temp_TFTP"))
            {
                Process.Start("CMD.exe", @"/C cd C:\Motorola\SDM3000\Common\ & rename temp_TFTP TFTP");
                Thread.Sleep(250);
            }
        }

        /// <summary>
        /// Starts the TFTP application from the configuration folder
        /// </summary>
        public static void StartTftp()
        {
            //this folder was found to cause errors when attempting to use TFTP in the program's context. Renaming clears the issue.
            if (Directory.Exists(@"C:\Motorola\SDM3000\Common\TFTP"))
            {
                Process.Start("CMD.exe", @"/C cd C:\Motorola\SDM3000\Common\ & rename TFTP temp_TFTP");
                Thread.Sleep(250);
            }

            //if the reference to TFTP is null (There is no relevant instance open) create a new one
            if (Tftp == null)
            {
                Tftp = new Process();
                Tftp.EnableRaisingEvents = true;
                Tftp.Exited += OnTftpExit;
                Tftp.StartInfo.Arguments = @"C:\";
                Tftp.StartInfo.FileName = SerialConnection.GetSetting("config directory") + @"\tftpd32.exe";
                Tftp.StartInfo.WorkingDirectory = SerialConnection.GetSetting("config directory");
                Tftp.Start();
            }

            //if the directory was renamed, set the name right again
            if (Directory.Exists(@"C:\Motorola\SDM3000\Common\temp_TFTP"))
            {
                Process.Start("CMD.exe", @"/C cd C:\Motorola\SDM3000\Common\ & rename temp_TFTP TFTP");
                Thread.Sleep(250);
            }

        }

        /// <summary>
        /// Stops the TFTP application. This is done as a convenience for the User.
        /// </summary>
        public static void StopTftp()
        {

            if (Tftp != null)
            {
                Tftp.CloseMainWindow();
                Tftp.Close();
                Tftp = null;
            }
        }
    }
}