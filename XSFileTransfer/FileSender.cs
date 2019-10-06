using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using XSLibrary.Utility;

namespace XSFileTransfer
{
    class FileSender
    {
        public delegate bool SendFunction(byte[] data); 
        IPEndPoint Destination { get; set; }

        // declare this as class-wide to avoid having this garbage collected
        LoggerConsolePeriodic Logger = new LoggerConsolePeriodic();

        public FileSender()
        {
            Logger.LogLevel = LogLevel.Detail;
            Logger.Prefix = "[SEND] ";
            Logger.Suffix = "\n";
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

                byte[] chunk = new byte[0];
                while (fileStream.Position != fileStream.Length)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var writer = new BinaryWriter(memoryStream))
                        {
                            long leftover = fileStream.Length - fileStream.Position;
                            int maxChunkSize = Constants.MaxPacketSize - (128 + 8 + 2 + 4);

                            bool createNew = fileStream.Position == 0;
                            int chunkSize = Math.Min((int)leftover, maxChunkSize);

                            bool lastChunk = chunkSize == leftover;

                            //if(chunk.Length != chunkSize)
                                chunk = new byte[chunkSize];

                            fileStream.Read(chunk, 0, chunk.Length);

                            writer.Write(Path.GetFileName(fileName));
                            writer.Write(fileSize);
                            writer.Write(createNew);
                            writer.Write(lastChunk);
                            writer.Write(chunkSize);
                            writer.Write(chunk);

                            Logger.Log(LogLevel.Information, "Sending chunk with {0} byte of data", chunkSize);
                            if (!send(memoryStream.ToArray()))
                                return false;

                            //Thread.Sleep(500);
                        }
                    }
                }
            }

            return true;
        }
    }
}
