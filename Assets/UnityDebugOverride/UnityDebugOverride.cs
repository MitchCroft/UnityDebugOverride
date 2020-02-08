/******************************************************************************
*                                                                             *
*                            2/7/2020 10:31:23 PM                             *
*                                                                             *
*                "Chuck Norris programs do not accept input."                 *
*                                                                             *
******************************************************************************/

using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

namespace UnityDebugOverride {
    /// <summary>
    /// Manage the application of custom Debug logging overwrites to the Unity default system 
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnityDebugOverride : MonoBehaviour {
        /*----------Types----------*/
        //PRIVATE

        /// <summary>
        /// Store the combination of values that are applied as overrides to the log system
        /// </summary>
        private struct OverridePair {
            /// <summary>
            /// The DebugOverride behaviour that has applied the changes
            /// </summary>
            public UnityDebugOverride overrider;

            /// <summary>
            /// The ILogger instance that has been applied to override the debug handling
            /// </summary>
            public ILogger instance;
        }

        /*----------Variables----------*/
        //CONST

        /// <summary>
        /// The name of the field that will be modified during operation
        /// </summary>
        private const string LOGGER_FIELD_NAME = "s_Logger";

        /// <summary>
        /// Store the binding flags that will be used when searching for object constructors
        /// </summary>
        private const BindingFlags CONSTRUCTOR_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public;

        /// <summary>
        /// Store a reference to the logger field that can be updated during operations
        /// </summary>
        private static readonly FieldInfo LOGGER_FIELD;

        /// <summary>
        /// Store various type objects that are used to test the validity of the objects used
        /// </summary>
        private static readonly Type LOGGER_INTERFACE_TYPE      = typeof(ILogger),
                                     LOG_HANDLER_INTERFACE_TYPE = typeof(ILogHandler),
                                     UNITY_OBJ_TYPE             = typeof(UnityEngine.Object),
                                     SCRIPTABLE_OBJ_TYPE        = typeof(ScriptableObject);

        /// <summary>
        /// Store a reusable array of type objects that can be used when looking for the required constructors
        /// </summary>
        private static readonly Type[] LOG_HANDLER_CONSTRUCTOR_PARAMS = new Type[] { typeof(ILogHandler) };

        //SHARED

        /// <summary>
        /// Create a pseudo-stack that is used to monitor the application of debug override objects
        /// </summary>
        private static List<OverridePair> activationStack;

        //VISIBLE

        [SerializeField, Tooltip("Flags if the GameObject attached to this object should be flagged as Don't Destroy on Load")]
        private bool flagDontDestroyOnLoad = true;

        [SerializeField, Tooltip("The type of the object that should be instantiated to be used as an override to the default debugger")]
        private string overrideType = string.Empty;

        [SerializeField, Tooltip("An object reference that will be used to hold a reference to a Scriptable Object (if the defined type derives from it)")]
        private UnityEngine.Object objectReference;

        //PUBLIC

        /// <summary>
        /// Store the default Logger that can be returned to when all overrides are removed
        /// </summary>
        public static readonly ILogger DEFAULT_LOGGER;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Store a reference to the Logger instance that is being used as the logger
        /// </summary>
        public ILogger logger { get; private set; }

        /*----------Functions----------*/
        //PRIVATE

        /// <summary>
        /// Retrieve the initial debug logging information that can can be used
        /// </summary>
        static UnityDebugOverride() {
            //Create the 'stack' container
            activationStack = new List<OverridePair>();

            //Get the type of the 'Debug' for reflection
            Type debugType = typeof(Debug);

            //Look for the field value that is be set
            LOGGER_FIELD = debugType.GetField(
                LOGGER_FIELD_NAME,
                BindingFlags.Static | BindingFlags.NonPublic
            );

            //If the field was not found, something went wrong
            if (LOGGER_FIELD == null)
                Debug.LogErrorFormat("Failed to find the logger field for override under the name '{0}'. Can't override", LOGGER_FIELD_NAME);
            else if (!typeof(ILogger).IsAssignableFrom(LOGGER_FIELD.FieldType)) {
                Debug.LogErrorFormat("Identified logger field '{0}' is not an ILogger field. Can't override", LOGGER_FIELD_NAME);
                LOGGER_FIELD = null;
            }

            //Stash the initial debug logger object
            DEFAULT_LOGGER = Debug.unityLogger;
        }

        /// <summary>
        /// Check to see if this object should be prevented from being destroyed on scene load
        /// <summary>
        private void Awake() { if (flagDontDestroyOnLoad) DontDestroyOnLoad(gameObject); }

        /// <summary>
        /// Setup references that are required when this this object is enabled
        /// </summary>
        private void OnEnable() {
            //Get the logger instance that is being used by this object
            logger = GetLogger();

            //Create an entry for this component on the stack
            activationStack.Add(new OverridePair { overrider = this, instance = logger });

            //Update the assigned logger stack
            UpdateAssignedLoggerInstance();
        }

        /// <summary>
        /// Clear references that are not required when this object is disabled
        /// </summary>
        private void OnDisable() {
            //Clear the logger instance
            logger = null;

            //Find this components entry in the 'stack'
            int ind = activationStack.FindIndex(x => x.overrider == this);

            //If nothing could be found something has gone very wrong
            if (ind == -1) {
                Debug.LogErrorFormat(this, "{0} object was deactivated but it's instance could not be found in the activation stack. This should not happen", this);
                return;
            }

            //Remove the entry from the stack
            activationStack.RemoveAt(ind);

            //Update the assigned logger stack
            UpdateAssignedLoggerInstance();
        }

        /// <summary>
        /// Retrieve the Logger object that will be used as the Logger for this object
        /// </summary>
        /// <returns>Returns a reference to the ILogger to be used or null if not usable</returns>
        private ILogger GetLogger() {
            //Check there is a type to be used
            if (string.IsNullOrEmpty(overrideType))
                return null;

            //Try to create a type from the override string
            Type loggerType = Type.GetType(overrideType, false);

            //Check that the type is valid for use
            if (!IsTypeUsable(loggerType)) return null;

            //If this Scriptable Object, use the object reference
            if (SCRIPTABLE_OBJ_TYPE.IsAssignableFrom(loggerType))
                return objectReference as ILogger;

            //Otherwise, try to instantiate a Logger that can be used
            else {
                try {
                    //Check to see if there is a Log handler constructor that should be used
                    if (loggerType.GetConstructor(CONSTRUCTOR_BINDING_FLAGS, null, LOG_HANDLER_CONSTRUCTOR_PARAMS, null) != null)
                        return (ILogger)Activator.CreateInstance(loggerType, Debug.unityLogger.logHandler);

                    //Otherwise, use the default constructor
                    return (ILogger)Activator.CreateInstance(loggerType);
                } catch (Exception exec) {
                    Debug.LogErrorFormat(this, "Failed to create a Debug Override instance of '{0}'. ERROR: {1}", loggerType.FullName, exec);
                    return null;
                }
            }
        }

        //PUBLIC

        /// <summary>
        /// Process the activation stack to assign the currently active Logger object
        /// </summary>
        public static void UpdateAssignedLoggerInstance() {
            //Ensure that the field has been found and can be set
            if (LOGGER_FIELD == null) {
                Debug.LogError("Unity Debug Override doesn't have a reference to the logger field, can't assign override loggers");
                return;
            }

            //Find the first usable logger object on the stack
            int prog = activationStack.Count - 1;
            for (; prog >= 0; --prog) {
                //Check to see if this logger instance is valid
                if (activationStack[prog].instance != null)
                    break;
            }

            //Get the logger that will be assigned
            ILogger logger = (prog > -1 ?
                activationStack[prog].instance :
                DEFAULT_LOGGER
            );

            //At no point should the logger assigned be null
            if (logger == null) throw new NullReferenceException("Unity Debug Override can't assign a null reference to the active logger");

            //Assign the logger that will be used
            try { LOGGER_FIELD.SetValue(null, logger); } catch (Exception exec) {
                Debug.LogErrorFormat("Unity Debug Override failed to update the logger to a '{0}' object. ERROR: {1}", logger, exec);
            }
        }

        /// <summary>
        /// Trim down the supplied type assembly name for simple storage
        /// </summary>
        /// <param name="type">A type defintion that is to be trimmed down to its basic information</param>
        /// <returns>Returns the supplied string without additional assembly information</returns>
        /// <remarks>
        /// This function is intended to handle strings produced by the Type.AssemblyQualifiedName property
        /// 
        /// Implementation is taken from the deserialized Argument Cache object within Unity
        /// Reference document https://github.com/jamesjlinden/unity-decompiled/blob/master/UnityEngine/UnityEngine/Events/ArgumentCache.cs
        /// </remarks>
        public static string MinifyTypeAssemblyName(Type type) {
            //Check that there is a unity object type name to clean
            if (type == null) return string.Empty;

            //Get the assembly name of the object
            string typeAssembly = type.AssemblyQualifiedName;

            //Find the point to cut off the type definition
            int point = int.MaxValue;

            //Find the points that are usually included within an assembly type name
            int buffer = typeAssembly.IndexOf(", Version=");
            if (buffer != -1) point = Math.Min(point, buffer);
            buffer = typeAssembly.IndexOf(", Culture=");
            if (buffer != -1) point = Math.Min(point, buffer);
            buffer = typeAssembly.IndexOf(", PublicKeyToken=");
            if (buffer != -1) point = Math.Min(point, buffer);

            //If nothing was found, type is fine
            if (point == int.MaxValue) return typeAssembly;

            //Substring the type to give the shortened version
            return typeAssembly.Substring(0, point);
        }

        /// <summary>
        /// Check to see if the supplied type is able to be used as a logger override
        /// </summary>
        /// <param name="type">The type object that describes the object that is being tested</param>
        /// <returns>Returns true if the specific type can be used</returns>
        public static bool IsTypeUsable(Type type) {
            //Check to see if the type is a valid object
            if (type == null) return false;

            //Check that this type is assignable from ILogger
            if (!LOGGER_INTERFACE_TYPE.IsAssignableFrom(type)) return false;

            //Check that it is a concrete class that can be used
            if (type.IsInterface ||
                type.IsAbstract ||
                type.IsGenericType)
                return false;

            //Force all UnityEngine.Object types to be Scriptable Objects for ease of management
            if (UNITY_OBJ_TYPE.IsAssignableFrom(type)) {
                if (!SCRIPTABLE_OBJ_TYPE.IsAssignableFrom(type)) {
                    Debug.LogWarningFormat("Unable to include '{0}' as it is a Unity Object that does not inherit ScriptableObject. This is a requirement for management purposes", type.FullName);
                    return false;
                }

                //Otherwise, it's good
                else return true;
            }

            //All non-scriptable objects need to have a supported constructor
            else {
                //Check to see if this object has a constructor that takes a LogHandler
                if (type.GetConstructor(CONSTRUCTOR_BINDING_FLAGS, null, LOG_HANDLER_CONSTRUCTOR_PARAMS, null) != null)
                    return true;

                //Otherwise, this object must have a default constructor
                return (type.GetConstructor(CONSTRUCTOR_BINDING_FLAGS, null, Type.EmptyTypes, null) != null);
            }
        }
    }
}