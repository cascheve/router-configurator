
using System;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;

namespace BetterRouterProgram
{
    public class FunctionUtil
    {
        private static ProgressWindow ProgressWindow = null;
<<<<<<< HEAD
        private static Process Tftp = null;
        private enum Progress : int {
=======
        private static string dateFormat = "yyyyMMdd";

        enum Progress : int {
>>>>>>> b825209cd02d9f57333344d2dfbc2d10b26f7eca
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

        private static void UpdateProgressWindow(string text, Progress value = Progress.None) {
            if(value != Progress.None) {
                ProgressWindow.progressBar.Value = (int)value;
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

            UpdateProgressWindow("Login Successful", Progress.Login);
        }

        public static void SetPassword() {
            UpdateProgressWindow("Setting Password");



            UpdateProgressWindow("Password Set", Progress.Password);
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

        public static void StartTftp(string configDir)
        {
            Tftp = Process.Start(configDir + "\\tftpd32.exe");
        }

        public static void StopTftp(Process p)
        {
            Tftp.CloseMainWindow();
            Tftp.Close();
        }
    }
}