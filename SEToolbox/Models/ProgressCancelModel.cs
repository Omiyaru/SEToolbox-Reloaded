using System;
using System.Diagnostics;
using System.Timers;

namespace SEToolbox.Models
{
    public class ProgressCancelModel : BaseModel, IDisposable
    {
        #region Fields

        private string _title;

        private string _subTitle;

        private string _dialogText;

        private double _progress;

        private double _maximumProgress;

        private TimeSpan? _estimatedTimeLeft;

        private Stopwatch _elapsedTimer;

        private readonly Stopwatch _progressTimer;

        private Timer _updateTimer;

        #endregion

        public ProgressCancelModel()
        {
            _progressTimer = new Stopwatch();
        }

        ~ProgressCancelModel()
        {
            Dispose(false);
        }

        #region Properties

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, nameof(Title));
        }

        public string SubTitle
        {
            get => _subTitle;
            set => SetProperty(ref _subTitle, nameof(SubTitle));
        }

        public string DialogText
        {
            get => _dialogText;
            set => SetProperty(ref _dialogText, nameof(DialogText));
        }

        public double Progress
        {
            get => _progress;

            set 
            {
              
            
                    SetProperty(ref _progress, nameof(Progress), ()=>
                    {
                        if (_progressTimer.IsRunning == false || _progressTimer.ElapsedMilliseconds > 100 && _progress == value)
                            System.Windows.Forms.Application.DoEvents();
                    _progressTimer.Restart();});
                    }
            }
   

        public double MaximumProgress
        {
            get => _maximumProgress;
            set => SetProperty(ref _maximumProgress, nameof(MaximumProgress));
        }

        public TimeSpan? EstimatedTimeLeft
        {
            get => _estimatedTimeLeft;
            set => SetProperty(ref _estimatedTimeLeft, nameof(EstimatedTimeLeft)); 
        }

        #endregion

        #region Methods

        public void ResetProgress(double initial, double maximumProgress)
        {
            MaximumProgress = maximumProgress;
            Progress = initial;
            _elapsedTimer = new Stopwatch();

            _updateTimer = new Timer(1000);
            _updateTimer.Elapsed += delegate
            {
                TimeSpan elapsed = _elapsedTimer.Elapsed;
                TimeSpan estimate = elapsed;

                if (Progress > 0)
                    estimate = new TimeSpan((long)(elapsed.Ticks / (Progress / MaximumProgress)));

                EstimatedTimeLeft = estimate - elapsed;
            };

            _elapsedTimer.Restart();
            _updateTimer.Start();

            System.Windows.Forms.Application.DoEvents();
        }

        public void IncrementProgress()
        {
            Progress++;
        }

        public void ClearProgress()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            _elapsedTimer.Stop();
            _elapsedTimer.Reset();
            Progress = 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_updateTimer != null)
                {
                    _updateTimer.Stop();
                    _updateTimer.Dispose();
                }
                _progressTimer?.Stop();
            }
        }

        #endregion
    }
}
