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
    public class BlacklistedLogger : ALoggerAsset {
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

        [Header("Blacklist Settings")]

        [SerializeField, Tooltip("The different operations that will be tested against incoming messages to determine if the message should be ignored")]
        private EvaluationOperation[] blacklistEvaluations;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Get and set the blacklist evaluation operations that will be applied to incoming messages
        /// </summary>
        public EvaluationOperation[] blacklistEvaluationOperations { get { return blacklistEvaluations; } set { blacklistEvaluations = value; } }

        /*----------Functions----------*/
        //PROTECTED

        /// <summary>
        /// Check to see if the message has been blacklisted before writing using the assigned ILogHandler to output the message
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="context">Object to which the message applies</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Arguments to be inserted into the format</param>
        protected override void ProcessLog(LogType logType, UnityEngine.Object context, string format, params object[] args) {
            //Check to see if the message has been blacklisted
            if (IsMessageBlacklisted(args != null && args.Length > 0 ?
                    string.Format(format, args) :
                    format
                ))
                return;

            //Apply the base functionality
            base.ProcessLog(logType, context, format, args);
        }

        /// <summary>
        /// Check to see if the message has been blacklisted before writing using the assigned ILogHandler to output the message
        /// </summary>
        /// <param name="exception">The runtime exception that occurred</param>
        /// <param name="context">Object to which the message applies</param>
        protected override void ProcessException(Exception exception, UnityEngine.Object context) {
            //Check to see if the message has been blacklisted
            if (IsMessageBlacklisted(string.Format("{0}: {1}", exception.GetType().Name, exception.Message)))
                return;

            //Apply the base functionality
            base.ProcessException(exception, context);
        }

        //PUBLIC

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