using System.Collections.Generic;
using System;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace BetterRouterProgram
{
    //TODO: Routine to wipe router of information
    // Separate secret input
    // get rid of timezone
    // move config files to "done" directory to reduce wrong file selection
    // all files to be moved added to connection dictionary
    // autocheck all 5 main files
    // make reboot checkbox functional
    // log file for all output
    // auto disconnect, pause, and close of progress window
    // if acl and cfg are not both there, do not populate in list
    // programatically find and check staticrp and antiacl file boxes if present
    // programmatically set computer's adapter ip address

    /// <summary>
    /// A collection of static functions used to interact with the Serial Connection. 
    /// This class acts as a toolbox for each operation required to configure the router
    /// </summary>
    public class FunctionUtil
    {
        private static ProgressWindow ProgressWindow = null;
        private const string DateFormat = "yyyy/MM/dd HH:mm:ss";
        private static Process Tftp = null;

        /// <summary>
        /// A list of shorthand enumerables used to indicate how far along the progress of the program is.
        /// </summary>
        public enum Progress : int {
                None = 0,
                Login = 5, 
                Ping = 10,
                TransferFilesStart = 10, //goes to 60 - length: 50
                CopyToSecondary = 80, 
                Password = 95,
                Reboot = 100
        };

        /// <summary>
        /// Initializes and shows the progress window, used to show the user how far along the program is.
        /// also used to show any errors that may arise.
        /// </summary>
        public static void InitializeProgressWindow() {
            ProgressWindow = new ProgressWindow();
            ProgressWindow.progressBar.Value = (int)Progress.None;
            ProgressWindow.Topmost = true;
            ProgressWindow.Show();
        }

        /// <summary>
        /// Updates the progress window using the text provided and any changes in progress
        /// </summary>
        /// <param name="text">The message to be displayed in the textblock</param>
        /// <param name="setValue">The value to set the progress bar to</param>
        /// <param name="toAdd">The amount of progress to be added to the current progress level</param>
        public static void UpdateProgressWindow(string text, Progress setValue = Progress.None, double toAdd = 0) {
            if(setValue != Progress.None) {
                ProgressWindow.progressBar.Value = (int)setValue;   
                ProgressWindow.progressBar.Value += toAdd;
            }

            ProgressWindow.currentTask.Text += "\n" + text;
        }

        /// <summary>
        /// Pings the host machine (the user's computer) from the router to get a bearing on network capability and the connections
        /// </summary>
        /// <returns>Indicates whether or not the ping test successfully pinged the host</returns>
        public static bool PingTest() {
            UpdateProgressWindow("Pinging Host Machine");

            //TODO: uncomment after testing
            //SerialConnection.RunInstruction("setd !1 -ip neta = 10.1.1.1 255.255.255.0");
            
            string message = SerialConnection.RunInstruction($"ping {SerialConnection.GetSetting("host ip address")}");

            bool retVal = false;
            
            if (message.Contains("is alive")) {
                UpdateProgressWindow("Ping Successful", Progress.Ping);
                retVal = true;
            }
	        else {
		        if(message.Contains("Host unreachable")){
                    UpdateProgressWindow("**Error: Host IP Address Cannot be Reached", Progress.Ping);
                }
                else if(message.Contains("Request timed out")) {
                    UpdateProgressWindow("**Error: Connection Attempt has Timed Out", Progress.Ping);
                }
                else {
                    UpdateProgressWindow($"**Error: Ping testing failed with message: {message}", Progress.Ping);
                }
            }

            return retVal;
        }

        /// <summary>
        /// Copies the previously loaded file directory into a back-up directory in case of an error on the router
        /// </summary>
        public static void CopyToSecondary(List<string> filesToCopy) {
            UpdateProgressWindow("Creating Back-Up Directory");

            //TODO change test5 after testing
            string backupDirectory = "a:/test5/";
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
                    UpdateProgressWindow($"**Error: Backup of {file} in {backupDirectory} could not be made");
                }
                else
                {
                    UpdateProgressWindow($"Created backup of {file} in {backupDirectory}");
                }
            }

        }

        /// <summary>
        /// Sets the password of the router
        /// </summary>
        /// <param name="password">The password used at the corresponding site/zone of the system</param>
        public static void SetPassword(string password) {
            UpdateProgressWindow("Setting Password");

            password = password.Trim(' ', '\t', '\r', '\n');
            
            if(password.Equals(SerialConnection.GetSetting("initial password"))){
                UpdateProgressWindow("**Password cannot be set to the same value. Skipping Step...");
                return;
            }

            string message = SerialConnection.RunInstruction(String.Format(
                "SETDefault -SYS NMPassWord = \"{0}\" \"{1}\" \"{2}\"",
                SerialConnection.GetSetting("initial password"),
                password, password
            ));

            if (message.Contains("Password changed")) {
                UpdateProgressWindow("Password Succesfully Changed");
            }
            else if (message.Contains("Invalid password")) {
                
                UpdateProgressWindow("**Password used doesn't meet requirements. Skipping Step...");
                return;
            }
	        else {
                UpdateProgressWindow($"**Error: {message.Substring(0, 50)}...");
                return;
            }

            SerialConnection.RunInstruction($"setd -ac secret = \"{password}\"");
            UpdateProgressWindow("Secret Password Set", Progress.Password);
        }

        /// <summary>
        /// Prompts and enables the user to reboot the router
        /// </summary>
        public static void PromptReboot() 
        {
            ProgressWindow.RebootButton.IsEnabled = true;
            ProgressWindow.RebootText.Opacity = 1.0;
            UpdateProgressWindow("Please Reboot or Disconnect");
        }

        /// <summary>
        /// Sends the reboot command to the router
        /// </summary>
        public static void HandleReboot()
        {
            SerialConnection.RunInstruction("rb");

            UpdateProgressWindow("Reboot Command Sent", Progress.Reboot);
        }

        /// <summary>
        ///  Prompt and enable the user to disconnect from the current serial port (This does not reboot the router)
        /// </summary>
        public static void PromptDisconnect()
        {
            ProgressWindow.DisconnectButton.IsEnabled = true;
            ProgressWindow.DisconnectText.Opacity = 1.0;

            UpdateProgressWindow("Please Disconnect from the COM Port.");
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