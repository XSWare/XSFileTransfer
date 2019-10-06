using System;
using System.IO;
using System.Net;

namespace XSFileTransfer
{
    class FileSender
    {
        IPEndPoint Destination { get; set; }

        public FileSender(IPEndPoint destination)
        {
            Destination = destination;
        }

        public void SendFile(string path)
        {
            using (var fileStream = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                var fileName = Path.GetFileName(path);
                var fileSize = fileStream.Length;
                var lastUpdateTime = DateTime.Now;

                while (fileStream.Position != fileStream.Length)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var writer = new BinaryWriter(memoryStream))
                        {
                            // Make the chunks as big as possible, let the TCP Stack do the rest
                            // Preserve 128 + 24 bytes for Header information (128 bytes for filename, 24 bytes for header info).
                            var chunk = new byte[ushort.MaxValue - (128 + 24)];
                            // [Offset 0] Advance by 2 bytes, first two bytes are packet size, calculated at the end.
                            writer.Seek(2, SeekOrigin.Current);
                            // [Offset 2] Packet Id, 3000 = FileTransfer
                            writer.Write((ushort)3000);
                            // [Offset 4]
                            writer.Write(Path.GetFileName(fileName));
                            // [Offset 6] FileName to be Created (1) / Appended (0) (6-8 = Length Prefix, 9-N = string)
                            writer.Write(fileStream.Position == 0);

                            //Read the payload (file contents chunk) into a buffer
                            var readBytes = fileStream.Read(chunk, 0, chunk.Length);
                            //Resize the buffer to the correct size
                            Array.Resize(ref chunk, readBytes);

                            // [Offset 6 + FileName Length] Write the total file size
                            writer.Write(fileSize);
                            // [Offset 10 + FileName Length] Write size contained in this packet
                            writer.Write((ushort)chunk.Length);
                            // [Offset 12 + FileName Length] Write payload buffer
                            writer.Write(chunk, 0, chunk.Length);

                            var pos = writer.BaseStream.Position;
                            writer.Seek(0, SeekOrigin.Begin);
                            // [Offset 0] Write the full packet size to the first two bytes
                            writer.Write((ushort)pos);

                            // Get the complete packet out of the stream
                            var buffer = memoryStream.ToArray();
                            // Send it
                            Send(buffer);
                        }
                    }
                }
            }
        }

        public void Send(byte[] data)
        {
            
        }
    }
}
