
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
        public static List <string> FilesToTransfer = null;
        BackgroundWorker bw = new BackgroundWorker();

        //end progress of all parts of configuration
        public enum Progress : int {
                None = 0,
                Login = 5, 
                Ping = 10,
                TransferFilesStart = 10, //goes to 60 - length: 50
                CopyToSecondary = 80, 
                SetTime = 85, 
                Password = 95,
                Reboot = 100,
        };

        public static void InitializeProgressWindow(ref ProgressWindow pw) {
            ProgressWindow = pw;
            ProgressWindow.progressBar.Value = (int)Progress.None;
        }

        public static void UpdateProgressWindow(string text, Progress setValue = Progress.None, double toAdd = 0) {
            if(setValue != Progress.None) {
                ProgressWindow.progressBar.Value = (int)setValue;   
                ProgressWindow.progressBar.Value += toAdd;
            }

            ProgressWindow.currentTask.Text += "\n" + text;
        }

        public static bool Login(string username = "root", string password = "") {
            UpdateProgressWindow("Logging In");

            //if the serial connection fails using the username and password specified
            if (!SerialConnection.Login(username, password)) {

                UpdateProgressWindow("**Login Unsuccessful**", Progress.None);

                SerialConnection.CloseConnection();
                return false;
            }
            
            UpdateProgressWindow("Login Successful", Progress.Login);
            return true;
        }

        public static void SetPassword(string password) {
            UpdateProgressWindow("Setting Password");

            password = password.Trim(' ', '\t', '\r', '\n');
            
            if(password.Equals(SerialConnection.GetSetting("initial password"))){
                UpdateProgressWindow("**Password cannot be set to the same value. Skipping Step**");
                return;
            }

            string message = SerialConnection.RunInstruction(
                $"SETDefault -SYS NMPassWord = \"{SerialConnection.GetSetting("initial password")}\"" + 
                $" \"{password}\" \"{password}\""
            );

            if (message.Contains("Password changed")) {
                UpdateProgressWindow("Password Succesfully Changed");
            }
            else if (message.Contains("Invalid password")) {
                
                UpdateProgressWindow("**Password used doesn't meet requirements, skipping step**");
                return;
            }
	        else {
                UpdateProgressWindow($"**{message}**");
                return;
            }

            SerialConnection.RunInstruction($"setd -ac secret = \"{password}\"");

            UpdateProgressWindow("Secret Password Set", Progress.Password);
        }
         
        //takes a signed offset which is the number of hours forward or backward the clock must be from CST
        public static void SetTime(string timeZoneString = "") {

            int offset = 0;

            if (timeZoneString != "")
            {
                offset = Int32.Parse(timeZoneString.Substring(4, 3));
            }

            DateTime setDate = DateTime.UtcNow.AddHours(offset);

            if (!setDate.IsDaylightSavingTime())
            {
                setDate = DateTime.UtcNow.AddHours(offset + 1);
            }

            //SET - SYS DATE = 2017 / 05 / 19 12:02
            //outputs as mm/dd/yyyy hh:mm:ss XM

            UpdateProgressWindow("Setting Time: " + setDate.ToString(DateFormat), Progress.SetTime);

            SerialConnection.RunInstruction("SET - SYS DATE = " + setDate.ToString(DateFormat));

            //Thread.Sleep(2000);
        }

        public static void PingTest() {
            UpdateProgressWindow("Pinging Host Machine");
            
            string message = SerialConnection.RunInstruction("ping " + "10.2.251.100");
            
            if (message.Contains("is alive")) {
                UpdateProgressWindow("Ping Successful");
            }
	        else {
		        if(message.Contains("Host unreachable")){
                    UpdateProgressWindow("**Host IP Address Cannot be Reached**");
                }
                else if(message.Contains("Request timed out")) {
                    UpdateProgressWindow("**Connection Attempt has Timed Out**");
                }
            }

            UpdateProgressWindow("Ping Test Completed", Progress.Ping);
        }

        public static string FormatHostFile(string file) {

            string filename = "";
            file = file.Trim();

            if(file.Equals("staticRP.cfg") || file.Equals("antiacl.cfg") || file.Equals("boot.ppc")) {
                filename = file;
            }
            else if(file.Equals("acl.cfg") || file.Equals("xgsn.cfg")) {
                filename = SerialConnection.GetSetting("router ID") + "_" + file;
            }
            else if(file.Equals("boot.cfg")) {
                filename = SerialConnection.GetSetting("router ID") + ".cfg";
            }

            return filename;
        }

        public static void TransferFiles(params string[] files) {
            double totalProgress = 50;

            UpdateProgressWindow("Transferring Configuration Files");
            
            SerialConnection.RunInstruction(@"cd a:\test\test1");

            int i = 0;
            string hostFile = "";

            foreach (var file in FilesToTransfer)
            {

                hostFile = FormatHostFile(file);

                UpdateProgressWindow($"Transferring File: {hostFile} -> {file}");

                //attempt to copy the files from the host to the machine
                string message = SerialConnection.RunInstruction(String.Format("copy {0}:{1} {2}",
                    SerialConnection.GetSetting("host ip address"),
                    hostFile, file
                ));

                if (message.Contains("File not found"))
                {
                    UpdateProgressWindow($"Error: {hostFile} not found in host configuration directory");
                }
                else if (message.Contains("Cannot route"))
                {
                    UpdateProgressWindow("Cannot connect to the Router via TFTP. \nCheck your ethernet connection.");
                }
                else
                {

                    //update the progress window according to the file's transfer status
                    UpdateProgressWindow(
                        $"{hostFile} Successfully Transferred",
                        Progress.TransferFilesStart,
                        (((double)totalProgress) / FilesToTransfer.Count) * (++i)
                    );
                }
            }
        }

        public static void CopyToSecondary() {
            UpdateProgressWindow("Creating Back-Up Files");

            SerialConnection.RunInstruction("cd a:/");

            SerialConnection.RunInstruction("md /test2");

            SerialConnection.RunInstruction("copy a:/primary/*.* a:/test2");

            UpdateProgressWindow("Copies Created Succesfully");

            UpdateProgressWindow("Backup Created Successfully", Progress.CopyToSecondary);
        }

        public static void PromptDisconnect()
        {

            ProgressWindow.DisconnectButton.IsEnabled = true;
            ProgressWindow.DisconnectText.Opacity = 1.0;

            UpdateProgressWindow("All operations have been run. Please Disconnect.");
        }

        public static void PromptReboot() {

            ProgressWindow.RebootButton.IsEnabled = true;
            ProgressWindow.RebootText.Opacity = 1.0;
            UpdateProgressWindow("Please Reboot Router", Progress.Reboot - 5);
        }

        public static void HandleReboot()
        {
            UpdateProgressWindow("Rebooting", Progress.Reboot - 5);
            
            SerialConnection.RunInstruction("rb");

            UpdateProgressWindow("Reboot Successful", Progress.Reboot);

        }

        public static void StartTftp()
        {
            //Directory.Move(@"C:\Motorola\SDM3000\Common\TFTP", @"C:\Motorola\SDM3000\Common\temp_TFTP");
            string strCmdText = @"/C cd C:\Motorola\SDM3000\Common\ & rename TFTP temp_TFTP";
            Process.Start("CMD.exe", strCmdText);
            Thread.Sleep(200);

            Tftp = new Process();
            Tftp.EnableRaisingEvents = true;
            Tftp.Exited += Tftp_Exited;
            Tftp.StartInfo.Arguments = @"C:\";
            Tftp.StartInfo.FileName = SerialConnection.GetSetting("config directory") + @"\tftpd32.exe";
            Tftp.StartInfo.WorkingDirectory = SerialConnection.GetSetting("config directory");

            Tftp.Start();
            
        }

        private static void Tftp_Exited(object sender, EventArgs e)
        {
            UpdateProgressWindow("TFTP was closed. This can errors with file transferring.");
        }

        public static void StopTftp()
        {
            if (Tftp != null)
            {
                Tftp.CloseMainWindow();
                Tftp.Close();
                Tftp = null;

                //Directory.Move(@"C:\Motorola\SDM3000\Common\temp_TFTP", @"C:\Motorola\SDM3000\Common\TFTP");
            }

            string strCmdText = @"/C cd C:\Motorola\SDM3000\Common\ & rename temp_TFTP TFTP";
            Process.Start("CMD.exe", strCmdText);
            Thread.Sleep(200);
        }


        public static void SetFilesToTransfer(Dictionary<string, bool> filesToTransfer)
        {
            FilesToTransfer = new List<string>(6);
            FilesToTransfer.Add("boot.ppc");
            FilesToTransfer.Add("boot.cfg");
            FilesToTransfer.Add("acl.cfg");

            foreach (var file in filesToTransfer.Keys)
            {
                if(filesToTransfer[file] == true)
                {
                    FilesToTransfer.Add(file);
                }
            }
        }
    }
}