using System;
using System.Net;
using XSLibrary.Cryptography.ConnectionCryptos;
using XSLibrary.Network;
using XSLibrary.Network.Acceptors;
using XSLibrary.Network.Connections;
using XSLibrary.Network.Connectors;
using XSLibrary.Utility;

namespace XSFileTransfer
{
    class Program
    {
        static IPEndPoint destination = null;
        static FileSender fileSender = new FileSender();
        static FileReceiver fileReceiver = null;
        static SecureAcceptor acceptor = null;
        static TCPPacketConnection connection = null;
        static Logger logger = new LoggerConsole();

        static void Main(string[] args)
        {
            logger.LogLevel = LogLevel.Error;

            fileSender.Logger.LogLevel = logger.LogLevel;

            DisplayCommands();

            while (true)
            {
                try
                {
                    string cmd = Console.ReadLine();

                    if (cmd == "exit")
                        return;

                    string[] arguments = cmd.Split(' ');

                    if (arguments.Length > 0 && arguments[0] == "receive")
                    {
                        string path = arguments.Length > 1 ? AssemblePath(arguments, 1, arguments.Length - 1) : Constants.DefaultReceiveFolder;
                        int port = arguments.Length > 2 ? Convert.ToInt32(arguments[arguments.Length - 1]) : Constants.DefaultPort;

                        StartReceiving(path, port);
                        logger.Log(LogLevel.Priority, "Saving received files to: \"{0}\"", path);
                    }
                    else if (arguments.Length > 1 && arguments[0] == "send")
                    {
                        destination = AddressResolver.Resolve(arguments[1], Constants.DefaultPort);
                        logger.Log(LogLevel.Priority, "Enter file path to send to " + destination);
                    }
                    else if(destination != null)
                    {
                        Send(destination, cmd.Trim('\"'));
                    }
                    else
                    {
                        logger.Log(LogLevel.Error, "Command not recognized.");
                        DisplayCommands();
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, ex.Message);
                }
            }
        }

        static void DisplayCommands()
        {
            logger.Log(LogLevel.Priority, "Commands:");
            logger.Log(LogLevel.Priority, "send <IP>:<Port>\t\t// port is optional, default = {0}", Constants.DefaultPort);
            logger.Log(LogLevel.Priority, "receive <path> <port> \t\t// path is optional, default = \"{0}\" // port is optional, default = {1}", Constants.DefaultReceiveFolder, Constants.DefaultPort);
            logger.Log(LogLevel.Priority, "exit\n");
        }

        static string AssemblePath(string[] arguments, int offset, int end)
        {
            string path = "";
            for (int i = offset; i < arguments.Length && i < end; i++)
                path += arguments[i] + " ";

            return path.Trim(' ').Trim('\"');
        }

        static void StartReceiving(string receiveFolder, int port)
        {
            if (acceptor != null)
                acceptor.Stop();

            fileReceiver = new FileReceiver(receiveFolder);
            fileReceiver.Logger.LogLevel = logger.LogLevel;

            acceptor = new SecureAcceptor(new TCPAcceptor(port, 10));
            acceptor.Logger = logger;
            acceptor.Crypto = CryptoType.EC25519;
            acceptor.SecureConnectionEstablished += OnSecureClientConnect;
            acceptor.Run();
        }

        static void Send(IPEndPoint destination, string filepath)
        {
            SecureConnector connector = new SecureConnector();
            connector.Logger = logger;
            connector.Crypto = CryptoType.EC25519;

            if (!connector.Connect(destination, out connection))
            {
                logger.Log(LogLevel.Error, "File transfer connection could not be established!");
                return;
            }

            connection.SendTimeout = Constants.DefaultTimeout;

            if (!fileSender.SendFile(filepath, connection.Send))
                logger.Log(LogLevel.Error, "Error while trying to send chunk!");

            connection.Disconnect();
        }

        static void OnSecureClientConnect(object sender, TCPPacketConnection receiveConnection)
        {
            receiveConnection.MaxPacketReceiveSize = Constants.MaxPacketSize;
            receiveConnection.ReceiveBufferSize = Constants.MaxPacketSize;
            receiveConnection.ReceiveTimeout = Constants.DefaultTimeout;

            int index = 0;
            byte[] data;
            while (receiveConnection.Receive(out data, out _))
            {
                logger.Log(LogLevel.Information, "Received chunk {0}", index);
                if(fileReceiver.ReceiveFile(data))
                {
                    receiveConnection.Disconnect();
                    return;
                }
                index++;
            }
        }
    }
}
