using System;
using System.Text;
using System.Threading;

using NetMQ;
using NetMQ.Sockets;

namespace TestZMQCalibrator
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create context and socket
            using (var server = new ResponseSocket())
            {
                string bindPoint = "tcp://*:5555";
                server.Bind(bindPoint);

                Console.WriteLine("Server listening on {0}", bindPoint);
                Console.WriteLine("press <Esc> to quit.");

                do
                {
                    while (!Console.KeyAvailable)
                    {
                        // Wait for next request from client
                        string message = server.ReceiveFrameString();
                        Console.WriteLine("Recieved message: {0}", message);

                        // Do Work
                        Thread.Sleep(1000);

                        // Send reply back through socket
                        server.SendFrame("Hello from the Calibrator!");
                    }
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
        }
    }
}
