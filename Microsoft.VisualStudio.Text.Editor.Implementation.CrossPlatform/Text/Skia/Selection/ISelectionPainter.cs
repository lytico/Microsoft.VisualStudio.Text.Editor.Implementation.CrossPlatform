using System;
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Declares a contract to which any object willing to handle painting of the text selection on the screen must adhere to.
    /// </summary>
    public interface ISelectionPainter : IDisposable
    {
        #region Methods
        /// <summary>
        /// Clears the selection on the screen
        /// </summary>
        void Clear();

        /// <summary>
        /// Called when painter is made active.
        /// </summary>
        void Activate();

        /// <summary>
        /// Paints a new selection or refreshes an existing one
        /// </summary>
        void Update(bool selectionChanged);
        #endregion //Methods
    }
}
