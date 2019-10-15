using System;

namespace XSFileTransfer
{
    public class Constants
    {
        public const int MaxPacketSize = 32 * 1024 * 1024;     // 512MByte seems to be the absolute maximum for this
        public const int DefaultPort = 3648;
        public const int DefaultTimeout = 10000;
        public static int MaxConnections = 4;

        public static string DefaultReceiveFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\XSFileTransfer";
    }
}
