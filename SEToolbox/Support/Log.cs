using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SEToolbox.Support;

public static partial class Log
{
    static StreamWriter writer;

    public static void Init(string fileName, bool appendFile = false)
    {
        writer = new StreamWriter(fileName, appendFile);
    }


    static void WriteLine(string message, TraceEventType traceEvent, Exception exception = null)
    {
        var thread = Thread.CurrentThread;
        var threadStr = thread.Name ?? thread.ManagedThreadId.ToString();
        var logStr = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {traceEvent,-5} [{threadStr}] - {message}";
        var exStr = exception == null ? null : $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {TraceEventType.Critical,-5} [{threadStr}] - {exception}";

        lock (writer)
        {
            writer.WriteLine(logStr);

            if (exStr != null)
                writer.WriteLine(exStr);

            writer.Flush();
        }
    }

    public static void Debug(string message)
    {
        WriteLine(message, TraceEventType.Verbose);
    }

    public static void Info(string message)
    {
        WriteLine(message, TraceEventType.Information);
    }

    public static void Warning(string message)
    {
        WriteLine(message, TraceEventType.Warning);
    }

    public static void Warning(string message, Exception exception)
    {
        WriteLine(message, TraceEventType.Warning, exception);
    }

    public static void Error(string message)
    {
        WriteLine(message, TraceEventType.Error);
    }

    public static void Error(string message, Exception exception)
    {
        WriteLine(message, TraceEventType.Error, exception);
    }
}
