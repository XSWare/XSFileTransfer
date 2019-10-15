using System;
using System.IO;
using System.Net;
using System.Threading;
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
                        string path = arguments.Length > 1 ? AssemblePath(arguments, 1, arguments.Length) : Constants.DefaultReceiveFolder;

                        StartReceiving(path, Constants.DefaultPort);
                        logger.Log(LogLevel.Priority, "Saving received files to: \"{0}\"", path);
                    }
                    else if (arguments.Length > 1 && arguments[0] == "send")
                    {
                        destination = AddressResolver.Resolve(arguments[1], Constants.DefaultPort);
                        logger.Log(LogLevel.Priority, "Enter file path to send to " + destination);
                    }
                    else if(destination != null)
                    {
                        Send(cmd.Trim('\"'));
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
            logger.Log(LogLevel.Priority, "send <IP>:<Port>\t// port is optional, default = {0}", Constants.DefaultPort);
            logger.Log(LogLevel.Priority, "receive <path>\t\t// path is optional, default = \"{0}\"", Constants.DefaultReceiveFolder);
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

        static void Send(string path)
        {
            if (File.Exists(path))
                SendSingleFile(path, "");
            else
            {
                if (!Directory.Exists(path))
                {
                    logger.Log(LogLevel.Error, "Invalid file/directory path.");
                    return;
                }

                string directory = Path.GetDirectoryName(path);

                string[] filePaths = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

                bool[] results = new bool[filePaths.Length];
                int maxConnections = Constants.MaxConnections; // threadsafe copy
                Semaphore mutex = new Semaphore(maxConnections, maxConnections);
                int index = 0;
                foreach (string filePath in filePaths)
                {
                    mutex.WaitOne();
                    int i = index;  // copy to avoid race condition
                    DebugTools.ThreadpoolStarter("file send thread", () =>
                    {
                        results[i] = SendSingleFile(filePath, directory);
                        mutex.Release();
                    });

                    index++;
                }

                // sync threads
                for (int i = 0; i < maxConnections; i++)
                    mutex.WaitOne();

                // check if any result was false
                int fails = 0;
                foreach (bool sendResult in results)
                    fails += sendResult ? 0 : 1;

                if (fails > 0)
                    logger.Log(LogLevel.Error, "Failed to send {0} out of {1} files!", fails, filePaths.Length);
                else
                    logger.Log(LogLevel.Priority, "All files sent successfully.");
            }
        }

        static bool SendSingleFile(string filePath, string directory)
        {
            logger.Log(LogLevel.Priority, "Sending file {0}", filePath);
            if (!Connect(out TCPPacketConnection connection))
            {
                logger.Log(LogLevel.Error, "File transfer connection could not be established for file {0}", filePath);
                return false;
            }

            if (!fileSender.SendFile(filePath, directory, connection.Send))
            {
                logger.Log(LogLevel.Error, "Error while trying to send chunk of file {0}", filePath);
                return false;
            }

            logger.Log(LogLevel.Priority, "Sent file \"{0}\" successfully.", filePath);

            connection.Disconnect();
            return true;
        }

        static bool Connect(out TCPPacketConnection connection)
        {
            SecureConnector connector = new SecureConnector();
            connector.Logger = logger;
            connector.Crypto = CryptoType.EC25519;

            if (!connector.Connect(destination, out connection))
            {

                return false;
            }

            connection.SendTimeout = Constants.DefaultTimeout;

            return true;
        }

        static void OnSecureClientConnect(object sender, TCPPacketConnection receiveConnection)
        {
            receiveConnection.MaxPacketReceiveSize = Constants.MaxPacketSize;
            receiveConnection.ReceiveBufferSize = Constants.MaxPacketSize;
            receiveConnection.ReceiveTimeout = Constants.DefaultTimeout;

            bool finished = false;
            string fileName = "";
            byte[] data;
            while (!finished && receiveConnection.Connected)
            {
                if (!receiveConnection.Receive(out data, out _))
                {
                    if (!finished)
                        logger.Log(LogLevel.Error, "Receiving of file {0}failed.", fileName != "" ? "\"" + fileName + "\" " : "");
                    return;
                }

                finished = fileReceiver.ReceiveFile(data, ref fileName);
            }
        }
    }
}
