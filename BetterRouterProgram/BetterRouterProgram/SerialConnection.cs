using System;

using System.Threading;
using System.IO.Ports;

namespace BetterRouterProgram
{
    public class SerialConnection
    {
        static SerialPort SerialPort = null;
        static ProgressWindow pw;

        public static void Connect(string portName, string initPassword, string sysPassword, string routerID, string configDir)
        {

            try
            {
                //Thread readThread = new Thread(ReadFromConnection);

                InitializeSerialPort(portName);

                System.Diagnostics.Process.Start(configDir + "\\tftpd32.exe");

                pw = new ProgressWindow();
                pw.Show();

                //CloseConnection()
            }

            //TODO: Better Exception Handling
            catch (System.IO.FileNotFoundException)
            {
                System.Windows.Forms.MessageBox.Show("Unable to locate the Specified File, please try again.");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                System.Windows.Forms.MessageBox.Show("Error: Could not find the TFTP Client executable in the folder specified. Please move the TFTP Application File (.exe) into the desired directory or choose a different directory and try again.");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Original Error: " + ex.Message);
            }

        }

        private static void InitializeSerialPort(string comPort) {
            SerialPort = new SerialPort(comPort, 9600);

            SerialPort.ReadTimeout = 500;
            SerialPort.WriteTimeout = 500;

            SerialPort.Open();
        }

        public static void CloseConnection() {
            SerialPort.Close();
        }

        public static void ResetConnectionBuffers() {
            SerialPort.DiscardInBuffer();
            SerialPort.DiscardOutBuffer();
        }

        public static string ReadResponse(char endChar) {
            char currentResponse = ' ';
            string response = "";

            while (true){
                currentResponse = (char)(SerialPort.ReadChar());
                response += currentResponse;

		        if(currentResponse == '#'){
			        break;
                }   
            }

            return response;
        }

        public static string RunInstruction(string instruction) {
            ResetConnectionBuffers();
	        SerialPort.Write(instruction + "\r\n");
	        message = ReadResponse('#');
	
	        return message;
        }

        //might move to different module
        public static void Login(string username = "root", string password = "") {
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
        }

        //move to different module
        /*public static void SetPassword() {
            pw.currentTask.Text = "Setting Password";


            pw.progressBar.Value = 20;
            pw.currentTask.Text = "Password Set";
        }
            

        public static void Login() {
            pw.currentTask.Text = "Logging In";


            pw.progressBar.Value = 10;
            pw.currentTask.Text = "Log-In Successful";
        }

        public static void SetTime() {
            pw.currentTask.Text = "Setting Time";


            pw.progressBar.Value = 30;
            pw.currentTask.Text = "Time Set";
        }

        public static void PingTest() {
            pw.currentTask.Text = "Pinging Host Machine";


            pw.progressBar.Value = 40;
            pw.currentTask.Text = "Ping Successful";
        }

        public static void CopyFiles() {
            pw.currentTask.Text = "Copying Configurations";


            pw.progressBar.Value = 50;
            pw.currentTask.Text = "File Copying Successful";
        }

        public static void CopyToSecondary() {
            pw.currentTask.Text = "Creating Back-Up Files";


            pw.progressBar.Value = 60;
            pw.currentTask.Text = "File Copying Successful";
        }*/


        //add to other module as well
        /* methods to add methods "cross-referenced"
        +prompt_reboot() - calls function to run instruction
        +run_instructions() - calls run_instruction()*/

    }
}