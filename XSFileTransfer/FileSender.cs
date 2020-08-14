using System;
using System.IO;
using XSLibrary.Utility;

namespace XSFileTransfer
{
    class FileSender
    {
        public delegate bool SendFunction(byte[] data);

        public LoggerConsole Logger = new LoggerConsole();

        public FileSender()
        {
            Logger.LogLevel = LogLevel.Warning;
        }

        public bool SendFile(string path, string directory, SendFunction send)
        {
            if (!File.Exists(path))
                return false;

            FileStream fileStream;

            try { fileStream = File.Open(path, FileMode.Open, FileAccess.Read); }
            catch
            {
                Logger.Log(LogLevel.Error, "Could not open file: \"{0}\"", path);
                return false;
            }

            string destinationPath;
            if (directory.Length <= 0)
                destinationPath = Path.GetFileName(path);
            else
                destinationPath = path.Replace(directory + "\\", "");

            var fileSize = fileStream.Length;
            int requiredSizeFilename = destinationPath.Length + sizeof(int);

            while (fileStream.Position != fileStream.Length)
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(memoryStream))
                    {
                        const int fileSizeLength = 8;
                        const int boolFlagLenth = 1;
                        const int chunkSizeLength = 4;

                        long leftover = fileStream.Length - fileStream.Position;
                        int maxChunkSize = Constants.MaxPacketSize - (requiredSizeFilename + fileSizeLength + (2 * boolFlagLenth) + chunkSizeLength);

                        if (maxChunkSize <= 0)
                        {
                            Logger.Log(LogLevel.Error, "Filename \"{0}\" size exceeding packet limit!", path);
                            return false;
                        }

                        bool createNew = fileStream.Position == 0;
                        int chunkSize;

                        if (leftover > int.MaxValue)
                            chunkSize = maxChunkSize;
                        else
                            chunkSize = Math.Min((int)leftover, maxChunkSize);

                        bool lastChunk = chunkSize == leftover;

                        byte[] chunk = new byte[chunkSize];

                        fileStream.Read(chunk, 0, chunk.Length);

                        writer.Write(destinationPath);
                        writer.Write(fileSize);
                        writer.Write(createNew);
                        writer.Write(lastChunk);
                        writer.Write(chunkSize);
                        writer.Write(chunk);

                        Logger.Log(LogLevel.Information, "Sending chunk with {0} byte of data", chunkSize);
                        if (!send(memoryStream.ToArray()))
                        {
                            fileStream.Dispose();
                            return false;
                        }
                    }
                }
            }

            fileStream.Dispose();

            return true;
        }
    }
}
