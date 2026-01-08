using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace SEToolbox.Support
{
    public static class ZipTools
    {
        public static void MakeClearDirectory(string folder)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            Directory.CreateDirectory(folder);
        }

        public static void GZipUncompress(string sourceFileName, string destinationFileName)
        {
            // Low memory, fast extract.
            using FileStream compressedByteStream = new(sourceFileName, FileMode.Open);
            if (File.Exists(destinationFileName))
            {
                File.Delete(destinationFileName);
            }

            using FileStream outStream = new(destinationFileName, FileMode.CreateNew);
            // GZipStream requires using. Do not optimize the stream.
            using GZipStream zip = new(compressedByteStream, CompressionMode.Decompress);
            zip.CopyTo(outStream);
            Log.WriteLine($"Decompressed from {compressedByteStream.Length:#,###0} bytes to {outStream.Length:#,###0} bytes.");
        }

        /// <summary>
        /// Reads only the specified number of bytes to an array.
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="numberBytes"></param>
        /// <returns></returns>
        public static byte[] GZipUncompress(string sourceFileName, int numberBytes)
        {
            using FileStream compressedByteStream = new(sourceFileName, FileMode.Open);
            using GZipStream zip = new(compressedByteStream, CompressionMode.Decompress);
            byte[] newBytes = new byte[numberBytes];
            int bytesRead = zip.Read(newBytes, 0, numberBytes);
            byte[] result = new byte[bytesRead];
            Array.Copy(newBytes, 0, result, 0, bytesRead);
            return result;
        }

        public static void GZipCompress(string sourceFileName, string destinationFileName)
        {
            // Low memory, fast compress.
            using FileStream originalByteStream = new(sourceFileName, FileMode.Open);
            if (File.Exists(destinationFileName))
            {
                File.Delete(destinationFileName);
            }

            using FileStream compressedByteStream = new(destinationFileName, FileMode.CreateNew);
            // GZipStream requires using. Do not optimize the stream.
            using GZipStream compressionStream = new(compressedByteStream, CompressionMode.Compress, true);

            originalByteStream.CopyTo(compressionStream);

            Log.WriteLine($"Compressed from {originalByteStream.Length:#,###0} bytes to {compressedByteStream.Length:#,###0} bytes.");
        }

        /// <summary>
        /// check for Magic Number: 1f 8b
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool IsGzipedFile(string fileName)
        {
            try
            {
                using FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                int byte1 = stream.ReadByte();
                int byte2 = stream.ReadByte();
                return byte1 == 0x1f && byte2 == 0x8b;
            }
            catch
            {
                return false;
            }
        }
    }
}
