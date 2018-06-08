using System;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Represents an adornment layer.
    /// </summary>
    public interface IAdornmentLayer
    {
        /// <summary>
        /// Gets the <see cref="IWpfTextView"/> to which this layer is attached.
        /// </summary>
        ITextView TextView { get; }

        /// <summary>
        /// Adds a <see cref="UIElement"/> to the layer.
        /// </summary>
        /// <param name="behavior">The positioning behavior of <paramref name="adornment"/>.</param>
        /// <param name="visualSpan">The span with which <paramref name="adornment"/> is associated.</param>
        /// <param name="tag">The tag associated with <paramref name="adornment"/>.</param>
        /// <param name="adornment">The <see cref="UIElement"/> to add to the view.</param>
        /// <param name="removedCallback">The delegate to call when <paramref name="adornment"/>
        /// is removed from the view.</param>
        /// <returns><c>true</c> if <paramref name="adornment"/> was added to the layer, otherwise <c>false</c>. 
        /// <paramref name="visualSpan"/> does not intersect the text that is visible in the view.</returns>
        /// <remarks>
        /// <para>If <paramref name="visualSpan"/> is specified, then the adornment will be removed whenever any line that crosses <paramref name="visualSpan"/> is formatted.</para>
        /// <para>If <paramref name="visualSpan"/> has a length of zero, then it will be invalidated when the line that contains the following character is invalidated
        /// (or the last line, if the visual span is at the end of the buffer).</para>
        /// </remarks>
        bool AddAdornment(AdornmentPositioningBehavior behavior, SnapshotSpan? visualSpan,
		                  object tag, AdornmentElement adornment, AdornmentRemovedCallback removedCallback);

        /// <summary>
        /// Adds a AdornmentPositioningBehavior.TextRelative <see cref="UIElement"/> to the layer.
        /// </summary>
        /// <param name="visualSpan">The span with which <paramref name="adornment"/> is associated.</param>
        /// <param name="tag">The tag associated with <paramref name="adornment"/>.</param>
        /// <param name="adornment">The <see cref="UIElement"/> to add to the view.</param>
        /// <returns><c>true</c> if <paramref name="adornment"/> was added to the layer, otherwise <c>false</c>. 
        /// <paramref name="visualSpan"/> does not intersect the text that is visible in the view.</returns>
        /// <remarks>This is equivalent to calling AddElement(AdornmentPositioningBehavior.TextRelative,
        /// visualSpan, tag, adornment, null);</remarks>
        /// <remarks>
        /// <para>The adornment is removed when any line that crosses <paramref name="visualSpan"/> is formatted.</para>
        /// <para>If <paramref name="visualSpan"/> has a length of zero, then it will be invalidated when the line that contains the following character is invalidated
        /// (or the last line, if the visualSpan is at the end of the buffer).</para>
        /// </remarks>
		bool AddAdornment(SnapshotSpan visualSpan, object tag, AdornmentElement adornment);

        /// <summary>
        /// Removes a specific <see cref="UIElement"/>.
        /// </summary>
        /// <param name="adornment"><see cref="UIElement"/> to remove.</param>
		void RemoveAdornment(AdornmentElement adornment);

        /// <summary>
        /// Removes all <see cref="UIElement"/> objects associated with a particular tag.
        /// </summary>
        /// <param name="tag">The tag to use to remove <see cref="UIElement"/>s.</param>
        void RemoveAdornmentsByTag(object tag);

        /// <summary>
        /// Removes all adornments with visual spans that overlap the given visual span.
        /// Any adornments without specified visual spans are ignored.
        /// </summary>
        /// <param name="visualSpan">The visual span to check for overlap with adornments.</param>
        void RemoveAdornmentsByVisualSpan(SnapshotSpan visualSpan);

        /// <summary>
        /// Removes all adornments for which the given predicate returns <c>true</c>.
        /// </summary>
        /// <param name="match">The predicate that will be called for each adornment</param>
        void RemoveMatchingAdornments(Predicate<IAdornmentLayerElement> match);

        /// <summary>
        /// Removes all adornments with visual spans for which the given predicate returns <c>true</c>.
        /// Any adornments without specified visual spans and tag are ignored.
        /// </summary>
        /// <param name="visualSpan">The visual span to check for overlap with adornments.</param>
        /// <param name="match">The predicate that will be called for each adornment</param>
        void RemoveMatchingAdornments(SnapshotSpan visualSpan, Predicate<IAdornmentLayerElement> match);

        /// <summary>
        /// Removes all <see cref="UIElement"/> objects in the layer.
        /// </summary>
        void RemoveAllAdornments();

        /// <summary>
        /// Determines whether this layer is empty, that is, it does not contain any adornments.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets or sets the opacity factor applied to the entire adornment layer when it is rendered in the user interface.
        /// </summary>
        double Opacity { get; set; }

        /// <summary>
        /// Gets a collection of the adornments and their associated data in the layer.
        /// </summary>
        ReadOnlyCollection<IAdornmentLayerElement> Elements { get; }
    }

    /// <summary>
    /// Defines an element in an adornment layer.
    /// </summary>
    public interface IAdornmentLayerElement
    {
        /// <summary>
        /// Gets the snapshot span that is associated with the adornment.
        /// </summary>
        SnapshotSpan? VisualSpan { get; }
        /// <summary>
        /// Gets the positioning behavior of the adornment.
        /// </summary>
        AdornmentPositioningBehavior Behavior { get; }
        /// <summary>
        /// Gets the adornment.
        /// </summary>
		AdornmentElement Adornment { get; }

        /// <summary>
        /// Gets the tag associated with the adornment.
        /// </summary>
        object Tag { get; }
        /// <summary>
        /// Defines the behavior when an adornment has been removed.
        /// </summary>
        AdornmentRemovedCallback RemovedCallback { get; }
    }

    /// <summary>
    /// Defines the behavior when a <see cref="UIElement"/> is removed from an <see cref="IAdornmentLayer"/>.
    /// </summary>
    /// <param name="tag">The tag associated with <paramref name="element"/>.</param>
    /// <param name="element">The <see cref="UIElement"/> removed from the view.</param>
	public delegate void AdornmentRemovedCallback(object tag, AdornmentElement element);
}
