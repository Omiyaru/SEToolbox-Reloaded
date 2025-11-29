using System;
using System.Diagnostics;

namespace SEToolbox.Support;

partial class Log
{
    public static void Critical(string message, Exception exception)
    {
        WriteLine(message, TraceEventType.Critical, exception);
    }
}
