
using System;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Windows.Media;
using System.IO;
using System.ComponentModel;

namespace BetterRouterProgram
{
    public class FunctionUtil
    {
        private static ProgressWindow ProgressWindow = null;
        private const string DateFormat = "yyyy/MM/dd HH:mm:ss";
        private static Process Tftp = null;

        public enum Progress : int {
                None = 0,
                Login = 5, 
                Ping = 10,
                TransferFilesStart = 10, //goes to 60 - length: 50
                CopyToSecondary = 80, 
                SetTime = 85, 
                Password = 95,
                Reboot = 100
        };

        public static void InitializeProgressWindow() {
            ProgressWindow = new ProgressWindow();
            ProgressWindow.progressBar.Value = (int)Progress.None;
            ProgressWindow.Topmost = false;
            ProgressWindow.Show();
        }

        public static void UpdateProgressWindow(string text, Progress setValue = Progress.None, double toAdd = 0) {
            if(setValue != Progress.None) {
                ProgressWindow.progressBar.Value = (int)setValue;   
                ProgressWindow.progressBar.Value += toAdd;
            }

            ProgressWindow.currentTask.Text += "\n" + text;
        }

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
                    UpdateProgressWindow("**Host IP Address Cannot be Reached**", Progress.Ping);
                }
                else if(message.Contains("Request timed out")) {
                    UpdateProgressWindow("**Connection Attempt has Timed Out**", Progress.Ping);
                }
                else {
                    UpdateProgressWindow($"Ping testing failed with message: {message}", Progress.Ping);
                }
            }

            return retVal;
        }

         //TODO: change paths for testing
        public static void CopyToSecondary() {
            UpdateProgressWindow("Creating Back-Up Directory");

            SerialConnection.RunInstruction("cd a:/");
            SerialConnection.RunInstruction("md /test4");
            SerialConnection.RunInstruction("copy a:/test3/*.* a:/test4");

            UpdateProgressWindow("Backup Created Successfully", Progress.CopyToSecondary);
        }

        public static void SetPassword(string password) {
            UpdateProgressWindow("Setting Password");

            password = password.Trim(' ', '\t', '\r', '\n');
            
            if(password.Equals(SerialConnection.GetSetting("initial password"))){
                UpdateProgressWindow("**Password cannot be set to the same value. Skipping Step**");
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
                
                UpdateProgressWindow("**Password used doesn't meet requirements, skipping step**");
                return;
            }
	        else {
                UpdateProgressWindow($"**{message.Substring(0, 50)}...**");
                return;
            }

            SerialConnection.RunInstruction($"setd -ac secret = \"{password}\"");
            UpdateProgressWindow("Secret Password Set", Progress.Password);
        }
         
        //takes a signed offset which is the number of hours forward or backward the clock must be from CST
        public static void SetTime(string timeZoneString = "") 
        {
            int offset = 0;

            if (!timeZoneString.Equals(string.Empty))
            {
                offset = Int32.Parse(timeZoneString.Substring(4, 3));
            }

            DateTime setDate = DateTime.UtcNow.AddHours(offset);
            if (!setDate.IsDaylightSavingTime())
            {
                setDate = DateTime.UtcNow.AddHours(offset + 1);
            }

            //outputs as mm/dd/yyyy hh:mm:ss XM
            UpdateProgressWindow("Setting Time: " + setDate.ToString(DateFormat), Progress.SetTime);

            SerialConnection.RunInstruction($"SET - SYS DATE = {setDate.ToString(DateFormat)}");
        }

        public static void PromptReboot() 
        {
            ProgressWindow.RebootButton.IsEnabled = true;
            ProgressWindow.RebootText.Opacity = 1.0;
            UpdateProgressWindow("Please Reboot or Disconnect");
        }

        public static void HandleReboot()
        {
            SerialConnection.RunInstruction("rb");

            UpdateProgressWindow("Reboot Command Sent Successfully", Progress.Reboot);
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

        private static void OnTftpExit(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("The TFTP Application was closed. This may cause errors in File Transfer.");
            Tftp = null;
        }

        public static void StartTftp()
        {
            if (Directory.Exists(@"C:\Motorola\SDM3000\Common\TFTP"))
            {
                Process.Start("CMD.exe", @"/C cd C:\Motorola\SDM3000\Common\ & rename TFTP temp_TFTP");
                Thread.Sleep(250);
            }

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
            
        }

        public static void StopTftp()
        {
            if (Directory.Exists(@"C:\Motorola\SDM3000\Common\temp_TFTP"))
            {
                Process.Start("CMD.exe", @"/C cd C:\Motorola\SDM3000\Common\ & rename temp_TFTP TFTP");
                Thread.Sleep(250);
            }

            if (Tftp != null)
            {
                Tftp.CloseMainWindow();
                Tftp.Close();
                Tftp = null;
            }
        }
    }
}