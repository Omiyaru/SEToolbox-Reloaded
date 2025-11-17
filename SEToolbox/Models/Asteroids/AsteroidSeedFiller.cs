using System;
using System.Collections.Generic;
using System.Linq;

using SEToolbox.Interop.Asteroids;
using SEToolbox.Support;


namespace SEToolbox.Models.Asteroids
{
    public class AsteroidSeedFiller : IMyVoxelFiller
    {
        private static readonly AsteroidSeedFillProperties properties = new();
        private AsteroidSeedFillProperties InitializeRandomModel(int index, MaterialSelectionModel defaultMaterial)
        {
            var randomModel = new AsteroidSeedFillProperties
            {
                Index = index
            };

            var materials = new List<MaterialSelectionModel>(properties.MaterialsList);
            var materialsDictionary = materials.ToDictionary(
            m => m.Value,
            m => defaultMaterial.Value == m.Value ? defaultMaterial : m, StringComparer.OrdinalIgnoreCase);

            return randomModel;
        }

        public IMyVoxelFillProperties CreateRandom(int index, MaterialSelectionModel defaultMaterial, IEnumerable<MaterialSelectionModel> materialsCollection, IEnumerable<GenerateVoxelDetailModel> voxelCollection)
        {
            var randomModel = InitializeRandomModel(index, defaultMaterial);

            // Split voxel files by size thresholds
            var largeVoxelFileList = voxelCollection.Where(v => v.FileSize > 100000).ToList();
            var smallVoxelFileList = voxelCollection.Where(v => v.FileSize > 0 && v.FileSize < 100000).ToList();

            // Ensure we have at least one list populated
            if (!largeVoxelFileList.Any() && !smallVoxelFileList.Any())
                throw new Exception("No valid asteroids found. Re-validate your game cache.");

            // Fallback logic if one list is empty
            double d = RandomUtil.GetDouble(1, 100);
            bool hasLarge = largeVoxelFileList.Any();
            bool hasSmall = smallVoxelFileList.Any();

            d = hasLarge && hasSmall ? d : (hasLarge ? 100 : (hasSmall ? d : 1));

            bool isLarge = d > 70;
            var selectedVoxelList = isLarge ? largeVoxelFileList : smallVoxelFileList;

            // Random asteroid selection
            int voxelIdx = RandomUtil.GetInt(selectedVoxelList.Count());
            randomModel.VoxelFile = selectedVoxelList[voxelIdx];

            //Random Main material selection (non-rare)
            var nonRare = materialsCollection.Where(m => !m.IsRare).ToArray();
            randomModel.MainMaterial = nonRare.ElementAt(RandomUtil.GetInt(nonRare.Length));

            // Categorize materials
            var rare = materialsCollection.Where(m => m.IsRare && m.MinedRatio >= 2).ToList();
            var superRare = materialsCollection.Where(m => m.IsRare && m.MinedRatio < 2).ToList();

            // Parameters
            int chunks = isLarge ? 20 : 10;
            int chunkSize = isLarge ? 5 : 2;
            double multiplier = 1.0;

            // === Rare Assignments ===
            AssignMaterials(index, randomModel, rare, chunks, chunkSize, ref multiplier, isLarge, isSuperRare: false);

            // === Reset for Super-Rare Assignments ===
            multiplier = 1.0;
            chunks = isLarge ? 50 : 10;//large/small 
            chunkSize = isLarge ? 2 : 0; //large/small
            // === Super-Rare Assignments ===
            AssignMaterials(index, randomModel, superRare, chunks, chunkSize, ref multiplier, isLarge, isSuperRare: true);

            return randomModel;
        }

