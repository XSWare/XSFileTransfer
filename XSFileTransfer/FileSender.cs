using System;
using System.IO;
using XSLibrary.Utility;

namespace XSFileTransfer
{
    class FileSender
    {
        public delegate bool SendFunction(byte[] data);

        public LoggerConsolePeriodic Logger = new LoggerConsolePeriodic();

        public FileSender()
        {
            Logger.LogLevel = LogLevel.Warning;
            Logger.Prefix = "[SEND] ";
            Logger.Suffix = "\n";
        }

        public bool SendFiles(string path, SendFunction send)
        {
            if (File.Exists(path))
                return SendFile(path, send);
            else
            {
                if(!Directory.Exists(path))
                    return false;

                string[] filePaths = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

                foreach(string filePath in filePaths)
                {
                    if (!SendFile(filePath, send))
                        return false;
                }

                return true;
            }
        }

        public bool SendFile(string path, SendFunction send)
        {
            if (!File.Exists(path))
                return false;

            using (var fileStream = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                var fileName = Path.GetFileName(path);
                var fileSize = fileStream.Length;
                var lastUpdateTime = DateTime.Now;
                int requiredSizeFilename = fileName.Length + sizeof(short);

                byte[] chunk = new byte[0];
                while (fileStream.Position != fileStream.Length)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var writer = new BinaryWriter(memoryStream))
                        {
                            long leftover = fileStream.Length - fileStream.Position;
                            int maxChunkSize = Constants.MaxPacketSize - (requiredSizeFilename + 8 + 2 + 4);

                            bool createNew = fileStream.Position == 0;
                            int chunkSize;

                            if (leftover > int.MaxValue)
                                chunkSize = maxChunkSize;
                            else
                                chunkSize = Math.Min((int)leftover, maxChunkSize);

                            bool lastChunk = chunkSize == leftover;

                            chunk = new byte[chunkSize];

                            fileStream.Read(chunk, 0, chunk.Length);

                            writer.Write(fileName);
                            writer.Write(fileSize);
                            writer.Write(createNew);
                            writer.Write(lastChunk);
                            writer.Write(chunkSize);
                            writer.Write(chunk);

                            Logger.Log(LogLevel.Information, "Sending chunk with {0} byte of data", chunkSize);
                            if (!send(memoryStream.ToArray()))
                                return false;

                            if(!lastChunk)
                                Logger.Log(LogLevel.Priority, "Progress: {0}%", 100 * fileStream.Position / fileSize);
                            else
                                Logger.Log(LogLevel.Priority, "Sent file \"{0}\" successfully.", path);
                        }
                    }
                }
            }

            return true;
        }
    }
}
