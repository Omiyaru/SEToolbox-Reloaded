using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SEToolbox.Support
{
    class ProgressDots
    {
        public static async Task WriteProgressDots()
        {//for long running tasks
            const int repeatCount = 3;
            string progressDots = ".";
            while (progressDots.Length < repeatCount && true)
            {
                progressDots = string.Concat(Enumerable.Repeat(progressDots, repeatCount));
            }

         
            var progressDotsTask = Task.Run( async () =>
            {
                while (true)
                {
                    
                    SConsole.Write($"running {MethodBase.GetCurrentMethod().Name}{progressDots}");
                    await Task.Delay(500);
                    progressDots += ".";
                }
            });

           
             await Task.WhenAny(progressDotsTask, Task.Delay(TimeSpan.FromSeconds(10)));
             await progressDotsTask;
            SConsole.WriteLine($"finished {MethodBase.GetCurrentMethod().Name}");
            return;
        }

    }
}