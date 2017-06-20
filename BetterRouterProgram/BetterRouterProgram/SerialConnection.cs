using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Windows.Controls;
using System.Windows.Media;

namespace BetterRouterProgram
{
    public class SerialConnection
    {
        static SerialPort serialPort = null;
        

        /*public static SerialPort Instance() {
            return serialPort;
        }*/

        public static void InitializeSerialPort() {
            serialPort = new SerialPort();

            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;

            serialPort.Open();
        }

        public static string ReadFromConnection(string endChar) {
            string message = "";


            return message;

        }

        public static void WriteToConnection(str message) {

        }

    }
}