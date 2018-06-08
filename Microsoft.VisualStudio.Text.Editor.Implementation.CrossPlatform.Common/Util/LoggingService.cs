//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//

using System;
using System.Collections;
using System.Text;

namespace Microsoft.VisualStudio.Text.Editor
{
    enum LogLevel
    {
        Fatal = 1,
        Error = 2,
        Warn = 4,
        Info = 8,
        Debug = 16,
    }

    static class LoggingService
    {
        #region convenience methods (string message)

        public static void LogDebug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public static void LogInfo(string message)
        {
            Log(LogLevel.Info, message);
        }

        public static void LogWarning(string message)
        {
            Log(LogLevel.Warn, message);
        }

        public static void LogError(string message)
        {
            Log(LogLevel.Error, message);
        }

        public static void LogFatalError(string message)
        {
            Log(LogLevel.Fatal, message);
        }

        #endregion

        #region convenience methods (string messageFormat, params object[] args)

        public static void LogDebug(string messageFormat, params object[] args)
        {
            Log(LogLevel.Debug, string.Format(messageFormat, args));
        }

        public static void LogInfo(string messageFormat, params object[] args)
        {
            Log(LogLevel.Info, string.Format(messageFormat, args));
        }

        public static void LogWarning(string messageFormat, params object[] args)
        {
            Log(LogLevel.Warn, string.Format(messageFormat, args));
        }

        public static void LogUserError(string messageFormat, params object[] args)
        {
            Log(LogLevel.Error, string.Format(messageFormat, args));
        }

        public static void LogError(string messageFormat, params object[] args)
        {
            LogUserError(messageFormat, args);
        }

        public static void LogFatalError(string messageFormat, params object[] args)
        {
            Log(LogLevel.Fatal, string.Format(messageFormat, args));
        }

        #endregion

        #region convenience methods (string message, Exception ex)

        static string FormatExceptionText(string message, Exception ex)
        {
            if (ex == null)
                return message;

            var exceptionText = new StringBuilder();
            exceptionText.AppendLine(message);
            exceptionText.Append(ex);
            if (ex.Data.Count > 0)
            {
                exceptionText.AppendLine();
                exceptionText.Append("Exception Data:");
                foreach (DictionaryEntry item in ex.Data)
                {
                    exceptionText.AppendLine();
                    exceptionText.AppendFormat("{0}: {1}", item.Key, item.Value);
                }
            }
            return exceptionText.ToString();
        }

        public static void LogDebug(string message, Exception ex)
        {
            Log(LogLevel.Debug, FormatExceptionText(message, ex));
        }

        public static void LogInfo(string message, Exception ex)
        {
            Log(LogLevel.Info, FormatExceptionText(message, ex));
        }

        public static void LogWarning(string message, Exception ex)
        {
            Log(LogLevel.Warn, FormatExceptionText(message, ex));
        }

        public static void LogError(string message, Exception ex)
        {
            Log(LogLevel.Error, message + (ex != null ? Environment.NewLine + ex : string.Empty));
        }
        #endregion

        public static void Log(LogLevel level, string message)
        {
            Console.WriteLine(level + ":" + message);
        }
    }
}