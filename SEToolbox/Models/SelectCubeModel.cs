using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SEToolbox.Interop;
using SEToolbox.Support;
using VRage.Game;
using VRage.ObjectBuilders;

namespace SEToolbox.Models
{
    public class SelectCubeModel : BaseModel
    {
        #region Fields

        private ObservableCollection<ComponentItemModel> _cubeList;
        private ComponentItemModel _cubeItem;

        #endregion

        #region Ctor

        public SelectCubeModel()
	{
        _cubeList = [];
    
	}

        #endregion

        #region Properties

        public ObservableCollection<ComponentItemModel> CubeList
        {
            get => _cubeList;

            set => SetProperty(ref _cubeList, value, nameof(CubeList));
        }

        public ComponentItemModel CubeItem
        {
            get => _cubeItem;

            set => SetProperty(ref _cubeItem, value, nameof(CubeItem));
        }

        #endregion

        #region Methods

        public void Load(MyCubeSize cubeSize, MyObjectBuilderType typeId, string subTypeId)
        {
            CubeList.Clear();

            SortedList<string, ComponentItemModel> list = [];
            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            var cubeDefinitions = SpaceEngineersResources.CubeBlockDefinitions.Where(c => c.CubeSize == cubeSize);

            foreach (var cubeDefinition in cubeDefinitions)
            {
                string textureFile = null;

                if (cubeDefinition.Icons != null)
                {
                    string icon = cubeDefinition.Icons.FirstOrDefault();

                    if (icon != null)
                        textureFile = SpaceEngineersCore.GetDataPathOrDefault(icon, Path.Combine(contentPath, icon));
                }

                TimeSpan buildTime = TimeSpan.Zero;

                if (cubeDefinition.IntegrityPointsPerSec != 0)
                {
                    double buildTimeSeconds = (double)cubeDefinition.MaxIntegrity / cubeDefinition.IntegrityPointsPerSec;

                    if (buildTimeSeconds <= TimeSpan.MaxValue.TotalSeconds)
                        buildTime = TimeSpan.FromSeconds(buildTimeSeconds);
                }

                ComponentItemModel c = new()
                {
                    Name = cubeDefinition.DisplayNameText,
                    TypeId = cubeDefinition.Id.TypeId,
                    TypeIdString = cubeDefinition.Id.TypeId.ToString(),
                    SubtypeId = cubeDefinition.Id.SubtypeName,
                    TextureFile = textureFile,
                    Time = buildTime,
                    Accessible = cubeDefinition.Public,
                    Mass = SpaceEngineersApi.FetchCubeBlockMass(cubeDefinition.Id.TypeId, cubeDefinition.CubeSize, cubeDefinition.Id.SubtypeName),
                    CubeSize = cubeDefinition.CubeSize,
                    Size = new BindableSize3DIModel(cubeDefinition.Size),
                };

                list.Add(c.FriendlyName + c.TypeIdString + c.SubtypeId, c);
            }

            ComponentItemModel cubeItem = null;

            foreach (var kvp in list)
            {
                var cube = kvp.Value;

                CubeList.Add(cube);

                if (cubeItem == null && cube.TypeId == typeId && cube.SubtypeId == subTypeId)
                    cubeItem = cube;
            }

            CubeItem = cubeItem ?? new ComponentItemModel(); 
        }

        #endregion
    }
}
