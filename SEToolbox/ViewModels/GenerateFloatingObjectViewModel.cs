using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Sandbox.Common.ObjectBuilders.Definitions;
using SEToolbox.Interop;
using SEToolbox.Models;
using SEToolbox.Services;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using MOBTypeIds = SEToolbox.Interop.SpaceEngineersTypes.MOBTypeIds;
using IDType = VRage.MyEntityIdentifier.ID_OBJECT_TYPE;
using VRage.Network;

namespace SEToolbox.ViewModels
{
    public class GenerateFloatingObjectViewModel : BaseViewModel
    {
        #region Fields

        private readonly GenerateFloatingObjectModel _dataModel;
        private bool? _closeResult;
        private bool _isBusy;

        #endregion

        #region Ctor

        public GenerateFloatingObjectViewModel(BaseViewModel parentViewModel, GenerateFloatingObjectModel dataModel)
            : base(parentViewModel)
        {

            _dataModel = dataModel;
            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand CreateCommand
        {
            get => new DelegateCommand(CreateExecuted, CreateCanExecute);
        }

        public ICommand CancelCommand
        {
            get => new DelegateCommand(CancelExecuted, CancelCanExecute);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the DialogResult of the View.  If True or False is passed, this initiates the Close().
        /// </summary>
        public bool? CloseResult
        {
            get => _closeResult;
            set => SetProperty(ref _closeResult, value, nameof(CloseResult));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value, nameof(IsBusy), () =>
                {
                    if (_isBusy)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                });
        }

        public ObservableCollection<ComponentItemModel> StockItemList
        {
            get => _dataModel.StockItemList;
        }

        public ComponentItemModel StockItem
        {
            get => _dataModel.StockItem;
            set => _dataModel.StockItem = value;
        }

        public bool IsValidItemToImport
        {
            get => _dataModel.IsValidItemToImport;
            set => _dataModel.IsValidItemToImport = value;
        }

        public double? Volume
        {
            get => _dataModel.Volume;
            set => _dataModel.Volume = value;
        }

        public double? Mass
        {
            get => _dataModel.Mass;
            set => _dataModel.Mass = value;
        }

        public int? Units
        {
            get => _dataModel.Units;
            set => _dataModel.Units = value;
        }

        public decimal? DecimalUnits
        {
            get => _dataModel.DecimalUnits;
            set => _dataModel.DecimalUnits = value;
        }

        public bool IsDecimal
        {
            get => _dataModel.IsDecimal;
            set => _dataModel.IsDecimal = value;
        }

        public bool IsInt
        {
            get => _dataModel.IsInt;
            set => _dataModel.IsInt = value;
        }

        public bool IsUnique
        {
            get => _dataModel.IsUnique;
            set => _dataModel.IsUnique = value;
        }

        public int Multiplier
        {
            get => _dataModel.Multiplier;
            set => _dataModel.Multiplier = value;
        }

        public float MaxFloatingObjects
        {
            get => _dataModel.MaxFloatingObjects;
            set => _dataModel.MaxFloatingObjects = value;
        }

        #endregion

        #region Methods

        #region Commands

        public bool CreateCanExecute()
        {
            return StockItem != null &&
                (IsUnique ||
                (IsInt && Units.HasValue && Units.Value > 0) ||
                (IsDecimal && DecimalUnits.HasValue && DecimalUnits.Value > 0));
        }

        public void CreateExecuted()
        {
            CloseResult = true;
        }

        public bool CancelCanExecute()
        {
            return true;
        }

        public void CancelExecuted()
        {
            CloseResult = false;
        }

        #endregion

        #region BuildEntity

        public MyObjectBuilder_EntityBase[] BuildEntities()
        {
            MyObjectBuilder_FloatingObject entity = new()
            {
                EntityId = SpaceEngineersApi.GenerateEntityId(IDType.ENTITY),
                PersistentFlags = MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene,
                Item = new MyObjectBuilder_InventoryItem { ItemId = 0 },
            };
            entity.Item.Amount = IsDecimal && DecimalUnits.HasValue ? DecimalUnits.Value.ToFixedPoint() :
                                     IsInt && Units.HasValue ? Units.Value.ToFixedPoint() :
                                IsUnique ? GenerateFloatingObjectModel.UniqueUnits.ToFixedPoint() : 1;

            IsValidItemToImport = true;
            entity.Item.PhysicalContent = SpaceEngineersResources.CreateNewObject<MyObjectBuilder_PhysicalObject>(StockItem.TypeId, StockItem.SubtypeId);
           
            /// <summary> See <see cref="Interop.SpaceEngineersTypeIds"/> for a list of all possible types.</summary
            switch (StockItem.TypeId)
            {
                case var t when t == MOBTypeIds.Component:
                case MyObjectBuilderType when t == MOBTypeIds.AmmoMagazine:
                case MyObjectBuilderType when t == MOBTypeIds.Ingot:
                case MyObjectBuilderType when t == MOBTypeIds.Ore:
                    break;
                case var t when t == MOBTypeIds.PhysicalGunObject:
                    CreateGunEntity(entity);
                    break;
                case var t when t == MOBTypeIds.GasContainerObject:
                    var gasContainer = entity.Item.PhysicalContent as MyObjectBuilder_GasContainerObject;
                    _ = (gasContainer?.GasLevel = 1f);
                    break;
                case var t when t == MOBTypeIds.OxygenContainerObject:
                    break;
                default:
                    // As yet uncatered for items which may be new.
                    IsValidItemToImport = false;
                break;
            }

            // Figure out where the Character is facing, and plant the new construct 1m out in front, and 1m up from the feet, facing the Character.
            Vector3D vectorFwd = _dataModel.CharacterPosition.Forward.ToVector3D();
            Vector3D vectorUp = _dataModel.CharacterPosition.Up.ToVector3D();
            vectorFwd.Normalize();
            vectorUp.Normalize();
            Vector3D vector = Vector3D.Multiply(vectorFwd, 1.0f) + Vector3D.Multiply(vectorUp, 1.0f);

            entity.PositionAndOrientation = new MyPositionAndOrientation
            {
                Position = Point3D.Add(_dataModel.CharacterPosition.Position.ToPoint3D(), vector).ToVector3D(),
                Forward = _dataModel.CharacterPosition.Forward,
                Up = _dataModel.CharacterPosition.Up
            };

            List<MyObjectBuilder_EntityBase> entities = [];

            for (int i = 0; i < Multiplier; i++)
            {
                MyObjectBuilder_FloatingObject newEntity = (MyObjectBuilder_FloatingObject)entity.Clone();
                newEntity.EntityId = SpaceEngineersApi.GenerateEntityId(IDType.ENTITY);
                if (StockItem.TypeId == MOBTypeIds.PhysicalGunObject)
                {
                    // Only required for pre-generating the Entity id for a gun that has been handled.
                    ((MyObjectBuilder_PhysicalGunObject)newEntity.Item.PhysicalContent).GunEntity.EntityId = SpaceEngineersApi.GenerateEntityId();
                }
                entities.Add(newEntity);
            }

            return [.. entities];
        }

        // GunEntity makes each 'GunObject' unique through EntityId, so no stacking,
        // and no ownership required. Only need to pre-generate EntityId for handled guns.
        // This is a hack approach for enums like AngleGrinderItem
        void CreateGunEntity(MyObjectBuilder_FloatingObject entity)
        {

            string enumName = StockItem.SubtypeId.Substring(0, StockItem.SubtypeId.Length - 4);
            if (Enum.TryParse(enumName, out MyObjectBuilderType itemEnum))
            {
                var gunEntity = SpaceEngineersResources.CreateNewObject<MyObjectBuilder_EntityBase>(itemEnum, StockItem.SubtypeId);
                {
                    gunEntity.EntityId = SpaceEngineersApi.GenerateEntityId();
                    gunEntity.PersistentFlags = MyPersistentEntityFlags2.None;
                    ((MyObjectBuilder_PhysicalGunObject)entity.Item.PhysicalContent).GunEntity = gunEntity;
                }
            }
            #endregion

        }

        #endregion
    }
}
