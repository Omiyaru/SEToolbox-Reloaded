using System.ComponentModel;
using SEToolbox.Interfaces;
using SEToolbox.Models;

namespace SEToolbox.ViewModels
{
    public class BaseViewModel(BaseViewModel ownerViewModel) : INotifyPropertyChanged
    {
        #region Fields

        private BaseViewModel _ownerViewModel = ownerViewModel;

        #endregion
        #region Ctor

        #endregion

        #region Properties

        public virtual BaseViewModel OwnerViewModel
        {
            get => _ownerViewModel;
            set => SetProperty(ref _ownerViewModel, value, nameof(OwnerViewModel));
        }

        public IMainView MainViewModel
        {
            get => (IMainView)_ownerViewModel;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// Use the <see cref="nameof()"/> in conjunction with OnPropertyChanged.
        /// This will set the property name into a string during compile, which will be faster to execute then a runtime interpretation.
        /// </summary>
        /// <param name="propertyNames">The name of the property that changed.</param>
        protected void OnPropertyChanged(params string[] propertyNames)
        {
            if (PropertyChanged != null)
            {
                foreach (string propertyName in propertyNames)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
        BaseModel baseModel = new();
        
        public void SetProperty(bool? field, bool? value, params object[] parameters) => baseModel.SetProperty(field, value, parameters);
        
        public void SetProperty(ref bool? field, bool? value, string propertyName) => baseModel.SetProperty(ref field, value, propertyName);
        public void SetProperty<T>(T field, T value, params object[] parameters) => baseModel.SetProperty(field, value, parameters);
         public void SetProperty<T>(ref T field, T value, object obj,  params object[] parameters) => baseModel.SetProperty(ref field, value, obj, parameters);
        #endregion

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}