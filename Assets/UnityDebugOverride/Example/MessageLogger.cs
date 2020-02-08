/******************************************************************************
*                                                                             *
*                             2/8/2020 5:56:44 PM                             *
*                                                                             *
*  They laughed when I said I wanted to be a comedian – they’re not laughing  *
*                                    now.                                     *
*                                                                             *
******************************************************************************/

using System.Collections;

using UnityEngine;

namespace UnityDebugOverride.Example {
    /// <summary>
    /// Output a selection of messages at set intervals
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MessageLogger : MonoBehaviour {
        /*----------Variables----------*/
        //VISIBLE

        [SerializeField, Tooltip("The messages that will be output while this object is active")]
        private string[] messages;

        [SerializeField, Min(float.Epsilon), Tooltip("The amount of time (in seconds) between messages being logged")]
        private float logInterval = 2.5f;

        /*----------Functions----------*/
        //PRIVATE

        /// <summary>
        /// Start the logging operation
        /// </summary>
        private void OnEnable() { StartCoroutine(OutputMessages()); }

        /// <summary>
        /// Log the next message at the interval point
        /// </summary>
        private IEnumerator OutputMessages() {
            //Store the progress through the messages
            int prog = -1;

            //Loop while coroutine is running
            while (true) {
                //Wait for the next interval point
                yield return new WaitForSeconds(logInterval);

                //Wait until there are messages to run
                while (messages == null || messages.Length == 0)
                    yield return null;

                //Log the next message
                Debug.Log(messages[prog = ((prog + 1) % messages.Length)]);
            }
        }
    }
}