
using System;
using System.Threading;
using System.IO.Ports;

namespace BetterRouterProgram
{
    public class FunctionUtil
    {
        private static ProgressWindow ProgressWindow = null;
        private const int LoginProgress = 10;
        private const int PingProgress = 20;
        private const int CopyFilesProgress = 50;
        private const int CopySecondaryProgress = 70;
        private const int SetTimeProgress = 80;
        private const int PasswordProgress = 90;        
        private const int RebootProgress = 100;

        public static void InitializeProgressWindow(ProgressWindow pw) {
            ProgressWindow = pw;
        }

        private static void UpdateProgressWindow(string text, int value = -1) {
            if(value > -1) {
                ProgressWindow.progressBar.Value = value;
            }

            ProgressWindow.currentTask.Text = text;
        }

        public static void Login(string username = "root", string password = "") {
            UpdateProgressWindow("Logging In");

            //if the serial connection fails using the username and password specified
            if (!SerialConnection.Login(username, password)) {
                
                SerialConnection.CloseConnection();
                //TODO: EXIT
            }

            UpdateProgressWindow("Login Successful", LoginProgress);
        }

        public static void StopTFTP()
        {
            System.Diagnostics.Process.Start("TASKKILL / F / IM tftpd32.exe");
        }

        public static void SetPassword() {
            UpdateProgressWindow("Setting Password");



            UpdateProgressWindow("Password Set", PasswordProgress);
        }
         
        public static void SetTime() {
            UpdateProgressWindow("Setting Time");


            UpdateProgressWindow("Time Set", SetTimeProgress);
        }

        public static void PingTest() {
            UpdateProgressWindow("Pinging Host Machine");


            UpdateProgressWindow("Ping Successful", PingProgress);
        }

        public static void CopyFiles() {
            UpdateProgressWindow("Copying Configurations");



            UpdateProgressWindow("File Copying Successful", CopyFilesProgress);
        }

        public static void CopyToSecondary() {
            UpdateProgressWindow("Creating Back-Up Files");



            UpdateProgressWindow("Backup Created Successfully", CopySecondaryProgress);
        }

        public static void Reboot() {
            UpdateProgressWindow("Rebooting");



            UpdateProgressWindow("Reboot Successful", RebootProgress); 
        }
    }
}