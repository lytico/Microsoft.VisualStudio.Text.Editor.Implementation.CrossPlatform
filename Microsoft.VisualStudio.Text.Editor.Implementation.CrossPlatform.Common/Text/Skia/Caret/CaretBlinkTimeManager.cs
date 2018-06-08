using System;
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Caches the system's current "caret blink time" setting.
    /// </summary>
    internal static class CaretBlinkTimeManager
    {
        private static int _blinkTime = 500;

#if __WINDOWS__
        [System.Security.SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            /// <summary>
            /// Gets the system caret's blink time rate
            /// </summary>
            /// <returns>
            /// An integer that represents the blink time in milli seconds
            /// </returns>
            [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetCaretBlinkTime")]
            internal static extern int GetCaretBlinkTime();
        }

        static CaretBlinkTimeManager()
        {
            _blinkTime = NativeMethods.GetCaretBlinkTime();

            // Note that this event only fires when the blink time is changed from the Control Panel.
            // If another program calls SetCaretBlinkTime from user32.dll, we won't be notified.
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        private static void SystemEvents_UserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            if (e.Category == Microsoft.Win32.UserPreferenceCategory.Keyboard)
            {
                _blinkTime = NativeMethods.GetCaretBlinkTime();
            }
        }
#elif __MACOS__
        static CaretBlinkTimeManager()
        {
            _blinkTime = (int)Foundation.NSUserDefaults.StandardUserDefaults.FloatForKey("NSTextInsertionPointBlinkPeriodOn");
            //TODO: Make use of NSTextInsertionPointBlinkPeriodOff
        }
#else
        //TOOD: Linux

#endif

        /// <summary>
        /// Gets the system caret's blink time rate
        /// </summary>
        /// <returns>
        /// An integer that represents the blink time in milli seconds
        /// </returns>
        internal static int GetCaretBlinkTime()
        {
            return _blinkTime;
        }
    }
}
