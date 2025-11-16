using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static SEToolbox.Support.Conditional;


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
        private static readonly int ATTACH_PARENT_PROCESS = 0;
        #endregion

        #region Debug

        [Conditional("DEBUG")]
        public static void WriteLine(string message, [CallerMemberName] string caller = "")
        {
            string debugTrace = GetDebugTrace(message);
            Console.WriteLine(debugTrace);
            Debug.WriteLine(debugTrace);
        }

        #endregion

        #region Standard Methods
        public static void WriteLine() => redirector.WriteLine();


        public static void WriteLink([CallerFilePath] string filePath = "")
        {
            string fileLink = GetFileLink(filePath);
            redirector.WriteLine($"File link: {fileLink}");
        }

        public static void Write<T>(params T[] values)
        {
            if (values != null && values.Length > 0)
            {
                redirector.Write(string.Join("\t", values));
            }
        }

        public static void WriteLine<T>(T value)
        {
            redirector.WriteLine($"{value}");
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
            redirector.Append(output);
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
        private static readonly StringBuilder _redirector = new();

        public override Encoding Encoding => Encoding.UTF8;
        public override void Close() => Flush();
        public StringBuilder Append<T>(T value)
        {
            return value switch
            {
                object when value is string => _redirector.Append(value as string),
                object when value is char[] => _redirector.Append(value as char[]),
                object when value is ValueType => _redirector.Append(value as ValueType),
                _ => _redirector,
            };
        }
        public StringBuilder AppendLine<T>(T value) => _redirector.AppendLine(value as string);

        public StringBuilder Append<T>(T value, int startIndex = 0, int count = int.MaxValue)
        {

            return value switch
            {
                object when value is string => _redirector.Append(value as string, startIndex, count),
                object when value is char[] => _redirector.Append(value as char[], startIndex, count),
                object when value is ValueType valueType => _redirector.Append(valueType),
                _ => _redirector,
            };
        }

        public StringBuilder Insert<T>(int index, T value = default, int? startIndex = null, int? count = null)
        {
            return value switch
            {
                object when value is string stringValue => startIndex.HasValue && count.HasValue?_redirector.Insert(index, stringValue, count.Value) : _redirector.Insert(index, stringValue),
                object when value is char[] charArray => startIndex.HasValue && count.HasValue ? _redirector.Insert(index, charArray, startIndex.Value, count.Value) : _redirector.Insert(index, charArray),
                object when value is ValueType valueType => _redirector.Insert(index, valueType),
                _ => _redirector,
            };
        }
        public StringBuilder Replace(object oldValue, object newValue, int? startIndex = null, int? count = null)
        {

            if ((bool)Condition(typeof(string) ?? typeof(char), oldValue.GetType(), newValue.GetType()))
            {
                return (oldValue, newValue) switch
                {
                    object when (bool)Condition(typeof(string), oldValue.GetType(), newValue.GetType()) => _redirector.Replace(oldValue as string, newValue as string),
                    object when oldValue is char oldChar && newValue is char newChar => _redirector.Replace(oldChar, newChar),
                    object when (bool)Condition(typeof(string), oldValue, newValue.GetType(), startIndex.HasValue, count.HasValue) => _redirector.Replace(oldValue as string, newValue as string, startIndex.Value, count.Value),
                    object when oldValue is char oldChar && newValue is char newChar && startIndex.HasValue && count.HasValue => _redirector.Replace(oldChar, newChar, startIndex.Value, count.Value),
                    _ => _redirector,
                };
            }
            return _redirector;
        }

        public static StringBuilder Replace<T>(T oldValue, T newValue, int? startIndex = null, int? count = null)
        {
            return (oldValue, newValue) switch
            {
                T when oldValue is string oldString && newValue is string newString => _redirector.Replace(oldString, newString),
                T when oldValue is char oldChar && newValue is char newChar => _redirector.Replace(oldChar, newChar),
                T when oldValue is string oldString && newValue is string newString && startIndex.HasValue && count.HasValue => _redirector.Replace(oldString, newString, startIndex.Value, count.Value),
                T when oldValue is char oldChar && newValue is char newChar && startIndex.HasValue && count.HasValue => _redirector.Replace(oldChar, newChar, startIndex.Value, count.Value),
                _ => _redirector,
            };
        }

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
                Debug.WriteLine($"An error occurred while flushing: {ex.Message}");
            }
        }

        public void Write<T>(T value)
        {
            try
            {
                Append(value);
            }
            catch (Exception ex)
            {
                Console.Write($"An error occurred while writing: {ex.Message}");
                Debug.Write($"An error occurred while writing: {ex.Message}");
            }
        }

        public void WriteLine<T>(T value)
        {
            var frame = new StackFrame(1);
            var output = $"{value}{Environment.NewLine}";
            var color = default(ConsoleColor);
            try
            {
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
            private static readonly Dictionary<string, (TraceEventType, ConsoleColor)> _severityDictionary =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    { "Critical", (TraceEventType.Critical, ConsoleColor.DarkRed) },
                    { "Error", (TraceEventType.Error, ConsoleColor.Red) },
                    { "Warning", (TraceEventType.Warning, ConsoleColor.Yellow) },
                    { "Info", (TraceEventType.Information, ConsoleColor.Gray) },
                    { "Debug", (TraceEventType.Verbose, ConsoleColor.DarkGray) }
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
                if (_severityCache.TryGetValue(methodName, out _))
                {
                    declaringTypeName = frame.GetMethod().DeclaringType.Name;
                }

                if (_severityDictionary.TryGetValue(declaringTypeName, out (TraceEventType, ConsoleColor) result))

                    _severityCache.Add(methodName, result);
                return result;
                throw new InvalidOperationException($"Severity for {methodName} cannot be found.");

            }
        }
    }
}


#endregion
