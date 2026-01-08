using System.Diagnostics;

namespace SEToolbox.Support
{
    public static partial class DiagnosticsLogging
    {
        private const string EventLogName = "Application";
        private const string EventSourceName = "SEToolbox.exe";

        #region CreateLog

        public static bool CreateLog()
        {
            return CreateLog(EventSourceName, EventLogName);
        }

        public static bool CreateLog(string source, string log)
        {
            try
            {
                if (!EventLog.SourceExists(source))
                {
                    EventLog.CreateEventSource(source, log);
                    return true;
                }

                // Log already exists, means its okay to start using it.
               // Log.WriteLine("Log source exists, proceeding.");

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region RemoveLog

        public static bool RemoveLog()
        {
            return RemoveLog(EventSourceName);
        }

        public static bool RemoveLog(string source)
        {
            try
            {
                if (EventLog.SourceExists(source))
                {
                    EventLog.DeleteEventSource(source);
                }

                // Log has been remove, or already removed.
                Debug.WriteLine("Log source removed or absent.");

                return true;
            }
            catch
            {
                // Could not access log to remove it.
                Debug.WriteLine("Failed to access the log source for removal.");

                return false;
            }
        }
         public static void LogWarning(string message) => EventLog.WriteEntry(EventSourceName, message, EventLogEntryType.Warning);

        #endregion

        #region LoggingSourceExists

        public static bool LoggingSourceExists()
        {
            try
            {
                return EventLog.SourceExists(EventSourceName);
            }
            catch
            {
                return false;
            }
        }
       
        #endregion
    }
}
