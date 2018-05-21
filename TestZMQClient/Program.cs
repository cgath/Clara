using System;
using System.Text;

using NetMQ;
using NetMQ.Sockets;

namespace TestZMQClient
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var client = new RequestSocket())
            {
                client.Connect("tcp://localhost:5555");

                Console.WriteLine("Client connected to tcp://127.0.0.1:5555");
                Console.WriteLine("Press <Enter> to send request. Press <Esc> to quit.");

                ConsoleKeyInfo cki;
                while (true)
                {
                    cki = Console.ReadKey();
                    if (cki.Key == ConsoleKey.Enter)
                    {
                        client.SendFrame("Hello from client!");

                        var message = client.ReceiveFrameString();
                        Console.WriteLine("Recieved {0} from Server.", message);
                    }

                    if (cki.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                }
            }
        }
    }
}
