
using System;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;

namespace BetterRouterProgram
{
    /* Functions that have user input:
    - prompt_reboot: on 
    - 
    */

    //TODO: In // print(...) statements put text in the progress window or a pop up little window

    /*Loading Spinner How To (For CopyToSecondary and CopyFiles):
    https://stackoverflow.com/questions/6359848/wpf-loading-spinner
    https://www.google.com/search?q=wpf+loading+animation&source=lnms&sa=X&ved=0ahUKEwjI-Y_q_97UAhUCSz4KHdKgAtoQ_AUICSgA&biw=1536&bih=736&dpr=1.25
    */

    public class FunctionUtil
    {
        private static ProgressWindow ProgressWindow = null;
        private const string dateFormat = "yyyy/MM/dd HH:mm:ss";

        private static Process Tftp = null;

        private enum Progress : int {
                None = 0,
                Login = 10, 
                Ping = 20,
                CopyFiles = 50,
                CopySecondary = 70, 
                SetTime = 80, 
                Password = 90,
                Reboot = 100,
        };

        public static void InitializeProgressWindow(ProgressWindow pw) {
            ProgressWindow = pw;
            ProgressWindow.progressBar.Value = (int)Progress.None;
        }

        private static void UpdateProgressWindow(string text, Progress value = Progress.None) {
            if (value == Progress.None)
            {
                value = (Progress)ProgressWindow.progressBar.Value;
            }
            if(value != (int)Progress.None) {
                ProgressWindow.progressBar.Value = (int)value;
            }

            ProgressWindow.currentTask.Text += "\n" + text;
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

        public static void SetPassword(string password) {
            UpdateProgressWindow("Setting Password");

            password = password.Trim(' ', '\t', '\r', '\n');
            
            if(password.Equals(SerialConnection.GetSetting("intitial password"))){
                //print('Can\'t change to the same password, skipping step');
                return;
            }

            string message = SerialConnection.RunInstruction(String.Format(
                "SETDefault -SYS NMPassWord = \"{0}\" \"{1}\" \"{2}\"", 
                SerialConnection.GetSetting("intitial password"),
                password, 
                password
            ));

            if(message.Contains("Password changed")) {
                // print('Password successfully changed')
            }
            else if(message.Contains("Invalid password")) {
                
                // print('Password used doesn\'t meet requirements, skipping step')
                return;
            }
	        else{
		        // print('Something is wrong with the password used, skipping step')
                return;
            }

            SerialConnection.RunInstruction(String.Format("setd -ac secret = \"{}\"", password));

            UpdateProgressWindow("Password Set", Progress.Password);
        }
         
        //takes a signed offset which is the number of hours forward or backward the clock must be from CST
        public static void SetTime(string timeZoneString = "") {
            UpdateProgressWindow("Setting Time");

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

            UpdateProgressWindow(setDate.ToString(dateFormat), Progress.SetTime);

            //SerialConnection.RunInstruction("SET - SYS DATE = " + setDate.ToString(dateFormat));

            //Thread.Sleep(2000);

            UpdateProgressWindow("Time Set", Progress.SetTime);
        }

        public static void PingTest() {
            UpdateProgressWindow("Pinging Host Machine");
            
            string message = SerialConnection.RunInstruction("ping " + SerialConnection.GetSetting("router ID"));
            
            if (message.Contains("is alive")) {
                // print('ping successful, local machine connected')
            }
	        else {
		        if(message.Contains("Host unreachable")){
                    // print('IP address cannot be reached')
                }
                else if(message.Contains("Request timed out")) {
                    //           print('ping timed out')
                }

                //TODO: do something like this but with WPF stuff
                // print('ping failed to host: {}'.format(settings['ip_addr']))
                // exit = input('would you like to exit? (y/n): ')
                // if exit == 'y':
			        //  close_connection()                  
                // stop_tftpd()
                // sys.exit()
            }

            UpdateProgressWindow("Ping Successful", Progress.Ping);
        }

        public static void CopyFiles() {
            UpdateProgressWindow("Copying Configurations");

            //    run_instruction('cd')
            //    # parses each file in the list, checks to see if it is supported
            //# then it formats the string for the instruction
            //            for file in files_list:
            //        hostFile = ''
            //        file = file.strip(' \t\r\n')
            //        if file in IMPLICIT_FILES:
            //            if file == 'staticRP.cfg' or file == 'antiacl.cfg' or file == 'boot.ppc':
				        //    hostFile = file
            //            elif file == 'acl.cfg' or file == 'xgsn.cfg':
				        //    hostFile = settings['router_id'] + '_' + file
            //            elif file == 'boot.cfg':
				        //    hostFile = settings['router_id'] + '.cfg'
		          //  else:
			         //   print('{}: host file not supported'.format(file))
            //            continue
            //        reset_connection_buffers()
            //        instr = 'copy {}{} {}\r\n'.format(settings['ip_addr'] + ':', hostFile, file) 
            //        *******use setting config directory to prepend to hostFile
            //        router_connection.write(str_to_byte(instr))
            //        # waiting animation
            //            i = 0
            //        dotcount = 0
            //        currResponse = ''
            //        while True:
			         //   currResponse = router_connection.read().decode("utf-8")
            //            if '#' == currResponse:
				        //    break
            //            elif '.' == currResponse:
				        //    dotcount += 1
            //                if dotcount % 2:
					       //     print('copying file: {} -> {}   [{}]'.format(hostFile, file, spinner[i % len(spinner)]), end = '')
            //                    sleep(0.05)
            //                    print('\r', end = '')
            //                    i += 1
            //        print('copying file: {} -> {}   [ done ]     '.format(hostFile, file))
            //    print('\n')

            UpdateProgressWindow("File Copying Successful", Progress.CopyFiles);
        }

        public static void CopyToSecondary() {
            UpdateProgressWindow("Creating Back-Up Files");

            SerialConnection.RunInstruction("cd a:/");
            SerialConnection.RunInstruction("md /secondary");
            SerialConnection.RunInstruction("copy a:/primary/*.* a:/secondary");
          
            // print('copies created successfully')

            UpdateProgressWindow("Backup Created Successfully", Progress.CopySecondary);
        }

        public static void PromptReboot() {

            ProgressWindow.RebootButton.IsEnabled = true;
            ProgressWindow.RebootText.Opacity = 1.0;
            UpdateProgressWindow("Please Reboot Router", Progress.Reboot - 5);
        }

        public static void HandleReboot()
        {
            UpdateProgressWindow("Rebooting", Progress.Reboot - 5);
            
            //SerialConnection.RunInstruction("rb");
            Thread.Sleep(500);

            UpdateProgressWindow("Reboot Successful", Progress.Reboot);

        }

        public static void StartTftp()
        {
            Tftp = Process.Start(SerialConnection.GetSetting("config directory") + "\\tftpd32.exe");
        }

        public static void StopTftp()
        {
            //TODO: is tftp open?
            Tftp.CloseMainWindow();
            Tftp.Close();
        }
    }
}