using System.Collections.ObjectModel;

namespace SEToolbox.Models
{
    public class RegeneratePlanetModel : BaseModel
    {
        #region Fields

        private int _seed;
        private decimal _diameter;
        private bool _invalidKeenRange;

        #endregion

        #region Ctor

        public RegeneratePlanetModel()
        {
        }

        #endregion

        #region Properties

        public int Seed
        {
            get => _seed;
            set => SetProperty(ref _seed, value, nameof(Seed));
        }

        public decimal Diameter
        {
            get => _diameter;
            set => SetProperty(ref _diameter, value, nameof(Diameter), () => 
            InvalidKeenRange = _diameter < 19000 || _diameter > 120000);
        }

        public bool InvalidKeenRange
        {
            get => _invalidKeenRange;
            set => SetProperty(ref _invalidKeenRange, value, nameof(InvalidKeenRange));
        }

        #endregion

        #region Methods

        public void Load(int seed, float radius)
        {
            Seed = seed;
            Diameter = (decimal)(radius * 2f);
        }

        #endregion
    }
}
