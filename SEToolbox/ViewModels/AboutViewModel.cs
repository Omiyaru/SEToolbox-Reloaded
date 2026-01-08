using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

using SEToolbox.Services;
using SEToolbox.Support;

namespace SEToolbox.ViewModels
{
    public class AboutViewModel(BaseViewModel parentViewModel) : BaseViewModel(parentViewModel)
    {
        #region Fields

        private bool? _closeResult;

        #endregion

        #region Constructors

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the DialogResult of the View.  If True or False is passed, this initiates the Close().
        /// </summary>
        public bool? CloseResult
        {
            get => _closeResult;
            set => SetValue(ref _closeResult, value, nameof(CloseResult));
        }

        public ICommand OpenLinkCommand
        {
            get => new DelegateCommand(OpenLinkExecuted, OpenLinkCanExecute);
        }

        public ICommand CloseCommand
        { 
             get => new DelegateCommand(CloseExecuted, CloseCanExecute);
        }

        public static string Company
        {
            get
            {
                var company = Assembly.GetExecutingAssembly()
                     .GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)
                     .OfType<AssemblyCompanyAttribute>()
                     .FirstOrDefault();
                return company.Company;
            }
        }

        public Version Version
        {
            get => GlobalSettings.GetAppVersion();
        }
   
        public string HomepageUrl
        {
            get => Properties.Resources.GlobalHomepageUrl;
        }

        #endregion

        #region Methods

        public bool CloseCanExecute()
        {
            return true;
        }

        public void CloseExecuted()
        {
            CloseResult = false;
        }

        public bool OpenLinkCanExecute()
        {
            return true;
        }

        public void OpenLinkExecuted()
        {
            Process.Start(HomepageUrl);
        }

        #endregion
    }
}
