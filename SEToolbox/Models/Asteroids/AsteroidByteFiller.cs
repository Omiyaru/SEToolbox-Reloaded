using System;
using System.Collections.Generic;
using System.Linq;

using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Models;
using SEToolbox.Support;


namespace SEToolbox.Models.Asteroids
{
    public class AsteroidByteFiller : IMyVoxelFiller
    {
        public IMyVoxelFillProperties CreateRandom(int index, MaterialSelectionModel defaultMaterial, IEnumerable<MaterialSelectionModel> materialsCollection, IEnumerable<GenerateVoxelDetailModel> voxelCollection)
        {
            AsteroidByteFillProperties randomModel = new()
            {
                Index = index,
                MainMaterial = defaultMaterial,
                SecondMaterial = defaultMaterial,
                ThirdMaterial = defaultMaterial,
                FourthMaterial = defaultMaterial,
                FifthMaterial = defaultMaterial,
                SixthMaterial = defaultMaterial,
                SeventhMaterial = defaultMaterial,
            };

            //Must be by reference, not by value

            List<GenerateVoxelDetailModel> largeVoxelFileList = [.. voxelCollection.Where(v => v.FileSize > 100000)];
            List<GenerateVoxelDetailModel> smallVoxelFileList = [.. voxelCollection.Where(v => v.FileSize > 0 && v.FileSize < 100000)];

            if (!largeVoxelFileList.Any() && !smallVoxelFileList.Any())
                // no asteroids? You are so screwed.
                throw new Exception("No valid asteroids found. Re-validate your game cache.");
            bool hasSmallVoxelFiles = smallVoxelFileList.Any();
            double randomValue = hasSmallVoxelFiles ? RandomUtil.GetDouble(1, 100) : 100;
            double d = largeVoxelFileList.Any() ? randomValue : 1;
            bool isLarge = d > 70;

            List<GenerateVoxelDetailModel> selectedVoxelList = isLarge ? largeVoxelFileList : smallVoxelFileList;
            randomModel.VoxelFile = selectedVoxelList[RandomUtil.GetInt(selectedVoxelList.Count)];

            MaterialSelectionModel[] nonRareMaterials = [.. materialsCollection.Where(m => !m.IsRare)];
            randomModel.MainMaterial = nonRareMaterials[RandomUtil.GetInt(nonRareMaterials.Length)];

            List<MaterialSelectionModel> rareMaterials = [.. materialsCollection.Where(m => m.IsRare && m.MinedRatio >= 2)];
            List<MaterialSelectionModel> superRareMaterials = [.. materialsCollection.Where(m => m.IsRare && m.MinedRatio < 2)];

            if (isLarge)
            {
                AssignMaterials(randomModel, rareMaterials, superRareMaterials, [(40, 60), (6, 12), (6, 12)], [(2, 4), (1, 3), (1, 3)]);
            }
            else
            {
                AssignMaterials(randomModel, rareMaterials, superRareMaterials, [(6, 13)], [(2, 4)]);
            }

            return randomModel;
        }

        private static void AssignMaterials(AsteroidByteFillProperties model, List<MaterialSelectionModel> rare, List<MaterialSelectionModel> superRare, (int min, int max)[] rarePercents, (int min, int max)[] superRarePercents)
        {
            for (int i = 0; i < rarePercents.Length && rare.Any(); i++)
            {
                int idx = RandomUtil.GetInt(rare.Count);
                int percent = RandomUtil.GetInt(rarePercents[i].min, rarePercents[i].max);
                AssignMaterial(model, i + 1, rare[idx], percent);
                rare.RemoveAt(idx);
            }

            for (int i = 0; i < superRarePercents.Length && superRare.Any(); i++)
            {
                int idx = RandomUtil.GetInt(superRare.Count);
                int percent = RandomUtil.GetInt(superRarePercents[i].min, superRarePercents[i].max);
                AssignMaterial(model, i + rarePercents.Length + 1, superRare[idx], percent);
                superRare.RemoveAt(idx);
            }
        }

        private static void AssignMaterial(AsteroidByteFillProperties model, int position, MaterialSelectionModel material, int percent)
        {
            switch (position)
            {
                case 1:
                    model.SecondMaterial = material;
                    model.SecondPercent = percent;
                    break;
                case 2:
                    model.ThirdMaterial = material;
                    model.ThirdPercent = percent;
                    break;
                case 3:
                    model.FourthMaterial = material;
                    model.FourthPercent = percent;
                    break;
                case 4:
                    model.FifthMaterial = material;
                    model.FifthPercent = percent;
                    break;
                case 5:
                    model.SixthMaterial = material;
                    model.SixthPercent = percent;
                    break;
                case 6:
                    model.SeventhMaterial = material;
                    model.SeventhPercent = percent;
                    break;
            }
        }

        public void FillAsteroid(MyVoxelMapBase asteroid, IMyVoxelFillProperties fillProperties)
        {
            AsteroidByteFillProperties properties = (AsteroidByteFillProperties)fillProperties;

            IList<byte> baseAssets = asteroid.CalcVoxelMaterialList();

            List<double> distribution = [double.NaN];
            List<byte> materialSelection =
            [
                // Ensure MainMaterial is not null
              (byte)Conditional.ConditionCoalesced(null, properties?.MainMaterial, SpaceEngineersResources.GetMaterialIndex(properties.MainMaterial.Value),0),
            ];

            for (int i = 2; i <= 7; i++)
            {
                double percent = 0;
                string materialValue = null;

                switch (i)
                {
                    case 2:
                        percent = properties.SecondPercent;
                        materialValue = properties.SecondMaterial.Value;
                        break;
                    case 3:
                        percent = properties.ThirdPercent;
                        materialValue = properties.ThirdMaterial.Value;
                        break;
                    case 4:
                        percent = properties.FourthPercent;
                        materialValue = properties.FourthMaterial.Value;
                        break;
                    case 5:
                        percent = properties.FifthPercent;
                        materialValue = properties.FifthMaterial.Value;
                        break;
                    case 6:
                        percent = properties.SixthPercent;
                        materialValue = properties.SixthMaterial.Value;
                        break;
                    case 7:
                        percent = properties.SeventhPercent;
                        materialValue = properties.SeventhMaterial.Value;
                        break;
                }

                if (percent > 0 && materialValue != null)
                {
                    distribution.Add(percent / 100);
                    materialSelection.Add(SpaceEngineersResources.GetMaterialIndex(materialValue));
                }
            }
            List<byte> newDistribution = [];
            int count;
            for (int i = 1; i < distribution.Count; i++)
            {
                count = (int)Math.Floor(distribution[i] * baseAssets.Count); // Round down.
                for (int j = 0; j < count; j++)
                {
                    newDistribution.Add(materialSelection[i]);
                }
            }
            count = baseAssets.Count - newDistribution.Count;
            for (int j = 0; j < count; j++)
            {
                newDistribution.Add(materialSelection[0]);
            }

            newDistribution.Shuffle();
            asteroid.SetVoxelMaterialList(newDistribution);
            //asteroid.ForceVoxelFaceMaterial(_dataModel.BaseMaterial.DisplayName);
        }
        public void FillAsteroid(MyVoxelMapBase asteroid, IMyVoxelFillProperties fillProperties, AsteroidFillType.AsteroidFills fillType, int seed)
        {
            // seed = 0;
            if (fillType == AsteroidFillType.AsteroidFills.None)
            {
                FillAsteroid(asteroid, fillProperties);
                return;
            }
            //else if (seed > 0 )
            // {
            //     GenerateSeed(seed);
            // }
        }

        // public int  GenerateSeed(int seed) {

    }
        
    }


