using System;
using System.Threading;
using System.IO.Ports;

namespace BetterRouterProgram
{
    public class SerialConnection
    {
        private static SerialPort SerialPort = null;

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

        


        //add to other module as well
        /* methods to add methods "cross-referenced"
        +prompt_reboot() - calls function to run instruction
        +run_instructions() - calls run_instruction()*/

    }
}