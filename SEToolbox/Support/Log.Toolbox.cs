using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Res = SEToolbox.Properties.Resources;
using System.Linq;

namespace SEToolbox.Support;

partial class Log
{
    public static void Exception(Exception exception)
    {
        var diagReport = new StringBuilder();
        diagReport.AppendLine(Res.ClsErrorUnhandled);
        var appFile = Path.GetFullPath(Assembly.GetEntryAssembly().Location);
        var appFilePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        Dictionary<string, string> environmentVariables = Environment.GetEnvironmentVariables() as Dictionary<string, string>;
        Dictionary<string, string> environmentInfo = new()
         {
            { Res.ClsErrorApplication, ObfuscatePathNames(appFile) },
            { Res.ClsErrorCommandLine, ObfuscatePathNames(Environment.CommandLine) },
            { Res.ClsErrorCurrentDirectory, ObfuscatePathNames(Environment.CurrentDirectory) },
            { Res.ClsErrorSEBinPath, GlobalSettings.Default.SEBinPath },
            { Res.ClsErrorSEBinVersion, GlobalSettings.Default.SEVersion.ToString()},
            { Res.ClsErrorProcessorCount, Environment.ProcessorCount.ToString() },
            { Res.ClsErrorOSVersion, Environment.OSVersion.ToString() },
            { Res.ClsErrorVersion, Environment.Version.ToString() },
            { Res.ClsErrorIs64BitOperatingSystem, Environment.Is64BitOperatingSystem.ToString() },
            { Res.ClsErrorIntPtrSize, IntPtr.Size.ToString() },
            { Res.ClsErrorIsAdmin, ToolboxUpdater.IsRunningElevated().ToString() },
            { Res.ClsErrorCurrentUICulture, CultureInfo.CurrentUICulture.IetfLanguageTag },
            { Res.ClsErrorCurrentCulture, CultureInfo.CurrentCulture.IetfLanguageTag },
            { Res.ClsErrorTimesStartedTotal, GlobalSettings.TimesStartedInfo.Total.ToString() },
            { Res.ClsErrorTimesStartedLastReset, GlobalSettings.TimesStartedInfo.LastReset.ToString() },
            { Res.ClsErrorTimesStartedLastGameUpdate, GlobalSettings.TimesStartedInfo.LastGameUpdate.ToString() }
        };

        foreach (var entry in environmentVariables)
        {
            diagReport.Append($"{entry.Key}: {entry.Value}{Environment.NewLine}");
        }
        diagReport.AppendLine();

        var sb = new StringBuilder();

        foreach (var entry in environmentInfo)
        {
            sb.AppendFormat($"{entry.Key}: {entry.Value}{Environment.NewLine}");
        }
        diagReport.Append(sb.ToString());
        diagReport.Append(Res.ClsErrorFiles).AppendLine();

        if (appFilePath != null)
        {
            var files = Directory.GetFiles(appFilePath);
            foreach (var (fileName, fileInfo, fileVer) in from file in files
                                                          let fileName = Path.GetFileName(file)
                                                          let fileInfo = new FileInfo(file)
                                                          let fileVer = FileVersionInfo.GetVersionInfo(file)
                                                          select (fileName, fileInfo, fileVer))
            {
                diagReport.AppendLine($"{fileInfo.LastWriteTime:O}\t{fileInfo.Length:#,###0}\t{fileVer.FileVersion}\t{fileName}\r{Environment.NewLine}");
            }
        }
        WriteLine(diagReport.ToString(), TraceEventType.Critical, exception);
    }

    static string ObfuscatePathNames(string path)
    {
        return path.Replace($@"\{Environment.UserName}\", @"\%USERNAME%\");
    }
}
