
using System;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Collections.Generic;

namespace BetterRouterProgram
{

    public class FunctionUtil
    {
        private static ProgressWindow ProgressWindow = null;
        private const string DateFormat = "yyyy/MM/dd HH:mm:ss";
        private static Process Tftp = null;
        private static ArrayList<string> FilesToTransfer = null;

        private enum Progress : int {
                None = 0,
                Login = 10, 
                Ping = 20,
                TransferFilesStart = 20,
                CopyToSecondary = 70, 
                SetTime = 80, 
                Password = 90,
                Reboot = 100,
        };

        public static void InitializeProgressWindow(ref ProgressWindow pw) {
            ProgressWindow = pw;
            ProgressWindow.progressBar.Value = (int)Progress.None;
        }

        private static void UpdateProgressWindow(string text, Progress value = Progress.None, double toAdd = 0) {
            if(value != Progress.None) {
                ProgressWindow.progressBar.Value = (int)value;
                ProgressWindow.progressBar.Value += toAdd;
            }

            ProgressWindow.currentTask.Text += "\n" + text;
        }

        public static void Login(string username = "root", string password = "") {
            UpdateProgressWindow("Logging In");

            //if the serial connection fails using the username and password specified
            if (!SerialConnection.Login(username, password)) {
                
                SerialConnection.CloseConnection();
                return;
            }
            
            UpdateProgressWindow("Login Successful", Progress.Login);
        }

        public static void SetPassword(string password) {
            UpdateProgressWindow("Setting Password");

            password = password.Trim(' ', '\t', '\r', '\n');
            
            if(password.Equals(SerialConnection.GetSetting("initial password"))){
                UpdateProgressWindow("Password cannot be set to the same value. Skipping Step");
                return;
            }

            //TODO: Change Literal {0}
            string message = SerialConnection.RunInstruction(String.Format(
                "SETDefault -SYS NMPassWord = \"P25CityX2016!\" \"{1}\" \"{2}\"", 
                SerialConnection.GetSetting("initial password"),
                password, 
                password
            ));

            if (message.Contains("Password changed")) {
                UpdateProgressWindow("Password Succesfully Changed");
            }
            else if (message.Contains("Invalid password")) {
                
                UpdateProgressWindow("Password used doesn't meet requirements, skipping step");
                return;
            }
	        else {
                UpdateProgressWindow(message);
                return;
            }

            SerialConnection.RunInstruction(String.Format("setd -ac secret = \"{0}\"", password));

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
                UpdateProgressWindow("Ping Succesful");
            }
	        else {
		        if(message.Contains("Host unreachable")){
                    UpdateProgressWindow("**IP Address Cannot be Reached**");
                }
                else if(message.Contains("Request timed out")) {
                    UpdateProgressWindow("**Connection Timed Out**");
                }

                //TODO: do something like this but with WPF stuff
                // print('ping failed to host: {}'.format(settings['ip_addr']))
                // exit = input('would you like to exit? (y/n): ')
                // if exit == 'y':
                //  close_connection()                  
                // stop_tftpd()
                // sys.exit()
            }

            UpdateProgressWindow("Ping Test Completed", Progress.Ping);
        }

        private static FormatHostFile(string file) {
            string filename = "";
            if(file.Equals("staticRP.cfg") || file.Equals("anitacl.cfg") || file.Equals("boot.ppc")) {
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
            double total = 50;

            UpdateProgressWindow("Transfering Configurations");

            int progress = (int)Progress.TransferFilesStart;
            
            SerialConnection.RunInstruction("cd");

            int i = 0;
            string copyFileInstruction = "";
            string hostFile = "";
            foreach (var file in FilesToTransfer)
            {
                //print('copying {file})
                hostFileFormatHostFile(file)
                UpdateProgressWindow(String.Format("Transferring File: {0} -> {1}", hostFile, file));
                /*SerialConnection.RunInstruction(String.Format("copy {0}:{1}\\{2} {3}", 
                    SerialConnection.GetSetting("host ip address"), 
                    SerialConnection.GetSetting("config directory"),
                    hostFile, file
                ));*/
                UpdateProgressWindow(
                    String.Format("File: {0} Transferred", hostFile), 
                    Progress.TransferFilesStart, 
                    (((double) 50)/FilesToTransfer.Count)*(i++)
                );
            }

            //        *******update progress bar for each file done
            //        instr = 'copy {}{} {}\r\n'.format(settings['ip_addr'] + ':', hostFile, file) 
            //        *******use setting config directory to prepend to hostFile
            //        router_connection.write(str_to_byte(instr))
            
            //        print('copying file: {} -> {}   [ done ]     '.format(hostFile, file))
        }

        public static void CopyToSecondary() {
            UpdateProgressWindow("Creating Back-Up Files");

            SerialConnection.RunInstruction("cd a:/");
            SerialConnection.RunInstruction("md /secondary");
            SerialConnection.RunInstruction("copy a:/primary/*.* a:/secondary");

            UpdateProgressWindow("Copies Created Succesfully");

            UpdateProgressWindow("Backup Created Successfully", Progress.CopyToSecondary);
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
            Thread.Sleep(500);

            UpdateProgressWindow("Reboot Successful", Progress.Reboot);

        }

        public static void StartTftp()
        {
            Tftp = Process.Start(SerialConnection.GetSetting("config directory") + "\\tftpd32.exe");
        }

        public static void StopTftp()
        {
            if (Tftp != null)
            {
                Tftp.CloseMainWindow();
                Tftp.Close();
            }
        }


        public static void SetFilesToTransfer(Dictionary<string, bool> filesToTransfer)
        {
            FilesToTransfer = new ArrayList<string>(6);
            FilesToTransfer.Add("boot.ppc");
            FilesToTransfer.Add("boot.cfg");
            FilesToTransfer.Add("acl.cfg")

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