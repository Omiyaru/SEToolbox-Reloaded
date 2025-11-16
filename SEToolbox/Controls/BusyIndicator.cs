using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SEToolbox.Controls
{    public enum Indicator
        {
            ProgressBar,
            Spinner
            //Percentage
        }
    public class BusyIndicator : Control
    {
        private Storyboard _spinnerAnimation;

        static BusyIndicator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyIndicator),
                new FrameworkPropertyMetadata(typeof(BusyIndicator)));
        }

        #region Dependency Properties

        public Indicator Mode
        {
            get => (Indicator)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(
                nameof(IsBusy),
                typeof(bool),
                typeof(BusyIndicator),
                new PropertyMetadata(false));

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(
                nameof(Mode),
                typeof(Indicator),
                typeof(BusyIndicator),
                new PropertyMetadata (Indicator.Spinner));

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _spinnerAnimation = GetTemplateChild("SpinnerAnimation") as Storyboard;
        }

        #region Public Methods

        public void Start() => Start(Mode);

        public void Start(Indicator mode)
        {
            Mode = mode;
            IsBusy = true;
            Resume();
        }

        public void Stop()
        {
            IsBusy = false;
            StopAnimation();
        }

        public void Pause()
        {
            if (_spinnerAnimation != null && Mode == Indicator.Spinner)
            {
                _spinnerAnimation.Pause(this);
            }
        }

        public void Resume()
        {
            if (_spinnerAnimation != null && Mode == Indicator.Spinner)
            {
                _spinnerAnimation.Begin(this, true); // Ensure animation starts if not already running
                _spinnerAnimation.Resume(this);
            }
        }

        private void StopAnimation()
        {
            _spinnerAnimation?.Stop(this);
        }

        #endregion
    }
}
