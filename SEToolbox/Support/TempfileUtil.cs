using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SEToolbox.Support
{
    public static class TempFileUtil
    {
        private static readonly List<string> TempFiles;
        public static string TempPath;

        static TempFileUtil()
        {
            TempFiles = [];
            string assemblyName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            TempPath = Path.Combine(Path.GetTempPath(), assemblyName);
            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }
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
            string ext = string.IsNullOrEmpty(fileExtension) ? ".tmp" : fileExtension;
            string fileName = Path.Combine(TempPath,$"{Guid.NewGuid()}{ext}");
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
                        // Unable to delete any locked files.
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
                catch
                {
                }
            }

            foreach (DirectoryInfo dir in basePath.GetDirectories())
            {
                try
                {
                    dir.Delete(true);
                }
                catch
                {
                }
            }
        }
    }
}
