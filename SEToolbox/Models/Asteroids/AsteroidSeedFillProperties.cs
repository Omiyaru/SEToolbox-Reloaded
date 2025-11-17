

using SEToolbox.Support;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;

namespace SEToolbox.Models.Asteroids
{
    public class AsteroidSeedFillProperties : BaseModel, IMyVoxelFillProperties
    {
        #region Fields

        private int _index;
        private GenerateVoxelDetailModel _voxelFile;
        private MaterialSelectionModel _mainMaterial, _firstMaterial, _secondMaterial, _thirdMaterial, _fourthMaterial, _fifthMaterial, _sixthMaterial, _seventhMaterial;
        private int _firstVeins, _secondVeins, _thirdVeins, _fourthVeins, _fifthVeins, _sixthVeins, _seventhVeins;
        private double _firstRadius, _secondRadius, _thirdRadius, _fourthRadius, _fifthRadius, _sixthRadius, _seventhRadius;
        private ObservableCollection<GenerateVoxelDetailModel> _voxelFileList;
        private List<MaterialSelectionModel> _materialsList;
        private List<VoxelMaterialAssetModel> _materialsAssets;


        #endregion
        public struct MaterialInfo
        {
            public int Index;
            public string Name;
            public MaterialSelectionModel Material;
            public double Radius ;
            public int Veins;
        }           
            public static Dictionary<int, (string Name, MaterialSelectionModel Material, double? Radius, int? Veins)> _materialsData = MaterialsData;
            private static readonly Dictionary<int, (string Name, MaterialSelectionModel Material, double? Radius, int? Veins)> _materialsDataCache = [];
            public static Dictionary<int, (string Name, MaterialSelectionModel Material, double? Radius, int? Veins)> MaterialsData => _materialsDataCache;
            // private static void BuildMaterialsDataCache()
            // {
            //     if (_materialsDataCache.Count > 0)
            //         return;
            //     var index = 0;
            //     foreach (var material in _materialsData.Select(x => x.Value.Material))
            //     {
            //         _materialsDataCache.Add(index, ($"{material}", material, index > 0 ? material.Radius : null, index > 0 ? material.Veins : null));
            //         index++;
            //     }
            //}
    
      
        #region Properties
        public ObservableCollection<GenerateVoxelDetailModel> VoxelFileList
        {
            get => _voxelFileList;
            set => SetProperty(ref _voxelFileList, value, nameof(VoxelFileList));

        }


        public List<MaterialSelectionModel> MaterialsList
        {
            get => _materialsList = [.. MaterialsData.Select(x => x.Value.Material)];
            set => SetProperty( ref _materialsList, value, nameof(MaterialsList));
        }

