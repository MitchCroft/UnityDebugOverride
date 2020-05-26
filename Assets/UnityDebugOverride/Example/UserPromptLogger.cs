/******************************************************************************
*                                                                             *
*                            5/26/2020 3:19:03 PM                             *
*                                                                             *
*    "All browsers support the hex definitions #chuck and #norris for the     *
*                           colors black and blue."                           *
*                                                                             *
******************************************************************************/

using UnityEngine;
using UnityEngine.UI;

namespace UnityDebugOverride.Example {
    /// <summary>
    /// Prompt the user whenever they receive an input message to ensure that it is seen
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UserPromptLogger : ALoggerBehaviour {
        /*----------Variables----------*/
        //INVISIBLE

        /// <summary>
        /// Store the progress through the messages to determine when a prompt should be displayed
        /// </summary>
        private int progressCounter = -1;

        //VISIBLE

        [Header("Object References")]

        [SerializeField, Tooltip("The game object that will be enabled whenever a prompt message is to be displayed")]
        private GameObject promptObject;

        [SerializeField, Tooltip("The text component that will be assigned the formatted message for display")]
        private Text textOutput;

        [Header("Operation Settings")]

        [SerializeField, Min(1), Tooltip("Specifies the 1 in 'n' number of messages that will be displayed on the prompt object")]
        private int promptFrequency = 1;

        /*----------Functions----------*/
        //PROTECTED

        /// <summary>
        /// Apply the default logging behaviour of the ILogHandler assigned
        /// </summary>
        /// <param name="logType">The type of log message</param>
        /// <param name="context">Object to which the message applies</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Arguments to be inserted into the format</param>
        protected override void ProcessLog(LogType logType, UnityEngine.Object context, string format, params object[] args) {
            //Check if this message should be logged
            if ((progressCounter = ((progressCounter + 1) % promptFrequency)) == 0) {
                //Display the various elements
                promptObject?.SetActive(true);
                if (textOutput) textOutput.text = (args != null && args.Length > 0 ?
                    string.Format(format, args) :
                    format
                );
            }

            //Log the base elements to the console
            base.ProcessLog(logType, context, format, args);
        }
    }
}