using System;
using System.Diagnostics.Contracts;
using SEToolbox.Interfaces;

namespace SEToolbox.Services
{
    /// <summary>
    /// Class wrapping System.Windows.Forms.ColorDialog, making it accept a IColorDialog.
    /// </summary>
    public class ColorDialog : IDisposable
    {
        private readonly IColorDialog _colorDialog;
        private System.Windows.Forms.ColorDialog _concreteColorDialog;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorDialog"/> class.
        /// </summary>
        /// <param name="colorDialog">The interface of a color dialog.</param>
        public ColorDialog(IColorDialog colorDialog)
        {
            Contract.Requires(colorDialog != null);

            _colorDialog = colorDialog;

            // Create concrete ColorDialog
            _concreteColorDialog = new System.Windows.Forms.ColorDialog
            {
                AllowFullOpen = colorDialog.AllowFullOpen,
                AnyColor = colorDialog.AnyColor,
                FullOpen = colorDialog.FullOpen,
                CustomColors = colorDialog.CustomColors,
                ShowHelp = colorDialog.ShowHelp,
                SolidColorOnly = colorDialog.SolidColorOnly,
            };
            
            _concreteColorDialog.Color = _colorDialog switch
            {
                IColorDialog c when  c.DrawingColor.HasValue => c.DrawingColor.Value,
                IColorDialog c when  c.MediaColor.HasValue => System.Drawing.Color.FromArgb(c.MediaColor.Value.A, c.MediaColor.Value.R, c.MediaColor.Value.G, c.MediaColor.Value.B),
                IColorDialog c when  c.BrushColor != null => System.Drawing.Color.FromArgb(c.BrushColor.Color.A, c.BrushColor.Color.R, c.BrushColor.Color.G, c.BrushColor.Color.B),
                _ => throw new InvalidOperationException($"Unknown color source {_concreteColorDialog.Color}"),
            };

        }

        /// <summary>
        /// Runs a common dialog box with the specified owner.
        /// </summary>
        /// <param name="owner">
        /// Any object that implements System.Windows.Forms.IWin32Window that represents the top-level
        /// window that will own the modal dialog box.
        /// </param>
        /// <returns>
        /// System.Windows.Forms.DialogResult.OK if the user clicks OK in the dialog box; otherwise,
        /// System.Windows.Forms.DialogResult.Cancel.
        /// </returns>
        public System.Windows.Forms.DialogResult ShowDialog(System.Windows.Forms.IWin32Window owner)
        {
            Contract.Requires(owner != null);

            var result = _concreteColorDialog.ShowDialog(owner);

            // Update ViewModel
            _colorDialog.DrawingColor = _concreteColorDialog.Color;
            _colorDialog.MediaColor = System.Windows.Media.Color.FromArgb(_concreteColorDialog.Color.A, _concreteColorDialog.Color.R, _concreteColorDialog.Color.G, _concreteColorDialog.Color.B);
            _colorDialog.BrushColor = new System.Windows.Media.SolidColorBrush(_colorDialog.MediaColor.Value);
            _colorDialog.CustomColors = _concreteColorDialog.CustomColors;

            return result;
        }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ColorDialog()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _concreteColorDialog?.Dispose();
                _concreteColorDialog = null;
            }
        }
    }

    #endregion
}

