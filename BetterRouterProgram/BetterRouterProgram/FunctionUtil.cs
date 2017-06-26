
using System;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;

namespace BetterRouterProgram
{
    public class FunctionUtil
    {
        private static ProgressWindow ProgressWindow = null;
        private static string dateFormat = "yyyyMMdd";

        enum Progress : int {
                None = -1,
                Login = 10, 
                Ping = 20,
                CopyFiles = 50,
                CopySecondary = 70, 
                SetTime = 80, 
                Password = 90,
                Reboot = 100
            };  

        public static void InitializeProgressWindow(ProgressWindow pw) {
            ProgressWindow = pw;
        }

        private static void UpdateProgressWindow(string text, int value = (int)Progress.None) {
            if(value > (int)Progress.None) {
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

            UpdateProgressWindow("Login Successful", (int)Progress.Login);
        }

        // Get the return value of Process.Start in order to stop the process
        public static void StopProcess(Process p)
        {
            p.CloseMainWindow();
            p.Close();
        }

        public static void SetPassword() {
            UpdateProgressWindow("Setting Password");



            UpdateProgressWindow("Password Set", (int)Progress.Password);
        }
         
        public static void SetTime(int offset = 0) {
            UpdateProgressWindow("Setting Time");

            // day = datetime.now().strftime('%Y/%m/%d')
            // minutesec = datetime.now().strftime('%M:%S')
            // hour = int(datetime.now().strftime('%H')) + offset
            // date = '{} {}:{}'.format(day, hour, minutesec)
            // run_instruction('SET -SYS DATE = {}'.format(date))

            DateTime setDate = DateTime.Now.AddHours(offset);

            //SET - SYS DATE = 2017 / 05 / 19 12:02
            //may be setDate.DateTime
            SerialConnection.RunInstruction("SET -SYS DATE = " + setDate.ToString());

            UpdateProgressWindow("Time Set", (int)Progress.SetTime);
        }

        public static void PingTest() {
            UpdateProgressWindow("Pinging Host Machine");


            UpdateProgressWindow("Ping Successful", (int)Progress.Ping);
        }

        public static void CopyFiles() {
            UpdateProgressWindow("Copying Configurations");



            UpdateProgressWindow("File Copying Successful", (int)Progress.CopyFiles);
        }

        public static void CopyToSecondary() {
            UpdateProgressWindow("Creating Back-Up Files");



            UpdateProgressWindow("Backup Created Successfully", (int)Progress.CopySecondary);
        }

        public static void Reboot() {
            UpdateProgressWindow("Rebooting");



            UpdateProgressWindow("Reboot Successful", (int)Progress.Reboot); 
        }
    }
}