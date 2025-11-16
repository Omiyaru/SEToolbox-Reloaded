using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SEToolbox.Support
{
    public static class TempFileUtil
    {
        private static readonly List<string> TempFiles;
        public static readonly string TempPath;

        static TempFileUtil()
        {
            TempFiles = [];
            TempPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location));
            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);
        }

        /// <summary>
        /// Generates a temporary filename in the 'c:\users\%username%\AppData\Local\Temp\%ApplicationName%' path.
        /// </summary>
        /// <returns></returns>
        public static string NewFileName()
        {
            return NewFileName(null);
        }

        /// <summary>
        /// Generates a temporary filename in the 'c:\users\%username%\AppData\Local\Temp\%ApplicationName%' path.
        /// </summary>
        /// <param name="fileExtension">optional file extension in the form '.ext'</param>
        /// <returns></returns>
        public static string NewFileName(string fileExtension)
        {
            string fileName;

            if (string.IsNullOrEmpty(fileExtension))
            {
               fileName = Path.Combine(TempPath, Guid.NewGuid() + ".tmp");
            }
            else
            {
               fileName = Path.Combine(TempPath, Guid.NewGuid() + fileExtension);
            }

            TempFiles.Add(fileName);

            return fileName;
        }

        /// <summary>
        /// Cleanup, and remove all Temporary files.
        /// </summary>
        public static void Dispose()
        {
            foreach (var fileName in TempFiles)
            {
                if (File.Exists(fileName))
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch
                    {
                        // Unable to dispose file
                    }
                }
            }

            TempFiles.Clear();
        }

        public static void DestroyTempFiles()
        {
            DirectoryInfo basePath = new(TempPath);
            foreach (FileInfo file in basePath.GetFiles())
            {
                try
                {
                    file.Delete();
                }
                catch { }
            }

            foreach (DirectoryInfo dir in basePath.GetDirectories())
            {
                try
                {
                    dir.Delete(true);
                }
                catch { }
            }
        }
    }
}
