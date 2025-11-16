//    using System;
//     using System.Collections.ObjectModel;
//     using System.ComponentModel;
//     using System.Diagnostics.Contracts;
//     using System.Windows.Input;

//     using SEToolbox.Interfaces;
//     using SEToolbox.Models;
//     using SEToolbox.Services;
// using VRageMath;

//     namespace SEToolbox.ViewModels
// {


//     public class GroupMoveToNewPositionViewModel : BaseViewModel
//     {
//         #region Fields
//         #region Fields

//         private readonly IDialogService _dialogService;
//         private readonly GroupMoveModel _dataModel;
//         private Vector3D _centerPosition;

//         #endregion


//         #endregion

//         #region Constructors
//         #region Command Properties

//         public GroupMoveToNewPositionViewModel(BaseViewModel parentViewModel, GroupMoveModel dataModel)
//      : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>())
//         {
//         }

//         public GroupMoveToNewPositionViewModel(BaseViewModel parentViewModel, GroupMoveModel dataModel, IDialogService dialogService)
//             : base(parentViewModel)
//         {
//             Contract.Requires(dialogService != null);
//             _dialogService = dialogService;
//             _dataModel = dataModel;

//             // Will bubble property change events from the Model to the ViewModel.
//             _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
//         }

//         public ICommand ApplyCommand
//         {
//             get { return new DelegateCommand(ApplyExecuted, ApplyCanExecute); }
//         }

//         public ICommand CancelCommand
//         {
//             get { return new DelegateCommand(CancelExecuted, CancelCanExecute); }
//         }

//         #endregion

//         #endregion

//         #region Properties

//         /// <summary>
//         /// Gets or sets the DialogResult of the View.  If True or False is passed, this initiates the Close().
//         /// </summary>
//         public bool CloseResult
//         {
//             get;

//             set
//             {
//                 field = value;
//                 OnPropertyChanged(nameof(CloseResult));
//             }
//         }

//         /// <summary>
//         /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
//         /// </summary>
//         public bool IsBusy
//         {
//             get => _dataModel.IsBusy;

//             set => _dataModel.IsBusy = value;
//         }
//         public ObservableCollection<GroupMoveItemModel> Selections
//         {
//             get => _dataModel.Selections;

//             set => _dataModel.Selections = value;
//         }




//         public Vector3D CenterPosition
//         {
//             get => _centerPosition;
//             set
//             {
//                 if (value != _centerPosition)
//                 {
//                     _centerPosition = value;
//                     OnPropertyChanged(nameof(CenterPosition));
//                 }
//             }
//         }
//         #endregion

//     }
// }