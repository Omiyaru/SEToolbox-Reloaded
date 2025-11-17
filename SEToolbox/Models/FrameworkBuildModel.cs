namespace SEToolbox.Models
{
    public class FrameworkBuildModel : BaseModel
    {
        public const int UniqueUnits = 1;

        #region Fields

        private double? _buildPercent;

        #endregion

        #region Properties

        public double? BuildPercent
        {
            get => _buildPercent;

            set => SetProperty(ref _buildPercent, value, nameof(BuildPercent));
        }

        #endregion
    }
}
