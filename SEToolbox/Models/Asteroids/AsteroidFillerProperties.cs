using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Models;
using SEToolbox.Support;
using static Sandbox.Game.World.Generator.MyObjectSeed;
using static VRage.Game.MyObjectBuilder_AsteroidGeneratorDefinition;
using VRageMath;


namespace SEToolbox.Models.Asteroids
{
    public class AsteroidFillerProperties : BaseModel
    {   
        private int _asteroidSizeMax;
        private int _asteroidSizeMin;
        private int _seed;
        private object _surfaceModulation;
        private Vector3D? _surfaceModulationOffset;
        private Vector3D? _surfaceModulationScale;
        private int _seedTypes;
        private int _asteroidSize;

        public int AsteroidSizeMax
        {
            get => _asteroidSizeMax;
            set => SetProperty(ref _asteroidSize, value, nameof(AsteroidSizeMax));
        }

        public int AsteroidSizeMin
        {
            get => _asteroidSizeMin;
            set => SetProperty(ref _asteroidSizeMin, value, nameof(AsteroidSizeMin));
        }

        public int Seed
        {
            get => _seed;
            set => SetProperty(ref _seed, value, nameof(Seed));
        }
        public object SurfaceModulation
        {
            get => _surfaceModulation;
            set => SetProperty(ref _surfaceModulation, value, nameof(SurfaceModulation));
        }

       public Vector3D? SurfaceModulationOffset
        {
            get => _surfaceModulationOffset;
            set => SetProperty(ref _surfaceModulationOffset, value, nameof(SurfaceModulationOffset));
        }
        public Vector3D? SurfaceModulationScale
        {
            get => _surfaceModulationScale;
            set => SetProperty(ref _surfaceModulationScale, value, nameof(SurfaceModulationScale));
        }
        public int SeedTypes
        {
            get => _seedTypes;
            set => SetProperty(ref _seedTypes, value, nameof(SeedTypes));
        }
          
    }
}
