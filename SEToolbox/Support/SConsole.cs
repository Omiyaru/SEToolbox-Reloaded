using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;


namespace SEToolbox.Support
{
    public class SConsole
    {
        #region Imports
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "AttachConsole")]  // [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AttachConsole(IntPtr processId);
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "AllocConsole")]
        private static extern bool AllocConsole();

        #endregion

      #region Properties
        private static readonly int ATTACH_PARENT_PROCESS = Process.GetCurrentProcess().Id;
        private static readonly Redirector redirector = new Redirector();
        private static readonly nint dwProcessId = Process.GetCurrentProcess().Id > 0 ? ATTACH_PARENT_PROCESS : 0;

        private static bool _isAttached = EnsureAttachment();

        #endregion

        #region Internal Methods    
      

        internal static string GetFileLink([CallerFilePath] string filePath = "")
        {
            if (File.Exists(filePath))
            {
                return $"file://{Path.GetFullPath(filePath).Replace('\\', '/')}";
            }
            return string.Empty;
        }
        
        internal static string GetDebugTrace(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string caller = "")
        {
            var frame = new StackFrame(1);
            string position = $"{frame.GetFileLineNumber()}, {frame.GetFileColumnNumber()}";
            string link = GetFileLink(filePath);
            string output = $"{link}: {Environment.NewLine}{message}{Environment.NewLine} Details: at {caller}, {position}{Environment.NewLine}{new StackTrace(frame)}";
            return output ?? string.Empty;
        }

        #region Methods

        [Conditional("DEBUG")]
        public static void WriteLine(string message, [CallerMemberName] string caller = "")
        {
            string debugTrace = GetDebugTrace(message);
            redirector.WriteLine(debugTrace);

        }
        #endregion

        #region Methods
     
        public static void WriteLine()
        {
            redirector.WriteLine();
        }
        public static void WriteLink([CallerFilePath] string filePath = "")
        {
            string fileLink = GetFileLink(filePath);
            redirector.WriteLine($"File link: {fileLink}");
        }

        public static void Write<T>(params T[] values)
        {
            if (values != null && values.Length > 0 && _isAttached)
                redirector.Write(string.Join("\t", values));
        }

        #endregion

        public static void Init()
        {
            _isAttached = true;
            if (redirector != null && _isAttached)
            {
                redirector.WriteLine(redirector);
            }
            Console.CancelKeyPress += (sender, e) => e.Cancel = true;
        }

        private static bool EnsureAttachment()
        {

            if (dwProcessId == 0 || dwProcessId != ATTACH_PARENT_PROCESS || dwProcessId != Process.GetCurrentProcess().Id)
            {
                return false;
            }
            AllocConsole();
            return AttachConsole(dwProcessId);
        }

        #endregion

        #region Redirector

        public sealed class Redirector : TextWriter
        {
            private static readonly StreamWriter _redirector = new(Console.OpenStandardOutput(), Encoding.UTF8) { AutoFlush = true };

            public override Encoding Encoding => Encoding.UTF8;
            public override void Close() => Flush();

            public override void Flush()
            {
                try
                {
                    var output = Output();
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        WriteLine(output);
                        Console.ResetColor();
                    }

                }
                catch (Exception ex)
                {
                    WriteLine($"An error occurred while flushing: {ex.Message}");
                }
            }

            public override void Write(string value) => Write(value);
            public void Write<T>(T value)
            {
                try
                {
                    Output($"{value}");
                }
                catch (Exception ex)
                {
                    Output($"An error occurred while writing: {ex.Message}");
                }
            }
            public override void WriteLine(string value) => WriteLine(value);
            public void WriteLine<T>(params T[] values)
            {
                var frame = new StackFrame(1);
                var output =  string.Empty;
                var valueContains = values.Any(v => v.ToString().Contains(typeof(Exception).Name) || v.GetType().IsSubclassOf(typeof(Exception)));
                var exeption = valueContains ? values.FirstOrDefault() as Exception : null;
                try
                {
                    var severity = Severity.GetSeverity(frame);
                    var traceEventType = severity.Item1;
                    var color = severity.Item2;
                    if (values != null && valueContains)
                        output += $"{traceEventType}: {exeption}{Environment.NewLine}{GetDebugTrace(exeption?.Message ?? string.Empty)}";
                    var consoleColor = Console.ForegroundColor;
                    if (color != default)
                    {
                        Console.ForegroundColor = color;
                    }
                    if (traceEventType != TraceEventType.Verbose)
                    {
                         Output(output, values);
                    }
                    else
                    {
                        Output(output);
                    }
                    Console.ResetColor();
                }

                catch (Exception ex)
                {
                   Output($"An error occurred while writing a line: {ex.Message}");
                }
            }
            public string Output() => $"{_redirector}";
            public object Output(params object[] values) => $"{values}";
            public string Output<T>( params T[] values) => $"{values}";
           

            #endregion
        }
    }

    #region Severity

    public static class Severity
    {
        private static readonly Dictionary<string, (TraceEventType, ConsoleColor)> _severityDictionary =
            new(StringComparer.OrdinalIgnoreCase)
            {
                    { "Critical",(TraceEventType.Critical, ConsoleColor.DarkRed) },
                    { "Error", (TraceEventType.Error, ConsoleColor.Red) },
                    { "Warning", (TraceEventType.Warning, ConsoleColor.Yellow) },
                    { "Info", (TraceEventType.Information, ConsoleColor.Green) },
                    { "Debug",(TraceEventType.Verbose, ConsoleColor.Gray) }
            };

        private static readonly Dictionary<string, (TraceEventType, ConsoleColor)> _severityCache =
                new(StringComparer.OrdinalIgnoreCase);

        public static (TraceEventType, ConsoleColor) GetSeverity(StackFrame frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame), "Frame cannot be null.");
            }

            var methodName = frame.GetMethod().Name;
            string declaringTypeName = string.Empty;
            if (_severityDictionary.TryGetValue(methodName, out _))
            {
                declaringTypeName = frame.GetMethod().DeclaringType.Name;
            }

            if (_severityDictionary.TryGetValue(declaringTypeName, out var result))

                _severityCache.Add(methodName, result);
            return result;

            throw new InvalidOperationException($"Severity for {methodName} cannot be found.");

        }
    }
}


#endregion