        public GenerateVoxelDetailModel VoxelFile
        {
            get => _voxelFile;
            set => SetProperty(ref _voxelFile, value, nameof(VoxelFile));
        }


        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value, nameof(Index));
        }

        public MaterialSelectionModel MainMaterial
        {
            get => _mainMaterial;
            set => SetProperty(ref _mainMaterial, value, nameof(MainMaterial));
        }

        public MaterialSelectionModel FirstMaterial
        {
            get => _firstMaterial;
            set => SetProperty(ref _firstMaterial, value, nameof(FirstMaterial));
        }

        public int FirstVeins
        {
            get => _firstVeins == 0 ? 1 : _firstVeins;
            set => _firstVeins = value;
        }

        public double FirstRadius
        {
            get => _firstRadius == 0 ? 1 : _firstRadius;
            set => _firstRadius = value;
        }

        public MaterialSelectionModel SecondMaterial
        {
            get => _secondMaterial;
            set => SetProperty(ref _secondMaterial, value, nameof(SecondMaterial));
        }

        public int SecondVeins
        {
            get => _secondVeins == 0 ? 1 : _secondVeins;
            set => _secondVeins = value;
        }

        public double SecondRadius
        {
            get => _secondRadius == 0 ? 1 : _secondRadius;
            set => _secondRadius = value;
        }

        public MaterialSelectionModel ThirdMaterial
        {
            get => _thirdMaterial;
            set => SetProperty(ref _thirdMaterial, value, nameof(ThirdMaterial));
        }

        public int ThirdVeins
        {
            get => _thirdVeins == 0 ? 1 : _thirdVeins;
            set => _thirdVeins = value;
        }

        public double ThirdRadius
        {
            get => _thirdRadius == 0 ? 1 : _thirdRadius;
            set => _thirdRadius = value;
        }

        public MaterialSelectionModel FourthMaterial
        {
            get => _fourthMaterial;
            set => SetProperty(ref _fourthMaterial, value, nameof(FourthMaterial));
        }

        public int FourthVeins
        {
            get => _fourthVeins == 0 ? 1 : _fourthVeins;
            set => _fourthVeins = value;
        }

        public double FourthRadius
        {
            get => _fourthRadius == 0 ? 1 : _fourthRadius;
            set => _fourthRadius = value;
        }

        public MaterialSelectionModel FifthMaterial
        {
            get => _fifthMaterial;
            set => SetProperty(ref _fifthMaterial, value, nameof(FifthMaterial));
        }

        public int FifthVeins
        {
            get => _fifthVeins == 0 ? 1 : _fifthVeins;
            set => _fifthVeins = value;
        }

        public double FifthRadius
        {
            get => _fifthRadius == 0 ? 1 : _fifthRadius;
            set => _fifthRadius = value;
        }

        public MaterialSelectionModel SixthMaterial
        {
            get => _sixthMaterial;
            set => SetProperty(ref _sixthMaterial, value, nameof(SixthMaterial));
        }

        public int SixthVeins
        {
            get => _sixthVeins == 0 ? 1 : _sixthVeins;
            set => _sixthVeins = value;
        }

        public double SixthRadius
        {
            get => _sixthRadius == 0 ? 1 : _sixthRadius;
            set => _sixthRadius = value;
        }

        public MaterialSelectionModel SeventhMaterial
        {
            get => _seventhMaterial;
            set => SetProperty(ref _seventhMaterial, value, nameof(SeventhMaterial));
	  }

       	public int SeventhVeins
        {
            get => _seventhVeins == 0 ? 1 : _seventhVeins;
            set => _seventhVeins = value;
        }

        public double SeventhRadius
        {
            get => _seventhRadius == 0 ? 1 : _seventhRadius;
            set => _seventhRadius = value;
        }
        
        public List<VoxelMaterialAssetModel> MaterialsAssets
        {
            get => _materialsAssets;
            set => _materialsAssets = value;
        }

        #endregion

        public IMyVoxelFillProperties Clone()
        {
            AsteroidSeedFillProperties clone = (AsteroidSeedFillProperties)MemberwiseClone();
            clone.Index = Index;
            clone.VoxelFile = VoxelFile.Clone();
            var clonedMaterials = new List<MaterialSelectionModel>();
            foreach (var material in MaterialsList)
            {
                clonedMaterials.Add(material.Clone());
                clone.MaterialsList = [.. clone.MaterialsList.Select(x => x.Clone())];
            }
            return clone;
        }
        
        static AsteroidSeedFillProperties()
        {
            var properties = new AsteroidSeedFillProperties();
            foreach (var (material, index) in properties.MaterialsList.Select((x, i) => (x, i)))
            {
                _materialsData.Add(index, ($"{material}", material, index > 0 ? material.Radius : null, index > 0 ? material.Veins : null));
            }
        }

        public static void GetMaterial(int index, MaterialSelectionModel material, int? radius, int? veins)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material));

            if (index < 0 || index >= MaterialsData.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            MaterialsData[index] = ($"{material}", material, radius, veins);
        }

        /// <summary>
        /// Retrieves the radius of the material at the specified vein index.
        /// </summary>
        /// <param name="index">The index of the vein from which to retrieve the radius.</param>
        /// <returns>The radius of the material at the specified vein index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the vein index is invalid.</exception>
        public static double GetRadius(int index)
        {
            var materials = MaterialsData.Select(x => x.Value.Material).ToList();
            return GetMaterial(index, materials).Radius;
        }
      
        public static MaterialSelectionModel GetMaterial(int index, List<MaterialSelectionModel> materialList)
        {
            if (materialList == null)
                throw new ArgumentNullException(nameof(materialList));

            if (index < 0 || index >= materialList.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return materialList[index];
        }

        public  int GetVeins(int index, List<MaterialSelectionModel> materialsList)
        {
            if (index < 0 || index >= MaterialsList.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Invalid vein index: " + index);
            return materialsList[index].Veins;
        }
    
        public  static void SetMaterial( int index, MaterialSelectionModel material, int? radius, int? veins)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material));

            if (index < 0 || index >= MaterialsData.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            MaterialsData[index] = ($"{material}", material, radius, veins);
        }

        internal void RandomizeMaterials()
        {
            if (MaterialsList == null || MaterialsList.Count == 0)
                throw new InvalidOperationException(" Materials list is empty or not initialized.");

            var random = new Random();
            var indices = Enumerable.Range(0, MaterialsList.Count).OrderBy(i => RandomUtil.GetInt(0, MaterialsList.Count)).ToArray();

            var veins = RandomUtil.GetInt((int)(MaterialsData.Select(x => x.Value.Veins).Min() * 0.85),
                                    (int)(MaterialsData.Select(x => x.Value.Veins).Max() * 1.5 * 0.85));
            
            var radius = RandomUtil.GetInt((int)( MaterialsData.Select(x => x.Value.Radius).Min() * 0.85),
                                     (int)( MaterialsData.Select(x => x.Value.Radius).Max() * 1.5 * 0.85));

            MainMaterial = MaterialsList[indices[0]];
            var materialIndices = indices.Skip(1).Take(veins).ToArray();
            foreach (var index in materialIndices)
            {
                GetMaterial(index, MaterialsList[index], radius, veins);
            }
        }
        
    }
}

