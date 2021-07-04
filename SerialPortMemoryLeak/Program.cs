using System;
using System.Diagnostics;
using System.IO.Ports;

namespace SerialPortMemoryLeak
{
    class Program
    {
        private const int ITERATIONS = 20000;
        private const int TIMEOUT = 1; //SerialPort.InfiniteTimeout
        private static string _port;
        
        static void Main(string[] args)
        {
            if (!ParseArgs(args))
            {
                Console.WriteLine("Usage: SerialPortMemoryLeak <serial_port>");
                Environment.Exit(1);
            }

            SerialPort sp = new(_port, 9600, Parity.None, 8, StopBits.One)
            {
                WriteTimeout = TIMEOUT,
                ReadTimeout = TIMEOUT,
                DtrEnable = true
            };

            sp.Open();
            if (!sp.IsOpen)
            {
                Console.WriteLine("Serial Port not open");
                Environment.Exit(1);
            }

            Console.WriteLine("Progress: ");

            for (int i = 1; i <= ITERATIONS; i++)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"Progress: {i,8}/{ITERATIONS}");
                byte[] buffer = new byte[1024];
                try
                {
                    sp.Read(buffer, 0, 1024);
                }
                catch (TimeoutException)
                {
                    continue;
                }
                
            }

            Console.WriteLine($"\nRun complete. Press <Enter> to continue.");
            Console.ReadLine();
            sp.Close();
        }

        private static bool ParseArgs(string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    _port = args[0];
                    return true;
                default:
                    return false;
            }
        }
    }
}