        public MaterialSelectionModel GetMaterial(int index, List<MaterialSelectionModel> materials)
        {
            if (materials == null || materials.Count == 0)
                throw new ArgumentNullException(nameof(materials));
            if (index < 0 || index >= materials.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return materials[index];
        }

        private static void AssignMaterials(int index, AsteroidSeedFillProperties randomModel, List<MaterialSelectionModel> materials, int chunks, int chunkSize, ref double multiplier, bool isLarge = false, bool isSuperRare = false)
        {   
           bool isSecretRandom;
            randomModel.MaterialsList = [];
            int slotIndex = isSuperRare ? 5 : 1; // superRare starts filling from slot 5+

            // Set how many we’ll assign depending on size/type
            _ = isSuperRare ?
                (isLarge ? 7 : 5) :
                 isLarge ? 4 : 3;

            var materialsToRemove = new List<MaterialSelectionModel>();
            foreach (var mat in materials)
            {

               
              
                    
                isSecretRandom = RandomUtil.EnableSecretRandom;
                int idx = RandomUtil.GetInt(materials.Count);
                
                var material = materials[idx];
                int veins = RandomUtil.GetInt((int)(chunks * multiplier), (int)(chunks * 1.5 * multiplier));
                double radius = chunkSize > 0 ? RandomUtil.GetInt((int)(chunkSize * multiplier), (int)(chunkSize * 1.5 * multiplier)) : 0;
                AsteroidSeedFillProperties.MaterialsData[index] = (material.Value.ToString(), material, radius, veins);
                materialsToRemove.Add(material);

                foreach (var m in materialsToRemove)
                {
                    var inst = mat;
                    idx = materials.IndexOf(mat);
                    material = materials[idx];
                    materials.Remove(material);
                }
                multiplier = 1.0;

                if (slotIndex >= 1 && slotIndex <= 7)
                {
                    bool isSuperRareRange = slotIndex >= 5 && slotIndex <= 7 && isSuperRare;
                    double multiplierFactor = isSuperRareRange ? 0.75 : 0.50;
              

                    foreach (var kv in AsteroidSeedFillProperties.MaterialsData)
                    {
                        material = kv.Value.Material;
                        veins = (int)kv.Value.Veins;
                        radius = (double)kv.Value.Radius;
                        }
                    if (isSecretRandom)
                    {
                        int hash;
                        hash = HashCode.Combine(materials.IndexOf(material), veins, Convert.ToInt32(radius)).GetHashCode();
                        RandomUtil.SetSecretRandom(hash);
                       }

                    multiplier *= multiplierFactor;
                }


                    materials.RemoveAt(idx);
                }
                multiplier *= isLarge ? 0.85 : 0.75;
            }


        //interiorMaterial???
        public void FillAsteroid(MyVoxelMapBase asteroid, IMyVoxelFillProperties fillProperties)
        {
            AsteroidSeedFillProperties properties = (AsteroidSeedFillProperties)fillProperties;

            /* The full history behind this hack/crutch eludes me.
                * There are roids that won't change their materials unless their face materials forced to something other than current value.
                * So we have to do that manually by setting to a usually unused or (uranium) and then reverting to the one we chose (=old one in case of a flaky roid)
                */
            //byte oldMaterial = asteroid.VoxelMaterial;
            //var value = properties.MainMaterial;
            
            // ForceVoxelFaceMaterial should no longer be required.
            // MyVoxelMaterialDefinition voxelSurfaceMaterial = value.VoxelSurfaceMaterial
            //asteroid.ForceVoxelFaceMaterial("Uraninite_01");
            //asteroid.ForceVoxelFaceMaterial(properties.MainMaterial.Value);

            ///look into SurfaceMaterial?? (something to o withh planets??
            // Cycle through veins info and add 'spherical' depisits to the voxel cell grid (not voxels themselves)

            // Add ore veins into the asteroid voxel field

            for (int index = 0; index < 7; index++)
            {
                int veins = properties.GetVeins(index, null);
                if (veins <= 0)
                    continue;
                MaterialSelectionModel material = AsteroidSeedFillProperties.GetMaterial(index, null);

                double radius = AsteroidSeedFillProperties.GetRadius(index);

                if (veins <= 0)
                    continue;

                for (int v = 0; v < veins; v++)
                {
                    asteroid.SeedMaterialSphere(material.MaterialIndex.Value, radius);
                }
            }
            //look into surface materials

            // Hide the surface materials up to depth of 2 cells.
            asteroid.ForceShellMaterial(properties.MainMaterial.Value, 2);


            // The following code blocks contain alternative methods for material manipulation, currently not in use 

            // This recovers material assigning ability for most roids (could be specific to indestructibleContent property?)
            // And not for all, apparently :(
            //asteroid.ForceVoxelFaceMaterial(_dataModel.BaseMaterial.DisplayName); // don't change mattype

            // Doesn't help
            // asteroid.ForceIndestructibleContent(0xff);

            // Alt ends

        }
    }
}

