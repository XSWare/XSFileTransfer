using System;
using System.Net;
using System.Net.Sockets;
using XSLibrary.Network.Acceptors;
using XSLibrary.Network.Connections;
using XSLibrary.Network.Connectors;
using XSLibrary.Utility;

namespace XSFileTransfer
{
    class Program
    {
        static FileReceiver fileReceiver;

        static void Main(string[] args)
        {
            Logger logger = new LoggerConsole();
            logger.LogLevel = LogLevel.Detail;

            Console.WriteLine("Receive folder path:");
            fileReceiver = new FileReceiver("C:\\Receive"/*Console.ReadLine()*/);

            TCPAcceptor acceptor = new TCPAcceptor(3648, 10);
            acceptor.Logger = logger;
            acceptor.ClientConnected += OnClientConnect;
            acceptor.Run();

            Console.WriteLine("Destination IP:");

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"/*Console.ReadLine()*/), 3648);

            FileSender fileSender = new FileSender();

            while (true)
            {
                string filePath = Console.ReadLine();

                Connector<TCPConnection> connector = new TCPConnector();
                connector.Logger = logger;

                TCPConnection connection;

                if (!connector.Connect(endpoint, out connection))
                {
                    Console.WriteLine("unable to send file");
                    continue;
                }

                //connection.MaxPackageReceiveSize = Constants.MaxPacketSize;

                TCPPacketConnection.PackageParser parser = new TCPPacketConnection.PackageParser();

                parser.MaxPackageSize = Constants.MaxPacketSize;

                fileSender.SendFile(filePath,
                    //(byte[] data) =>
                    //{
                    //    byte[] packet = new byte[data.Length + 5];
                    //    Array.Copy(TCPPacketConnection.CreateHeader(data.Length), 0, packet, 0, 5);
                    //    Array.Copy(data, 0, packet, 5, data.Length);

                    //    int position = 0;

                    //    while (position < packet.Length)
                    //    {
                    //        int leftover = packet.Length - position;
                    //        int chunkSize = Math.Min(leftover, 32000);
                    //        byte[] chunk = new byte[chunkSize];

                    //        Array.Copy(packet, position, chunk, 0, chunkSize);

                    //        parser.AddData(chunk);
                    //        while (!parser.NeedsFreshData)
                    //        {
                    //            parser.ParsePackage();
                    //            if (parser.PackageFinished)
                    //            {
                    //                chunk = parser.GetPackage();
                    //                fileReceiver.ReceiveFile(chunk);
                    //            }
                    //        }

                    //        position += chunkSize;
                    //    }

                    //    return true;
                    //});

                    (byte[] data) => connection.Send(data, 10000));

                //    (byte[] data) =>
                //{
                //    fileReceiver.ReceiveFile(data);
                //    return true;
                //});

                Console.WriteLine("send file successfully");
            }
        }

        static void OnSecureClientConnect(object sender, TCPPacketConnection receiveConnection)
        {
            byte[] data;
            while (receiveConnection.Receive(out data, out _))
            {
                //if (data.Length < 140)
                //    continue;

                fileReceiver.ReceiveFile(data);
            }
        }

        static void OnClientConnect(object sender, Socket acceptedSocket)
        {
            TCPConnection connection = new TCPConnection(acceptedSocket);

            byte[] data;
            while(connection.Receive(out data, out _))
            {
                fileReceiver.ReceiveFile(data);
            }
        }
    }
}
