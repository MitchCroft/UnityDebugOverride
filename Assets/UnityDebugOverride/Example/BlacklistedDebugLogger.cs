/******************************************************************************
*                                                                             *
*                             2/8/2020 3:30:02 PM                             *
*                                                                             *
*                     Why did the chicken get a penalty?                      *
*                               For fowl play.                                *
*                                                                             *
******************************************************************************/

using System;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityDebugOverride.Example {
    /// <summary>
    /// Manage the writing of log messages to the debug console, with the option to blacklist specific messages
    /// </summary>
    public sealed class BlacklistedDebugLogger : ScriptableObject, ILogger {
        /*----------Types----------*/
        //PUBLIC

        /// <summary>
        /// Define the different types of operations that can be run on incoming log messages to determine if they are usable
        /// </summary>
        public enum EStringEvaluationOperation {
            /// <summary>
            /// Is the message identical to the listed sequence?
            /// </summary>
            Is,

            /// <summary>
            /// Does the message begin with the listed sequence?
            /// </summary>
            StartsWith,

            /// <summary>
            /// Does the message end with the listed sequence?
            /// </summary>
            EndsWith,

            /// <summary>
            /// Does the message contain the listed sequence at all?
            /// </summary>
            Contains,
        }

        /// <summary>
        /// Store a collection of values that defines an evaluation operation on an incoming string message
        /// </summary>
        [Serializable] public struct EvaluationOperation {
            /// <summary>
            /// The type of comparison operation that will be run with the sequence of characters
            /// </summary>
            public EStringEvaluationOperation type;

            /// <summary>
            /// The string of characters that will be involved in the evaluation operation
            /// </summary>
            public string sequence;
        }

        /*----------Variables----------*/
        //VISIBLE

        [Header("Basic Settings")]

        [SerializeField, Tooltip("Flags if logging is enabled for this object")]
        private bool loggingEnabled = true;

        [SerializeField, Tooltip("Defines the minimum log severity that will be reported by this logger (Exceptions will always be reported)")]
        private LogType filterLogLevel = LogType.Log;

        [Header("Blacklist Settings")]

        [SerializeField, Tooltip("The different operations that will be tested against incoming messages to determine if the message should be ignored")]
        private EvaluationOperation[] blacklistEvaluations;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Get and set the Log handler that is assigned for this object to use
        /// </summary>
        public ILogHandler logHandler { get; set; }

        /// <summary>
        /// Get and set logging enabled state for this object
        /// </summary>
        public bool logEnabled { get { return loggingEnabled; } set { loggingEnabled = value; } }

        /// <summary>
        /// Get and set the log filtering level that is used to control logging output
        /// </summary>
        public LogType filterLogType { get { return filterLogLevel; } set { filterLogLevel = value; } }

        /// <summary>
        /// Get and set the blacklist evaluation operations that will be applied to incoming messages
        /// </summary>
        public EvaluationOperation[] blacklistEvaluationOperations { get { return blacklistEvaluations; } set { blacklistEvaluations = value; } }

        /*----------Functions----------*/
        //PRIVATE

        /// <summary>
        /// Assign the default log handler if there is nothing else assigned
        /// </summary>
        private void OnEnable() { if (logHandler == null) logHandler = UnityDebugOverride.DEFAULT_LOGGER.logHandler; }

        /// <summary>
        /// Convert a generic object to a printable string format
        /// </summary>
        /// <param name="message">The message that is to be converted</param>
        /// <returns>Returns a string representation of the object</returns>
        private static string GetString(object message) {
            return (message != null ?
                message.ToString() :
                "Null"
            );
        }

        //PUBLIC

        /// <summary>
        /// Check to see if a log of the specified severity can be used
        /// </summary>
        /// <param name="logType">The log type that is to be checked </param>
        /// <returns>Returns true if the current settings allow for logging the specified message type</returns>
        public bool IsLogTypeAllowed(LogType logType) {
            //If logging is not enabled, cant
            if (!loggingEnabled) return false;

            //Check if the log out of range (But allow exceptions)
            if (logType > filterLogLevel)
                return (logType == LogType.Exception);

            //If this far, it's fine
            return true;
        }

        /// <summary>
        /// Check to see if the supplied message is blacklisted
        /// </summary>
        /// <param name="message">The message that is to be checked for inclusion</param>
        /// <returns>Returns true if the message is blacklisted and should be omitted</returns>
        public bool IsMessageBlacklisted(string message) {
            //Check there is a message to check
            if (string.IsNullOrEmpty(message))
                return false;

            //Make sure there are conditions to check
            if (blacklistEvaluations == null || blacklistEvaluations.Length == 0)
                return false;

            //Check all of the evaluations
            for (int i = 0; i < blacklistEvaluations.Length; ++i) {
                //Check there is text to evaluate
                if (string.IsNullOrEmpty(blacklistEvaluations[i].sequence))
                    continue;

                //Run the required operation
                switch (blacklistEvaluations[i].type) {
                    case EStringEvaluationOperation.Is:
                        if (message == blacklistEvaluations[i].sequence)
                            return true;
                        break;
                    case EStringEvaluationOperation.StartsWith:
                        if (message.StartsWith(blacklistEvaluations[i].sequence))
                            return true;
                        break;
                    case EStringEvaluationOperation.EndsWith:
                        if (message.EndsWith(blacklistEvaluations[i].sequence))
                            return true;
                        break;
                    case EStringEvaluationOperation.Contains:
                        if (message.Contains(blacklistEvaluations[i].sequence))
                            return true;
                        break;
                }
            }

            //If got this far, it's not blacklisted
            return false;
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public void Log(LogType logType, object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Convert the supplied message to a string
            string msg = GetString(message);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(logType, null, msg);
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public void Log(LogType logType, object message, UnityEngine.Object context) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Convert the supplied message to a string
            string msg = GetString(message);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(logType, context, msg);
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public void Log(LogType logType, string tag, object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Convert the supplied message to a string
            string msg = GetString(message);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(logType, null, "{0}: {1}", tag, msg);
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public void Log(LogType logType, string tag, object message, UnityEngine.Object context) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Convert the supplied message to a string
            string msg = GetString(message);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(logType, context, "{0}: {1}", tag, msg);
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public void Log(object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Log)) return;

            //Convert the supplied message to a string
            string msg = GetString(message);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(LogType.Log, null, msg);
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public void Log(string tag, object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Log)) return;

            //Convert the supplied message to a string
            string msg = GetString(message);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(LogType.Log, null, "{0}: {1}", tag, msg);
        }

        /// <summary>
        /// Logs a message to the unity Console using the default logger
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public void Log(string tag, object message, UnityEngine.Object context) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Log)) return;

            //Convert the supplied message to a string
            string msg = GetString(message);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(LogType.Log, context, "{0}: {1}", tag, msg);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an warning message
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public void LogWarning(string tag, object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Warning)) return;

            //Convert the supplied message to a string
            string msg = GetString(message);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(LogType.Warning, null, "{0}: {1}", tag, msg);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an warning message
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public void LogWarning(string tag, object message, UnityEngine.Object context) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Warning)) return;

            //Convert the supplied message to a string
            string msg = GetString(message);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(LogType.Warning, context, "{0}: {1}", tag, msg);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an error message
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        public void LogError(string tag, object message) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Error)) return;

            //Convert the supplied message to a string
            string msg = GetString(message);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(LogType.Error, null, "{0}: {1}", tag, msg);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an error message
        /// </summary>
        /// <param name="tag">Used to identify the source of a log message. It usually identifies the class where the log call occurs</param>
        /// <param name="message">String or object to be converted to a string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public void LogError(string tag, object message, UnityEngine.Object context) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(LogType.Error)) return;

            //Convert the supplied message to a string
            string msg = GetString(message);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(LogType.Error, context, "{0}: {1}", tag, msg);
        }

        /// <summary>
        /// Logs a formatted message
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Arguments to be inserted into the format</param>
        public void LogFormat(LogType logType, string format, params object[] args) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Convert the supplied message to a string
            string msg = string.Format(format, args);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(logType, null, msg);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an exception message
        /// </summary>
        /// <param name="exception">The runtime exception that occurred</param>
        public void LogException(Exception exception) {
            //Check that this object is has enabled logging
            if (!loggingEnabled) return;

            //See if the exception has blacklisted
            if (IsMessageBlacklisted(string.Format("{0}: {1}", exception.GetType().Name, exception.Message))) return;

            //Output the message
            logHandler.LogException(exception, null);
        }

        /// <summary>
        /// Logs a formatted message
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="context">Object to which the message applies</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Arguments to be inserted into the format</param>
        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args) {
            //Check to see if this log is allowed to be processed
            if (!IsLogTypeAllowed(logType)) return;

            //Convert the supplied message to a string
            string msg = string.Format(format, args);

            //Check to make sure the message isn't blacklisted
            if (IsMessageBlacklisted(msg)) return;

            //Output the message
            logHandler.LogFormat(logType, context, msg);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an exception message
        /// </summary>
        /// <param name="exception">The runtime exception that occurred</param>
        /// <param name="context">Object to which the message applies</param>
        public void LogException(Exception exception, UnityEngine.Object context) {
            //Check that this object is has enabled logging
            if (!loggingEnabled) return;

            //See if the exception has blacklisted
            if (IsMessageBlacklisted(string.Format("{0}: {1}", exception.GetType().Name, exception.Message))) return;

            //Output the message
            logHandler.LogException(exception, context);
        }


#if UNITY_EDITOR
        /// <summary>
        /// Manage the custom drawing of a Evaluation Operation object
        /// </summary>
        [CustomPropertyDrawer(typeof(EvaluationOperation))]
        private sealed class EvaluationOperationDrawer : PropertyDrawer {
            /*----------Variables----------*/
            //PRIVATE

            /// <summary>
            /// Store the height that will be used for a single line displayed
            /// </summary>
            private static readonly float LINE_HEIGHT = EditorGUIUtility.singleLineHeight + 2f;

            /*----------Functions----------*/
            //PUBLIC

            /// <summary>
            /// Get the height of that will be used when drawing the property to the inspector
            /// </summary>
            /// <param name="property">The property that is to be displayed</param>
            /// <param name="label">The label that has been assigned to the property</param>
            /// <returns>Returns the height the number of pixels required</returns>
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return LINE_HEIGHT * (property.isExpanded ? 2 : 1); }

            /// <summary>
            /// Draw the supplied property to the inspector 
            /// </summary>
            /// <param name="position">The position that has been allocated for the property to be displayed in</param>
            /// <param name="property">The property that is to be displayed</param>
            /// <param name="label">The label that has been assigned to the property</param>
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                // Using BeginProperty / EndProperty on the parent property means that
                // prefab override logic works on the entire property.
                EditorGUI.BeginProperty(position, label, property);

                //Flag if the property has been expanded
                bool isExpanded = property.isExpanded;

                //Display a foldout for more information
                property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width * .325f, EditorGUIUtility.singleLineHeight), isExpanded, label, true);

                //Check if the additional properties should be displayed
                if (isExpanded) {
                    EditorGUI.PropertyField(
                        new Rect(position.x + position.width * .325f, position.y, position.width * .675f, EditorGUIUtility.singleLineHeight),
                        property.FindPropertyRelative("type"),
                        GUIContent.none
                    );
                    EditorGUI.PropertyField(
                        new Rect(position.x, position.y + LINE_HEIGHT, position.width, EditorGUIUtility.singleLineHeight),
                        property.FindPropertyRelative("sequence"),
                        GUIContent.none
                    );
                }

                EditorGUI.EndProperty();
            }
        }
#endif
    }
}