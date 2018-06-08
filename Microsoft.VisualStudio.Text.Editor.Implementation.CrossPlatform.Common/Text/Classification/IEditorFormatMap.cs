//
// IEditorFormatMap.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corp
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Classification
{
	/// <summary>
	/// Maps from arbitrary keys to a <see cref="ResourceDictionary"/>.
	/// </summary>
	public interface IEditorFormatMap
	{
		/// <summary>
		/// Gets a <see cref="ResourceDictionary"/> for the specified key.
		/// </summary>
		/// <param name="key">
		/// The key.
		/// </param>
		/// <returns>
		/// The <see cref="ResourceDictionary"/> object that represents the set of property
		/// contributions from the provided <see cref="EditorFormatDefinition"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is empty or null.</exception>
		Dictionary<string, object> GetProperties (string key);

		/// <summary>
		/// Adds a <see cref="ResourceDictionary"/> for a new key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="properties">The new properties.</param>
		/// <remarks>
		/// <para>
		/// Adding properties will cause the FormatMappingChanged event to be raised.
		/// </para>
		/// <para>If <paramref name="key"/> already exists in the map, then this is equivalent to <see cref="SetProperties"/>.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is null or empty.</exception>
		void AddProperties (string key, Dictionary<string, object> properties);

		/// <summary>
		/// Sets the <see cref="ResourceDictionary"/> of a key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="properties">The new <see cref="ResourceDictionary"/> of properties.</param>
		/// <remarks>
		/// <para>
		/// Setting properties will cause the FormatMappingChanged event to be raised.
		/// </para>
		/// <para>
		/// If the <see cref="ResourceDictionary"/> set does not contain the expected properties, the consumer
		/// of the properties may throw an exception.
		/// </para>
		/// </remarks>
		void SetProperties (string key, Dictionary<string, object> properties);

		/// <summary>
		/// Begins a batch update on this <see cref="IEditorFormatMap"/>. Events
		/// will not be raised until <see cref="EndBatchUpdate"/> is called.
		/// </summary>
		/// <exception cref="InvalidOperationException"><see cref="BeginBatchUpdate"/> was called for a second time 
		/// without calling <see cref="EndBatchUpdate"/>.</exception>
		/// <remarks>You must call <see cref="EndBatchUpdate"/> in order to re-enable FormatMappingChanged events.</remarks>
		void BeginBatchUpdate ();

		/// <summary>
		/// Ends a batch update on this <see cref="IEditorFormatMap"/> and raises an event if any changes were made during
		/// the batch update.
		/// </summary>
		/// <exception cref="InvalidOperationException"><see cref="EndBatchUpdate"/> was called without calling <see cref="BeginBatchUpdate"/> first.</exception>
		/// <remarks>You must call <see cref="EndBatchUpdate"/> in order to re-enable FormatMappingChanged events if <see cref="BeginBatchUpdate"/> was called.</remarks>
		void EndBatchUpdate ();

		/// <summary>
		/// Determines whether this <see cref="IEditorFormatMap"/> is in the middle of a batch update.
		/// </summary>
		bool IsInBatchUpdate { get; }

		/// <summary>
		/// Occurs when this <see cref="IEditorFormatMap"/> changes.
		/// </summary>
		event EventHandler<FormatItemsEventArgs> FormatMappingChanged;
	}
}
