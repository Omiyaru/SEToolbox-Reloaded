using Microsoft.VisualBasic;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using System.Text;

namespace SEToolbox.Support
{
    public class SConsole
    {
        #region Imports
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "AttachConsole")]
        private static extern bool AttachConsole(IntPtr processId);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "AllocConsole")]
        private static extern bool AllocConsole();
        #endregion

        #region Properties
        private static readonly nint ATTACH_PARENT_PROCESS = Process.GetCurrentProcess().Id;
        private static readonly Redirector redirector = new();
        private static bool _isAttached = EnsureAttachment();
        #endregion

        #region Init
        public SConsole()
        {
            Init(); 
        }
        public static void Init()
        {
            _isAttached = EnsureAttachment();
        }
        private static bool EnsureAttachment()
        {
            AllocConsole();
            return AttachConsole(ATTACH_PARENT_PROCESS);
        }
        #endregion

        #region Methods
        public static string ReadLine() => redirector.ReadLine();

        public static void WriteLine(params object[] values)
        {
            bool isDebugEvent = DebugEvent.GetDebugEvent(new StackFrame(1)).Item1 != TraceEventType.Information;

            if (_isAttached)
            {
                if (isDebugEvent)
                {
                    foreach (var value in values)
                    {
                        redirector.WriteLine(DebugEvent.GetDebugTrace(value as string));
                    }
                }
                else
                {
                    if (values?.Length > 0 || values is IList<object>)
                    {
                        redirector.WriteLine(string.Join(Environment.NewLine, values));
                    }
                }
            }
        }

        public static void Write<T>(params T[] values)
        {
            if (values?.Length > 0 && _isAttached)
            {
                redirector.Write(values);
            }
        }

        #endregion
    }
    #region Redirector
    public class Redirector : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        private readonly TextWriter _redirectorWriter = Console.Out;
        private readonly TextReader _redirectorReader = Console.In;

        public Redirector()
        {
            _redirectorWriter = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8);
            _redirectorReader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);

        }

        public override void Flush()
        {
            try
            {
                var output = WriteOutput();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    _redirectorWriter.WriteLine(output);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while flushing: {ex.Message}");
            }
        }

        public void Write<T>(params T[] values)
        {
            foreach (var value in values)
            {
                _redirectorWriter.Write(value?.ToString());
            }
        }

        public override void WriteLine() => WriteLine<object>(string.Empty);

        public void WriteLine<T>(params T[] values)
        {
            var exception = DebugEvent.CheckException(values);
            var debugEvent = DebugEvent.GetDebugEvent(new StackFrame(1));
            var message = values.OfType<string>().FirstOrDefault();
            var color = debugEvent.Item2;
            var output = string.Empty;
            try
            {
                if (!string.IsNullOrWhiteSpace(output))
                {
                    if (exception != null && debugEvent.Item1 != TraceEventType.Information)
                    {
                        Console.ForegroundColor = color;
                        output = $"{debugEvent.Item1}: {exception}{Environment.NewLine}{DebugEvent.GetDebugTrace(exception?.Message ?? message ?? string.Empty)}";
                        WriteLineOutput(DebugEvent.CheckException(values), output);

                        Console.ResetColor();
                    }
                    else if (exception == null)
                    {
                        output = $"{message}";
                        Console.ForegroundColor = color;
                        _redirectorWriter.Write(output);
                        Console.ResetColor();
                        WriteLineOutput(DebugEvent.CheckException(values));
                    }
                }
            }
            catch (Exception ex)
            {
                _redirectorWriter.WriteLine($"An error occurred while writing a line: {ex.Message}");
            }
        }

        public string ReadLine()
        {
            return _redirectorReader.ReadLine();
        }

        public string WriteOutput()
        {
            lock (_redirectorWriter)
            {
                _redirectorWriter.Flush();
            }
            return _redirectorWriter.ToString();
        ;
        }

        public void WriteLineOutput(params object[] values)
        {
            var output = string.Empty;

            foreach (var value in values)
            {
                output += value?.ToString();
            }

            output += Environment.NewLine;
            lock (_redirectorWriter)
            {
                _redirectorWriter.Flush();
            }
            _redirectorWriter.WriteLine(output);


        }
    }
    #endregion

    #region Debug Events
    public class DebugEvent
    {
        private static readonly Dictionary<string, (TraceEventType, ConsoleColor)> _eventCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, (TraceEventType, ConsoleColor)> EventDictionary = new(StringComparer.OrdinalIgnoreCase)
        {
                { "Critical", (TraceEventType.Critical, ConsoleColor.DarkRed)},
                { "Error", (TraceEventType.Error, ConsoleColor.Red) },
                { "Warning", (TraceEventType.Warning, ConsoleColor.Yellow) },
                { "Info", (TraceEventType.Information, ConsoleColor.Green) },
                { "Debug", (TraceEventType.Verbose, ConsoleColor.Gray) }
        };



        public DebugEvent()
        {
            foreach (var pair in EventDictionary)
            {
                _eventCache.Add(pair.Key, pair.Value);
            }
        }

        internal static string GetFileLink([CallerFilePath] string filePath = "")
        {
            var path = Path.GetFullPath(filePath).Replace('\\', '/');//.Replace(" ", "%20");
            return File.Exists(filePath) ? $"file://{path}" : string.Empty;
        }

        internal static string ObfuscatePathLink(string path)
        {
            var uri = new Uri(path);
            var filePath = uri.LocalPath;
            var fileLink = GetFileLink(filePath);
            var obfuscatedPath = filePath.Replace(@"\", @"%5C");
            return fileLink.Replace(filePath, obfuscatedPath);
        }


        public static (TraceEventType, ConsoleColor) GetDebugEvent(StackFrame frame)
        {
            if (frame == null || frame.GetMethod() == null)
            {
                throw new ArgumentNullException(nameof(frame), "Frame cannot be null.");
            }

            var methodName = frame.GetMethod().Name;
            return _eventCache.TryGetValue(methodName, out var result) ? result : default;
        }
        public static Exception CheckException(params object[] messages)
        {
            var firstString = messages.OfType<string>().FirstOrDefault();
            var firstException = messages.OfType<Exception>().FirstOrDefault();
            var containsException = messages.Any(m => m.ToString().Contains(typeof(Exception).Name) || messages.Any(m => m.ToString().Contains(typeof(Exception).FullName)));


            if (firstException != null && firstException is AggregateException aggregateException)
            {
                var innerExceptions = aggregateException.InnerExceptions.Select(e => e.ToString());
                messages = [.. messages, .. innerExceptions];
            }

            return firstException ?? (containsException ? new Exception(firstString) : null);
        }
        internal static string GetDebugTrace(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string caller = "")
        {
            try
            {
                var frame = new StackFrame(1, true);
                var fileName = frame.GetFileName() ?? string.Empty;
                var lineNumber = frame.GetFileLineNumber();
                var columnNumber = frame.GetFileColumnNumber();
                var position = $"{fileName}({lineNumber}, {columnNumber})";
                var link = GetFileLink(filePath);

                var trace = new StackTrace(frame);
                return $"{message} {Environment.NewLine} at {position}: {caller}{Environment.NewLine} {link}: {Environment.NewLine}{trace}{Environment.NewLine}";
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
    #endregion



