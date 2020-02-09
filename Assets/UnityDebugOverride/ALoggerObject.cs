/******************************************************************************
*                                                                             *
*                             2/9/2020 2:02:02 PM                             *
*                                                                             *
*             What do you call an Argentinian with a rubber toe?              *
*                                   Roberto                                   *
*                                                                             *
******************************************************************************/

using System;

using UnityEngine;

namespace UnityDebugOverride {
    /// <summary>
    /// Provide a base level for logger Scriptable Objects to base their functionality off of
    /// </summary>
    /// <remarks>
    /// This class object is a Scriptable Object port of the UnityEngine.Logger object for use with
    /// the UnityDebugOverride system. This is intended as a quick start object where people can 
    /// customise the logging behaviour quickly without large amounts of copy/paste boilerplate. 
    /// All messages received via the ILogger interface are forwarded to two functions (ProcessLog 
    /// and ProcessException) but can be overridden individually if required
    /// 
    /// UnityEngine.Logger.cs accessed 2020-02-09 from
    /// https://github.com/jamesjlinden/unity-decompiled/blob/master/UnityEngine/UnityEngine/Logger.cs
    /// </remarks>
    public abstract class ALoggerObject : ScriptableObject, ILogger {
        /*----------Variables----------*/
        //VISIBLE

        [Header("Basic Settings")]

        [SerializeField, Tooltip("Flags if logging is enabled for this object")]
        private bool loggingEnabled = true;

        [SerializeField, Tooltip("Defines the minimum log severity that will be reported by this logger (Exceptions will always be reported)")]
        private LogType filterLogLevel = LogType.Log;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Get and set the Log handler that is assigned for this object to use
        /// </summary>
        public virtual ILogHandler logHandler { get; set; }

        /// <summary>
        /// Get and set logging enabled state for this object
        /// </summary>
        public virtual bool logEnabled { get { return loggingEnabled; } set { loggingEnabled = value; } }

        /// <summary>
        /// Get and set the log filtering level that is used to control logging output
        /// </summary>
        public virtual LogType filterLogType { get { return filterLogLevel; } set { filterLogLevel = value; } }

        /*----------Functions----------*/
        //PROTECTED

        /// <summary>
        /// Assign the default log handler if there is nothing else assigned
        /// </summary>
        private void OnEnable() { if (logHandler == null) logHandler = UnityDebugOverride.DEFAULT_LOGGER.logHandler; }

        /// <summary>
        /// Apply the default logging behaviour of the ILogHandler assigned
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="context">Object to which the message applies</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Arguments to be inserted into the format</param>
        protected virtual void ProcessLog(LogType logType, UnityEngine.Object context, string format, params object[] args) { logHandler.LogFormat(logType, context, format, args); }

        /// <summary>
        /// Apply the default logging behaviour of the ILogHandler assigned
        /// </summary>
        /// <param name="exception">The runtime exception that occurred</param>
        /// <param name="context">Object to which the message applies</param>
        protected virtual void ProcessException(System.Exception exception, UnityEngine.Object context) { logHandler.LogException(exception, context); }

        /// <summary>
        /// Convert a generic object to a printable string format
        /// </summary>
        /// <param name="message">The message that is to be converted</param>
        /// <returns>Returns a string representation of the object</returns>
        protected static string GetString(object message) {
            return (message != null ?
                message.ToString() :
                "Null"
            );
        }

        //PUBLIC

        /// <summary>
        /// Check to see if a log of the specified severity can be used
        /// </summary>
        /// <param name="logType">The log type that is to be checked</param>
        /// <returns>Returns true if the current settings allow for logging the specified message type</returns>
        public virtual bool IsLogTypeAllowed(LogType logType) {
            //If logging is not enabled, cant
            if (!loggingEnabled) return false;

            //Check if the log out of range (But allow exceptions)
            if (logType > filterLogLevel)
                return (logType == LogType.Exception);

            //If this far, it's fine
            return true;
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public virtual void Log(LogType logType, object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Process the message
            ProcessLog(logType, null, GetString(message));
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public virtual void Log(LogType logType, object message, UnityEngine.Object context) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Process the message
            ProcessLog(logType, context, GetString(message));
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public virtual void Log(LogType logType, string tag, object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Process the message
            ProcessLog(logType, null, string.Format("{0}: {1}", tag, GetString(message)));
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public virtual void Log(LogType logType, string tag, object message, UnityEngine.Object context) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Process the message
            ProcessLog(logType, context, string.Format("{0}: {1}", tag, GetString(message)));
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public virtual void Log(object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Log)) return;

            //Process the message
            ProcessLog(LogType.Log, null, GetString(message));
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public virtual void Log(string tag, object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Log)) return;

            //Process the message
            ProcessLog(LogType.Log, null, string.Format("{0}: {1}", tag, GetString(message)));
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public virtual void Log(string tag, object message, UnityEngine.Object context) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Log)) return;

            //Process the message
            ProcessLog(LogType.Log, context, string.Format("{0}: {1}", tag, GetString(message)));
        }

        /// <summary>
        /// A variant of Logger.Log that logs an warning message
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public virtual void LogWarning(string tag, object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Warning)) return;

            //Process the message
            ProcessLog(LogType.Warning, null, string.Format("{0}: {1}", tag, GetString(message)));
        }

        /// <summary>
        /// A variant of Logger.Log that logs an warning message
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public virtual void LogWarning(string tag, object message, UnityEngine.Object context) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Warning)) return;

            //Process the message
            ProcessLog(LogType.Warning, context, string.Format("{0}: {1}", tag, GetString(message)));
        }

        /// <summary>
        /// A variant of Logger.Log that logs an error message
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public virtual void LogError(string tag, object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Error)) return;

            //Process the message
            ProcessLog(LogType.Error, null, string.Format("{0}: {1}", tag, GetString(message)));
        }

        /// <summary>
        /// A variant of Logger.Log that logs an error message
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public virtual void LogError(string tag, object message, UnityEngine.Object context) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Error)) return;

            //Process the message
            ProcessLog(LogType.Error, context, string.Format("{0}: {1}", tag, GetString(message)));
        }

        /// <summary>
        /// Logs a formatted message
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Arguments to be inserted into the format</param>
        public virtual void LogFormat(LogType logType, string format, params object[] args) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Process the message
            ProcessLog(logType, null, format, args);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an exception message
        /// </summary>
        /// <param name="exception">The runtime exception that occurred</param>
        public virtual void LogException(Exception exception) {
            //Check that this object is has enabled logging
            if (!loggingEnabled) return;

            //Process the exception
            ProcessException(exception, null);
        }

        /// <summary>
        /// Logs a formatted message
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="context">Object to which the message applies</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Arguments to be inserted into the format</param>
        public virtual void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Process the message
            ProcessLog(logType, context, format, args);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an exception message
        /// </summary>
        /// <param name="exception">The runtime exception that occurred</param>
        /// <param name="context">Object to which the message applies</param>
        public virtual void LogException(Exception exception, UnityEngine.Object context) {
            //Check that this object is has enabled logging
            if (!loggingEnabled) return;

            //Process the exception
            ProcessException(exception, context);
        }
    }
}