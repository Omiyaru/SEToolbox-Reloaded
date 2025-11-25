using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


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

        #region Constants
        private static int ATTACH_PARENT_PROCESS = 0;
        #endregion

        #region Debug

        [Conditional("DEBUG")]
        public static void WriteLine(string message, [CallerMemberName] string caller = "")
        {
            string debugTrace = GetDebugTrace(message);
            redirector.WriteLine(debugTrace);
            
        }

        #endregion

        #region Standard Methods
       
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

        public static void WriteLine<T>(T value)
        {
            redirector.WriteLine($"{value}");

            if (value != null && value is string && value.ToString().Contains($"{typeof(Exception).Name}") && _isAttached)
            {
                redirector.WriteLine($"{value}", new Exception());
            }
        }

        #endregion

        #region Internal Methods    
        private static readonly nint dwProcessId = Process.GetCurrentProcess().Id > 0 ? Process.GetCurrentProcess().Id : ATTACH_PARENT_PROCESS;

        private static bool _isAttached = EnsureAttachment();

        public static string GetFileLink([CallerFilePath] string filePath = "")
        {
            if (File.Exists(filePath))
            {
                return $"file://{Path.GetFullPath(filePath).Replace('\\', '/')}";
            }
            return string.Empty;
        }

        private static string GetDebugTrace(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string caller = "")
        {
            var frame = new StackFrame(1);
            string position = $"{frame.GetFileLineNumber()}, {frame.GetFileColumnNumber()}";
            string link = GetFileLink(filePath);
            string output = $"{link}: {Environment.NewLine}{message}{Environment.NewLine} Details: at {caller}, {position}{Environment.NewLine}{new StackTrace(frame)}";
            return output ?? string.Empty;
        }

        private static readonly Redirector redirector = new();

        public static void Init()
        {
            _isAttached = true;
            if (redirector != null && _isAttached)
            {
                Console.SetOut(redirector);
            }
            Console.CancelKeyPress += (sender, e) => e.Cancel = true;
        }
        
        public static void Output()
        {
            string output = redirector.Output();

            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(output);
                Console.ResetColor();
            }
            redirector.WriteLine(output);
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
    }

    #endregion

    #region Redirector

    public sealed class Redirector : TextWriter
    {
        private static StringBuilder _redirector = new();

        public override Encoding Encoding => Encoding.UTF8;
        public override void Close() => Flush();

        public override void Flush()
        {
            try
            {
                var output = Output();
                Console.Clear();
                Write(output);
            }
            catch (Exception ex)
            {
                WriteLine($"An error occurred while flushing: {ex.Message}");
                  
            }
        }

        public void Write<T>(T value)
        {
            try
            {
                Write($"{value}");
            }
            catch (Exception ex)
            {
                Write($"An error occurred while writing: {ex.Message}");
            }
        }

        public void WriteLine<T>(T value, Exception exception)
        {
            var frame = new StackFrame(1);
            var output = $"{value}{Environment.NewLine}";
            var color = default(ConsoleColor);
            try
            {   
                if (exception != null)
                {    
                var severity = Severity.GetSeverity(frame);
                color = severity(frame).Item2;
                if(value != null && value is string && value.ToString().Contains(typeof(Exception).Name))
                output += $"({frame.GetFileLineNumber()}, {frame.GetFileColumnNumber()}){Environment.NewLine}{new StackTrace(frame)}";
                var consoleColor = Console.ForegroundColor;
                if (color != default)
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(output);
                }
                else
                {
                    Console.WriteLine(output);
                }
                Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while writing a line: {ex.Message}");
            }
        }

        public string Output() => $"{_redirector}";

        #endregion

        #region Severity

        public static class Severity
        {
            private static readonly Dictionary<string, Func<StackFrame,(TraceEventType, ConsoleColor) >> _severityDictionary =
                new(StringComparer.OrdinalIgnoreCase)
                {   
                    { "Critical", f => (TraceEventType.Critical, ConsoleColor.DarkRed)},
                    { "Error", f => (TraceEventType.Error, ConsoleColor.Red) },
                    { "Warning", f => (TraceEventType.Warning, ConsoleColor.Yellow) },
                    { "Info", f => (TraceEventType.Information, ConsoleColor.Green) },
                    { "Debug", f => (TraceEventType.Verbose, ConsoleColor.Gray) }
                };
            
            private static readonly Dictionary<string, (TraceEventType, ConsoleColor)> _severityCache =
                new(StringComparer.OrdinalIgnoreCase);

            public static Func<StackFrame, (TraceEventType, ConsoleColor)> GetSeverity(StackFrame frame)
            {
                if (frame == null)
                {
                    throw new ArgumentNullException(nameof(frame), "Frame cannot be null.");
                }

                var methodName = frame.GetMethod().Name;
                string declaringTypeName = string.Empty;
                if (_severityCache.TryGetValue(methodName, out _))
                {
                    declaringTypeName = frame.GetMethod().DeclaringType.Name;
                }

                if (_severityDictionary.TryGetValue(declaringTypeName, out Func<StackFrame,(TraceEventType, ConsoleColor)> result))

                    _severityCache.Add(methodName, result(frame));
                return result;

                throw new InvalidOperationException($"Severity for {methodName} cannot be found.");

            }
        }
    }
}


    #endregion
