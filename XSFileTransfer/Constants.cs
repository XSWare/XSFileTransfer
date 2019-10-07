namespace XSFileTransfer
{
    public class Constants
    {
        public const int MaxPacketSize = 1 * 1024 * 1024;     // 512MByte seems to be the absolute maximum for this
        public const int DefaultPort = 3648;

        public const string DefaultReceiveFolder = "C:\\Receive";
    }
}
