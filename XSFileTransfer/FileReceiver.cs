using System;
using System.IO;

namespace XSFileTransfer
{
    class FileReceiver
    {
        public string DirectoryPath { get; private set; }

        public FileReceiver(string directory)
        {
            DirectoryPath = directory;

            Directory.CreateDirectory(DirectoryPath);
        }

        public void CreateFileFromStream(byte[] data)
        {

        }
    }
}
