using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace SEToolbox.Support
{
    class Loader
    {
        public static Task task = null;

        public static async Task<string> ProgressDots()
        {   var r = Enumerable.Range(0, 3);
            int repeatCount = r.Count();
            var sb = new StringBuilder(repeatCount);
            var repeat = Enumerable.Repeat(".", repeatCount);
            if (task != null)
            {
                while (true)
                {
                    sb.Clear();
                    for (int i = 0; i < repeatCount; i++)
                    {
                        sb.Append(repeat.ElementAt(i));   
                    }
                    if (task.IsCompleted && task.Status == TaskStatus.RanToCompletion && Task.CurrentId == null)
                    {
                        sb.Append($" {MethodBase.GetCurrentMethod().Name} finished");
                        break;
                    }
                    await Task.Delay(500);
                }
            }
            return sb.ToString();

        }
        public static async Task WriteProgressDots()
        {
            List<Task> tasks = [];
            bool running = true;
            if (task != null)
            {
                var progressDotsTask = Task.Run(async () =>
                {
                    while (running)
                    {
                        Console.WriteLine($"Running {MethodBase.GetCurrentMethod().Name}{ProgressDots()}");
                    }

                    try
                    {
                        string result = await Task.Run(() => Console.ReadLine());
                        if (result != null)
                        {
                            var newResult = result.Substring(0, result.Length - 1);
                            Console.WriteLine(newResult);

                            await Task.Delay(500);

                            if (task.IsCompleted && task.Status == TaskStatus.RanToCompletion && Task.CurrentId == null)
                            {
                                running = false;
                                newResult = $"{MethodBase.GetCurrentMethod().Name} finished";
                                Console.WriteLine($"{newResult}");
                            }
                        }
                        else if (result == null)
                        {
                            Console.WriteLine($"No input received. Exiting {MethodBase.GetCurrentMethod().Name}");
                            throw new Exception("No input received." + Environment.NewLine + new StackTrace());
                        }
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred while reading input: {ex.Message}");
                        throw new Exception(ex.Message);
                    }
                });

                await Task.WhenAny(progressDotsTask, Task.Delay(TimeSpan.FromSeconds(0.5)));
                await progressDotsTask;
                Console.WriteLine($"finished {MethodBase.GetCurrentMethod().Name}");
                return;
            }
        }
    }
}


