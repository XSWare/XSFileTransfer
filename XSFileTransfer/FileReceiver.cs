using System;
using System.IO;
using System.Security.AccessControl;
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
                        var destinationPath = reader.ReadString();
                        var dataSize = reader.ReadInt64();
                        bool createFile = reader.ReadBoolean();
                        bool lastChunk = reader.ReadBoolean();
                        int chunkSize = reader.ReadInt32();

                        long currentSize;

                        Logger.Log(LogLevel.Information, "Decoding chunk with {0} bytes.", chunkSize);

                        var chunk = new byte[chunkSize];
                        var read = reader.Read(chunk, 0, chunkSize);

                        string directory = Path.GetDirectoryName(DirectoryPath + "\\" + destinationPath);
                        Directory.CreateDirectory(directory);
                        MakeFolderWritable(directory);

                        try
                        {
                            using (var filestream = new FileStream(DirectoryPath + "\\" + destinationPath, createFile ? FileMode.Create : FileMode.Append))
                            {
                                filestream.Write(chunk, 0, chunk.Length);
                                currentSize = filestream.Length;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LogLevel.Error, "Could not write chunk: " + ex.Message);
                            continue;
                        }

                        if (lastChunk)
                        {
                            Logger.Log(LogLevel.Priority, "Receiving of file \"{0}\" complete.", destinationPath);
                            return true;
                        }
                        else
                            Logger.Log(LogLevel.Priority, "Progress: {0}%", 100 * currentSize / dataSize);
                    }

                    return false;
                }
            }
        }

        private void MakeFolderWritable(string Folder)
        {
            if (IsFolderReadOnly(Folder))
            {
                DirectoryInfo oDir = new DirectoryInfo(Folder);
                oDir.Attributes = oDir.Attributes & ~FileAttributes.ReadOnly;
            }
        }
        private bool IsFolderReadOnly(string Folder)
        {
            DirectoryInfo oDir = new DirectoryInfo(Folder);
            return ((oDir.Attributes & FileAttributes.ReadOnly) > 0);
        }
    }
}
