
using System;
using System.Threading;
using System.IO.Ports;

namespace BetterRouterProgram
{
    public class RouterConnection
    {
        static bool moveOn;
        static SerialPort serialPort;

        public static void Connect(string portName, string initPassword, string sysPassword, string routerID, string configDir)
        {
            
            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
            Thread readThread = new Thread(Read);

            // Create a new SerialPort object with default settings.
            serialPort = new SerialPort(portName, 9600);

            // Set the read/write timeouts
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;

            serialPort.Open();
            moveOn = true;
            readThread.Start();

            while (!moveOn)
            {
                
            }

            readThread.Join();
            serialPort.Close();
        }

        public static void Read()
        {
            while (!moveOn)
            {
                try
                {
                    
                }
                catch (TimeoutException) {

                }
            }
        }     
    }
}
