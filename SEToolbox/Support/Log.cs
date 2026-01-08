using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace SEToolbox.Support;

public static partial class Log
{
    static StreamWriter writer;

    public static void Init(string fileName, bool appendFile = false)
    {
        writer = new StreamWriter(fileName, appendFile);
    }


    public static void WriteLine<T>(params T[] objects)
    {
        var exception = objects.OfType<Exception>().FirstOrDefault();
        var message = objects.OfType<string>().FirstOrDefault();
        var traceEvent = objects.OfType<TraceEventType>().FirstOrDefault();
        if (writer == null || string.IsNullOrEmpty(message) || (!Events.Contains(traceEvent) && !Events.Any(item => traceEvent.HasFlag(item))))
        {
            return;
        }
        var now = DateTime.Now;
        var thread = Thread.CurrentThread;
        var threadStr = thread.Name ?? thread.ManagedThreadId.ToString();
        var outTime = $"{now:yyyy-MM-dd HH:mm:ss.fff}";
        var logStr = $"{outTime} {traceEvent,-5} [{threadStr}] - {message}";
        var errorMessage = exception?.ToString();
        var exStr = exception != null ? $"{outTime} {TraceEventType.Critical,-5} [{threadStr}] - {errorMessage}" : null;
        var text = new List<string> { logStr };
        writer.WriteLine(logStr);
        SConsole.WriteLine(logStr, traceEvent, exception);
        lock (writer)
        {
            if (exStr != null)
            {
                writer.WriteLine(exStr);
                SConsole.WriteLine(exStr);
                Debug.WriteLine(exStr);
            }
            if (text.Count >= 1 && text.All(item => !string.IsNullOrEmpty(item)))
            {
                writer.WriteLine(string.Join(Environment.NewLine, text));
                SConsole.WriteLine(text);
                Debug.WriteLine(string.Join(Environment.NewLine, text));
            }
            writer.Flush();
        }
       

        var set = GetEventQuery(message, traceEvent, exception);

        writer.WriteLine(set);
        SConsole.WriteLine(set);
        lock (writer)
        {
            writer.Flush();
        }
    }

    static readonly IQueryable<TraceEventType> EventsQueryable = new List<TraceEventType>().AsQueryable();
    public static object GetEventQuery(string message, TraceEventType traceEvent, Exception exception = null)
    {   
        var infoOrDebug = TraceEventType.Information | TraceEventType.Verbose;
        var eventQuery = EventsQueryable.Except([infoOrDebug]).FirstOrDefault();
        var set = traceEvent switch
        {
            TraceEventType e when e.HasFlag(eventQuery) => (message, traceEvent, exception),
            TraceEventType e when e.HasFlag(infoOrDebug) => (message, traceEvent, null),
            _ => (message, traceEvent, null)
        };
        return ((object message, TraceEventType traceEvent, Exception exception))set;
    }

    public static void WriteLine(params object[] objects) => WriteLine(objects.OfType<string>().ToArray());

    static readonly List<TraceEventType> Events =
    [
        TraceEventType.Critical,
        TraceEventType.Error,
        TraceEventType.Warning,
        TraceEventType.Verbose,
        TraceEventType.Information
    ];

    public static void Flush()
    {
        //flush Output
        lock (writer)
        {
            writer.Flush();
        }
    }
}
