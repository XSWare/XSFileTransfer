using System;
using System.Net;
using XSLibrary.Network.Acceptors;
using XSLibrary.Network.Connections;
using XSLibrary.Network.Connectors;
using XSLibrary.Utility;

namespace XSFileTransfer
{
    class Program
    {
        static FileReceiver fileReceiver;
        static Logger logger = new LoggerConsole();

        static void Main(string[] args)
        {
            logger.LogLevel = LogLevel.Detail;

            Console.WriteLine("Receive folder path:");
            fileReceiver = new FileReceiver(Console.ReadLine());

            SecureAcceptor acceptor = new SecureAcceptor(new TCPAcceptor(3648, 10));
            acceptor.Logger = logger;
            acceptor.SecureConnectionEstablished += OnSecureClientConnect;
            acceptor.Run();

            Console.WriteLine("Destination IP:");

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(Console.ReadLine()), 3648);

            FileSender fileSender = new FileSender();

            while (true)
            {
                string filePath = Console.ReadLine();

                Connector<TCPPacketConnection> connector = new SecureConnector();
                connector.Logger = logger;

                TCPPacketConnection connection;

                if (!connector.Connect(endpoint, out connection))
                {
                    Console.WriteLine("unable to send file");
                    continue;
                }

                connection.ReceiveBufferSize = Constants.MaxPacketSize;
                connection.MaxPacketReceiveSize = Constants.MaxPacketSize;

                if (!fileSender.SendFile(filePath,
                    (byte[] data) =>
                    {
                        return connection.Send(data, 10000);
                    }))
                    logger.Log(LogLevel.Error, "sending error");
                else
                    Console.WriteLine("send file successfully");
            }
        }

        static void OnSecureClientConnect(object sender, TCPPacketConnection receiveConnection)
        {
            receiveConnection.MaxPacketReceiveSize = Constants.MaxPacketSize;

            int index = 0;
            byte[] data;
            while (receiveConnection.Receive(out data, out _))
            {
                logger.Log(LogLevel.Information, "Received chunk {0}", index);
                fileReceiver.ReceiveFile(data);
                index++;
            }
        }
    }
}
