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

namespace SEToolbox
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private CoreToolbox _toolboxApplication;

        private void OnStartup(object sender, StartupEventArgs e)
        {
            bool appendLog = Enumerable.Contains(e.Args, "/appendlog");

            Log.Init("./log.txt", appendLog);
            Log.Info("Starting.");

            var settings = GlobalSettings.Default;

            Log.Info("Loading settings.");
            settings.Load();

            if ((NativeMethods.GetKeyState(System.Windows.Forms.Keys.ShiftKey) & KeyStates.Down) == KeyStates.Down)
            {
                Log.Info("Restting global settings.");

                // Reset User Settings when Shift is held down during start up.
                settings.Reset();
                settings.PromptUser = true;
            }
            TException.InitializeListeners();
            BindingErrorTraceListener.SetTrace();
            SConsole.Init();

            if (e?.Args.Length > 0)
                HandleReset();
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
                // Reset User Settings when Shift is held down during start up.
                GlobalSettings.Default.Reset();
                GlobalSettings.Default.PromptUser = true;

                // Clear app bin cache.
                string binCache = ToolboxUpdater.GetBinCachePath();
                if (Directory.Exists(binCache))
                {
                    try
                    {
                        Directory.Delete(binCache, true);
                    }
                    catch (Exception ex)
                    {
                        SConsole.WriteLine($"SEToolbox: Could not delete binCache. {ex.Message}");
                    }
                }
            }
        }

        private static void ConfigureLocalization()
        {

            LocalizeDictionary.Instance.SetCurrentThreadCulture = false;

            try
            {
                GlobalSettings.Default.Load();
            }
            catch (Exception ex)
            {
                SConsole.WriteLine($"SEToolbox: Could not load GlobalSettings.Default. {ex.Message}");
            }

            // Try to use user-saved language, else system
            CultureInfo culture;
            try
            {
                culture = !string.IsNullOrWhiteSpace(GlobalSettings.Default.LanguageCode)
                          ? CultureInfo.GetCultureInfoByIetfLanguageTag(GlobalSettings.Default.LanguageCode)
                          : CultureInfo.CurrentUICulture;
            }
            catch (Exception ex)
            {
                SConsole.WriteLine($"SEToolbox: Could not set language from GlobalSettings.Default.LanguageCode. {ex.Message}");
                culture = CultureInfo.CurrentUICulture;
            }

            LocalizeDictionary.Instance.Culture = culture ?? throw new NullReferenceException("Culture cannot be null");
            Thread.CurrentThread.CurrentUICulture = culture;
            Log.Info($"Language: {LocalizeDictionary.Instance.Culture.Name}");
        }

        private static void InitializeSplashScreen()
        {
            Splasher.Splash = new WindowSplashScreen();
            Splasher.ShowSplash();
            
        }

        private static void CheckForUpdates(string[] args)
        {
            Log.Info("Checking for updates.");
            string delimiter = "/"??"-";
            if (args.Length == 0 || (args.Length == 1 && args[0].Equals($"{delimiter}U", StringComparison.OrdinalIgnoreCase)))
            {
                ApplicationRelease update = CodeRepositoryReleases.CheckForUpdates(GlobalSettings.GetAppVersion());
                if (update != null)
                {
                    var dialogResult = MessageBox.Show(
                                string.IsNullOrEmpty(update.Notes)
                              ? string.Format(Res.DialogNewVersionMessage, update.Version)
                              : string.Format(Res.DialogNewVersionNotesMessage, update.Version, update.Notes),
                                Res.DialogNewVersionTitle, MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        
                        Process.Start(update.Link);// Opens release URL in browser
                        GlobalSettings.Default.Save();
                        Current.Shutdown();
                        return;
                    }

                    if (dialogResult == MessageBoxResult.No)
                    {
                        GlobalSettings.Default.IgnoreUpdateVersion = update.Version.ToString();
                    }
                }
            }
        }

        private static void ConfigureServices()
        {
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
         
               switch (_toolboxApplication)
               {
              case CoreToolbox when _toolboxApplication.Init(args):
                    _toolboxApplication.Load(args);
                    return;        
                case CoreToolbox when _toolboxApplication == null &&  message.Contains("Could not start"):// args.Length == 0
                case CoreToolbox when !_toolboxApplication.Init(args) && message.Contains("Could not initialize"):
                case CoreToolbox when !_toolboxApplication.Load(args) && message.Contains("Could not load"):
                default:
                        SConsole.WriteLine($"SEToolbox: {message} {nameof(CoreToolbox)}. Aborting.");
                        Current.Shutdown();

                    break;
            }  
            SConsole.WriteLine($"SEToolbox: {nameof(CoreToolbox)} started successfully.");
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            CoreToolbox.ExitApplication();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (sender == null)
            {
                Exception exception = e.Exception;
                while (exception != null)
                {
                    
                    SConsole.WriteLine(exception.Message);
                    exception = exception.InnerException;
                }
            }

            SConsole.WriteLine($"Unhandled exception occurred: {e.Exception.Message}");
            const int ClipbrdECannotOpenError = unchecked((int)0x800401D0);
            const int COMError = unchecked(-2147221040);

            if (e.Exception is COMException comException && comException != null && comException.ErrorCode == COMError && comException.ErrorCode == ClipbrdECannotOpenError)
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

            DiagnosticsLogging.LogException(e.Exception);

            string message = e.Exception is ToolboxException ? e.Exception.Message : string.Format(Res.DialogUnhandledExceptionMessage, e.Exception.Message + $"{new StackTrace(e.Exception, true)}");

            SConsole.WriteLine(message);
            MessageBox.Show(message, string.Format(Res.DialogUnhandledExceptionTitle, GlobalSettings.GetAppVersion()), MessageBoxButton.OK, MessageBoxImage.Error);

            TempFileUtil.Dispose();

            e.Handled = true;
            Current?.Shutdown();
        }
    }
}
