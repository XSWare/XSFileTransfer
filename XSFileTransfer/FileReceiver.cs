using System;
using System.IO;
using XSLibrary.Utility;
using XSLibrary.ThreadSafety.Executors;

namespace XSFileTransfer
{
    class FileReceiver
    {
        public string DirectoryPath { get; private set; }
        public LoggerConsole Logger = new LoggerConsole();

        SingleThreadExecutor m_directoryLock = new SingleThreadExecutor();

        public FileReceiver(string directory)
        {
            DirectoryPath = directory;
            Directory.CreateDirectory(DirectoryPath);

            Logger.LogLevel = LogLevel.Warning;
        }

        public bool ReceiveFile(byte[] packet, ref string filePath)
        {
            using (var stream = new MemoryStream(packet))
            {
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position < stream.Length)
                    {
                        if (filePath == "")
                            filePath = reader.ReadString();
                        else if (filePath != reader.ReadString())
                        {
                            Logger.Log(LogLevel.Error, "Header data of file \"{0}\" corrupt!", filePath);
                            continue;
                        }

                        var dataSize = reader.ReadInt64();
                        bool createFile = reader.ReadBoolean();
                        bool lastChunk = reader.ReadBoolean();
                        int chunkSize = reader.ReadInt32();

                        long currentSize;

                        if(createFile)
                            Logger.Log(LogLevel.Priority, "Receiving file \"{0}\"", filePath);

                        Logger.Log(LogLevel.Information, "Decoding chunk with {0} bytes.", chunkSize);

                        var chunk = new byte[chunkSize];
                        var read = reader.Read(chunk, 0, chunkSize);

                        string directory = Path.GetDirectoryName(DirectoryPath + "\\" + filePath);

                        m_directoryLock.Execute(() =>
                        {
                            if (!Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                                MakeFolderWritable(directory);
                            }
                        });

                        try
                        {
                            using (var filestream = new FileStream(DirectoryPath + "\\" + filePath, createFile ? FileMode.Create : FileMode.Append))
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
                            Logger.Log(LogLevel.Priority, "Receiving of file \"{0}\" complete.", filePath);
                            return true;
                        }
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
