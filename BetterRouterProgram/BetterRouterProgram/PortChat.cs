
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;

namespace BetterRouterProgram
{
    public class PortChat
    {
        static bool moveOn;
        static SerialPort serialPort;

        public static void Run(MainWindow m)
        {
            
            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
            Thread readThread = new Thread(Read);

            // Create a new SerialPort object with default settings.
            serialPort = new SerialPort();

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
