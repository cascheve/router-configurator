using System;
using System.Threading;
using System.IO.Ports;

namespace BetterRouterProgram
{
    public class FunctionUtil
    {
        private static ProgressWindow ProgressWindow = null;
        private static const int LoginProgress = 10;
        private static const int PingProgress = 20;
        private static const int CopyFilesProgress = 50;
        private static const int CopySecondaryProgress = 70;
        private static const int SetTimeProgress = 80;
        private static const int PasswordProgress = 90;        
        private static const int RebootProgress = 100;

        public static void initializeProgressWindow(ProgressWindow pw) {
            ProgressWindow = pw;
        }

        private static void UpdateProgressWindow(string text, int vaule = -1) {
            if(value > -1) {
                ProgressWindow.progressBar.Value = value;
            }
            ProgressWindow.currentTask.Text = text;
        }

        //might move to different module
        public static void Login(string username = "root", string password = "") {
            UpdateProgressWindow("Logging In")

            SerialPort.Write("\r\n");
            Thread.Sleep(500);
            
            SerialPort.Write(username + "\r\n")
            Thread.Sleep(500);
            
            SerialPort.Write(password + "\r\n")
            Thread.Sleep(500)
            
            /*check login success
            if router_connection.read(bytes_to_read()).decode("utf-8").rstrip().endswith('#'):
                print('login successful')
            else:
                print('login failed, closing script')
                stop_tftpd()
                close_connection()
                input('press any key to continue...')
                sys.exit()*/

            UpdateProgressWindow("Login Successful", LoginProgress)
        }

        public static void SetPassword() {
            UpdateProgressWindow("Setting Password")



            UpdateProgressWindow("Password Set", PasswordProgress)
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