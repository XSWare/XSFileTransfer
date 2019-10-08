using System.IO;
using XSLibrary.Utility;

namespace XSFileTransfer
{
    class FileReceiver
    {
        public string DirectoryPath { get; private set; }

        public LoggerConsolePeriodic Logger = new LoggerConsolePeriodic();

        public FileReceiver(string directory)
        {
            DirectoryPath = directory;
            Directory.CreateDirectory(DirectoryPath);

            Logger.LogLevel = LogLevel.Warning;
            Logger.Prefix = "[RECEIVE] ";
            Logger.Suffix = "\n";
        }

        public bool ReceiveFile(byte[] packet)
        {
            using (var stream = new MemoryStream(packet))
            {
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position < stream.Length)
                    {
                        var name = reader.ReadString();
                        var dataSize = reader.ReadInt64();
                        bool createFile = reader.ReadBoolean();
                        bool lastChunk = reader.ReadBoolean();
                        int chunkSize = reader.ReadInt32();

                        long currentSize;

                        Logger.Log(LogLevel.Information, "Decoding chunk with {0} bytes.", chunkSize);

                        var chunk = new byte[chunkSize];
                        var read = reader.Read(chunk, 0, chunkSize);

                        using (var filestream = new FileStream(DirectoryPath + "\\" + name, createFile ? FileMode.Create : FileMode.Append))
                        {
                            filestream.Write(chunk, 0, chunk.Length);
                            currentSize = filestream.Length;
                        }

                        if (lastChunk)
                        {
                            Logger.Log(LogLevel.Priority, "Receiving of file \"{0}\" complete.", name);
                            return true;
                        }
                        else
                            Logger.Log(LogLevel.Priority, "Progress: {0}%", 100 * currentSize / dataSize);
                    }

                    return false;
                }
            }
        }
    }
}
