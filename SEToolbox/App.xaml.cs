using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SEToolbox.Interfaces;
using SEToolbox.Interop;
using SEToolbox.Services;
using SEToolbox.Support;
using SEToolbox.Views;
using WPFLocalizeExtension.Engine;
using Res = SEToolbox.Properties.Resources;
using SEConsts = SEToolbox.Interop.SpaceEngineersConsts;
namespace SEToolbox
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private CoreToolbox _toolboxApplication;
        private static readonly GlobalSettings settings = GlobalSettings.Default;
        private void OnStartup(object sender, StartupEventArgs e)
        { 
             bool appendLog = Enumerable.Contains(e.Args, "/appendlog");
             
            Log.Init("./log.txt", appendLog);
            
            Log.WriteLine("Starting.");
            SConsole.Init();
                
            // Initialize GlobalSettings
            Log.WriteLine("Loading settings.");
            BindingErrorTraceListener.SetTrace();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
            settings.Load();
            HandleReset();
            ClearBinCache();
            ConfigureLocalization();
            InitializeSplashScreen();
            CheckForUpdates(e.Args);
            ConfigureServices();
            DisableTextBoxSynchronization();
            InitializeToolboxApplication(e.Args);
        }

        private static void HandleReset()
        {
            if ((NativeMethods.GetKeyState(System.Windows.Forms.Keys.ShiftKey) & KeyStates.Down) == KeyStates.Down)
            {
                Log.WriteLine("Resetting global settings.");
                // Reset User Settings when Shift is held down during start up.
                settings.Reset();
                settings.PromptUser = true;
            }
        }

        private static void ClearBinCache()
        {
            // Clear app bin cache.
            Log.WriteLine("Clearing bin cache");
            string binCache = ToolboxUpdater.GetBinCachePath();
            if (Directory.Exists(binCache))
            {
                try
                {
                    Directory.Delete(binCache, true);
                }
                catch (Exception ex)
                {
                    Log.WriteLine($"SEToolbox: Could not delete binCache. {ex.Message}");
                }
            }
        }

        private static void ConfigureLocalization()
        {
            CultureInfo culture;
            try
            {
                Log.WriteLine($"Configuring localization");
                culture = !string.IsNullOrWhiteSpace(settings.LanguageCode)
                          ? CultureInfo.GetCultureInfoByIetfLanguageTag(settings.LanguageCode)
                          : CultureInfo.CurrentUICulture;
            }
            catch (Exception ex)
            {
                Log.WriteLine($"SEToolbox: Could not set language from GlobalSettings. {ex.Message}");
                culture = CultureInfo.CurrentUICulture;
            }

            LocalizeDictionary.Instance.SetCurrentThreadCulture = false;
            LocalizeDictionary.Instance.Culture = culture ?? throw new NullReferenceException("Culture cannot be null");
            Thread.CurrentThread.CurrentUICulture = culture;

            Log.WriteLine($"Language: {LocalizeDictionary.Instance.Culture.Name}");
        }

        private static void InitializeSplashScreen()
        {
            Log.WriteLine("Showing splash screen.");
            Splasher.Splash = new WindowSplashScreen();
            Splasher.ShowSplash();
        }

        private static void CheckForUpdates(string[] args)
        {
            Log.WriteLine($"Checking for updates.");
            //int version = SEConsts.GetToolboxVersion();
            string delimiter = "/" ?? "-";
            ApplicationRelease update = CodeRepositoryReleases.CheckForUpdates(GlobalSettings.GetAppVersion());
            if (args.Any(a => a.Equals($"{delimiter}U", StringComparison.OrdinalIgnoreCase)) && update != null)
            {
                var dialogResult = MessageBox.Show(
                            string.IsNullOrEmpty(update.Notes)
                          ? string.Format(Res.DialogNewVersionMessage, update.Version)
                          : string.Format(Res.DialogNewVersionNotesMessage, update.Version, update.Notes),
                            Res.DialogNewVersionTitle, MessageBoxButton.YesNo, MessageBoxImage.Information);


                Log.WriteLine($"Found update: {update.Version}");
                switch (dialogResult)
                {
                    case MessageBoxResult.Yes:
                        Log.WriteLine($"Opening release URL: {update.Link}");
                        Process.Start(update.Link);
                        settings.Save();
                        Current.Shutdown();
                        return;
                    case MessageBoxResult.No:
                        Log.WriteLine($"Ignoring update: {update.Version}");
                        settings.IgnoreUpdateVersion = update.Version.ToString();
                        break;
                }
            }
        }

        private static void ConfigureServices()
        {
            Log.WriteLine("Configuring ServiceLocator.");
            ServiceLocator.RegisterSingleton<IDialogService, DialogService>();
            ServiceLocator.Register<IOpenFileDialog, OpenFileDialogViewModel>();
            ServiceLocator.Register<ISaveFileDialog, SaveFileDialogViewModel>();
            ServiceLocator.Register<IColorDialog, ColorDialogViewModel>();
            ServiceLocator.Register<IFolderBrowserDialog, FolderBrowserDialogViewModel>();
        }

        private static void DisableTextBoxSynchronization()
        {
            FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty = false;
        }

        private void InitializeToolboxApplication(string[] args)
        {   
         
            _toolboxApplication = new CoreToolbox();
            string message = string.Empty;

            Log.WriteLine($"Initializing {nameof(CoreToolbox)}");
            switch (_toolboxApplication)
            {
                case CoreToolbox when _toolboxApplication.Init(args):
                    _toolboxApplication.Load(args);
                    Log.WriteLine($"{nameof(CoreToolbox)} started successfully.");
                    break;
                case CoreToolbox when _toolboxApplication == null && message.Contains("Could not start"):// args.Length == 0
                case CoreToolbox when !_toolboxApplication.Init(args) && message.Contains("Could not initialize"):
                case CoreToolbox when !_toolboxApplication.Load(args) && message.Contains("Could not load"):
                default:
                    Log.WriteLine($"SEToolbox: {message} {nameof(CoreToolbox)}. Aborting.");
                    Current.Shutdown();
                    break;   
            }
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            Log.WriteLine("Shutting down.");
            CoreToolbox.ExitApplication();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (sender == null)
            {
                Exception exception = e.Exception;
                while (exception != null)
                {
                    Debug.WriteLine(exception.Message);
                    exception = exception.InnerException ?? exception.GetBaseException();
                }
            }

            Log.WriteLine($"Unhandled exception occurred: {e.Exception.Message}");
            const int ClipbrdECannotOpenError = unchecked((int)0x800401D0);
            const int COMError = unchecked(-2147221040);

            if (e.Exception is COMException comException && comException?.ErrorCode is COMError or ClipbrdECannotOpenError)
            {
                try
                {
                    Clipboard.SetDataObject(new DataObject(DataFormats.Text, comException.Message), true);
                }
                catch (COMException ex) when (ex.HResult == ClipbrdECannotOpenError)
                {
                    // Ignore this exception
                }

                e.Handled = true;
                return;
            }
             Log.WriteLine(e.Exception);

            string message = e.Exception is ToolboxException ? e.Exception.Message : string.Format(Res.DialogUnhandledExceptionMessage, e.Exception.Message + $"{new StackTrace(e.Exception, true)}");

            Debug.WriteLine(message);
            
            MessageBox.Show(message, string.Format(Res.DialogUnhandledExceptionTitle, GlobalSettings.GetAppVersion()), MessageBoxButton.OK, MessageBoxImage.Error);

            TempFileUtil.Dispose();

            e.Handled = true;
            Current?.Shutdown();
        }
    }
}
