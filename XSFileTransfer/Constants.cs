using System;

namespace XSFileTransfer
{
    public class Constants
    {
        public const int MaxPacketSize = 1 * 1024 * 1024;   // the chunk size needs to go through before the timeout is hit
        public const int DefaultPort = 3648;
        public const int DefaultTimeout = 30000;            // 1 MByte per 30 seconds needs a minimum transfer rate of 32KByte/s
        public static int MaxConnections = 2;

        public static string DefaultReceiveFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\XSFileTransfer";
    }
}
