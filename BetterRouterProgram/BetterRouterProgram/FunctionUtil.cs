using System;
using System.Threading;
using System.IO.Ports;

namespace BetterRouterProgram
{
    public class FunctionUtil
    {
        private static ProgressWindow ProgressWindow = null;

        enum Progress : int {
            None=-1,
            Login=10, 
            Ping=20,
            CopyFiles=50,
            CopySecondary=70, 
            SetTime=80, 
            Password=90,
            Reboot=100
            };  

        public static void InitializeProgressWindow(ProgressWindow pw) {
            ProgressWindow = pw;
        }

        private static void UpdateProgressWindow(string text, int value = Progress.None) {
            if(value > Progress.None) {
                ProgressWindow.progressBar.Value = value;
            }
            ProgressWindow.currentTask.Text = text;
        }

        //might move to different module
        public static void Login(string username = "root", string password = "") {
            UpdateProgressWindow("Logging In");

            SerialConnection.Login(username, password);

            UpdateProgressWindow("Login Successful", Progress.Login);
        }

        public static void SetPassword() {
            UpdateProgressWindow("Setting Password");



            UpdateProgressWindow("Password Set", Progress.Password);
        }
         
        public static void SetTime() {
            UpdateProgressWindow("Setting Time");


            UpdateProgressWindow("Time Set", Progress.SetTime);
        }

        public static void PingTest() {
            UpdateProgressWindow("Pinging Host Machine");


            UpdateProgressWindow("Ping Successful", Progress.Ping);
        }

        public static void CopyFiles() {
            UpdateProgressWindow("Copying Configurations");



            UpdateProgressWindow("File Copying Successful", Progress.CopyFiles);
        }

        public static void CopyToSecondary() {
            UpdateProgressWindow("Creating Back-Up Files");



            UpdateProgressWindow("Backup Created Successfully", Progress.CopySecondary);
        }

        public static void Reboot() {
            UpdateProgressWindow("Rebooting");



            UpdateProgressWindow("Reboot Successful", Progress.Reboot); 
        }
    }
}