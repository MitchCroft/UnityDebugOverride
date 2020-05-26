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

#if UNITY_EDITOR
using UnityEditor;
#endif

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
                                     SCRIPTABLE_OBJ_TYPE        = typeof(ScriptableObject),
                                     MONO_BEHAV_TYPE            = typeof(MonoBehaviour);

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

        [SerializeField, HideInInspector, Tooltip("Flags if the GameObject attached to this object should be flagged as Don't Destroy on Load")]
        private bool flagDontDestroyOnLoad = true;

        [SerializeField, HideInInspector, Tooltip("Flags if the previous Log Handler object in the stack should be used by the new instantiated object")]
        private bool usePreviousLogHandler = true;

        [SerializeField, HideInInspector, Tooltip("The type of the object that should be instantiated to be used as an override to the default debugger")]
        private string overrideType = string.Empty;

        [SerializeField, HideInInspector, Tooltip("An object reference that will be used to hold a reference to a UnityEngine.Object (if the defined type derives from it)")]
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

            //If this was the last object on the stack, update the active instance
            if (ind == activationStack.Count) UpdateAssignedLoggerInstance();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Handle modified inspector values to assign required references while the application is running
        /// </summary>
        private void OnValidate() {
            if (Application.isPlaying && isActiveAndEnabled) {
                //Look for this objects instance on the activation stack
                int ind = activationStack.FindIndex(x => x.overrider == this);

                //If nothing could be found then that's a problem that shouldn't happen
                if (ind == -1) throw new InvalidOperationException(string.Format("Unity Debug Override was unable to find it's override instance on the Activation Stack"));

                //Update the logger instance on the activation stack
                OverridePair over = activationStack[ind];
                over.instance = GetLogger();
                activationStack[ind] = over;

                //If this object is the latest on the stack, update the active index
                if (ind == activationStack.Count - 1)
                    UpdateAssignedLoggerInstance();
            }
        }
#endif

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

            //Store a reference to the ILogger object that will be used
            ILogger instance = null;

            //If this Scriptable Object, use the object reference
            if (UNITY_OBJ_TYPE.IsAssignableFrom(loggerType))
                instance = objectReference as ILogger;

            //Otherwise, try to instantiate a Logger that can be used
            else {
                try {
                    //Check to see if there is a Log handler constructor that should be used
                    if (loggerType.GetConstructor(CONSTRUCTOR_BINDING_FLAGS, null, LOG_HANDLER_CONSTRUCTOR_PARAMS, null) != null) 
                        instance = (ILogger)Activator.CreateInstance(loggerType, (ILogHandler)DEFAULT_LOGGER.logHandler);

                    //Otherwise, use the default constructor
                    else instance = (ILogger)Activator.CreateInstance(loggerType);
                } catch (Exception exec) {
                    Debug.LogErrorFormat(this, "Failed to create a Debug Override instance of '{0}'. ERROR: {1}", loggerType.FullName, exec);
                    return null;
                }
            }

            //Check if the previous Log Handler should be assigned
            if (instance != null && usePreviousLogHandler) {
                //Find the starting index that will be searched back from
                int prog = activationStack.FindIndex(x => x.overrider == this);
                prog = (prog == -1 ? activationStack.Count - 1 : prog - 1);

                //Look for the next active Logger that can be used to supply the log handler
                for (; prog >= 0; --prog) {
                    if (activationStack[prog].instance != null)
                        break;
                }

                //Get the logger that is being used
                ILogger logger = (prog > -1 ?
                    activationStack[prog].instance :
                    DEFAULT_LOGGER
                );

                //Assign the previous Log Handler to the new instance
                instance.logHandler = logger.logHandler;
            }

            //Return the found interface
            return instance;
        }

        //PUBLIC

        /// <summary>
        /// Assign the override type to be used by this object to override the Debug Logging functionality
        /// </summary>
        /// <param name="objectType">The object type that should be used as the override (This object must implement ILogger)</param>
        /// <param name="usePreviousLogHandler">Flags if the previous object in the stacks ILogHandler instance should be assigned to the new instance</param>
        /// <returns>Returns true if the object is able to be used and is assigned correctly</returns>
        /// <remarks>
        /// Types that have a ILogHandler Constructor will be initialised with the default Unity ILogHandler. Use 'useCurrentPreviousLogHandler'
        /// to carry across the ILogHandler object from before it on the stack
        /// 
        /// For Scriptable Objects implementing ILogger, use the <see cref="AssignOverrideObject{T}(T, bool)"/> function
        /// </remarks>
        public bool AssignOverrideType(Type objectType, bool usePreviousLogHandler) {
            //Check that the type can be used
            if (!IsTypeUsable(objectType)) return false;

            //If this type is a Scriptable Object direct the caller to the correct function
            if (SCRIPTABLE_OBJ_TYPE.IsAssignableFrom(objectType))
                throw new ArgumentException(string.Format("UnityDebugOverride.AssignOverrideType(Type, bool) can't accept the type '{0}', as it is a Scriptable Object. Use UnityDebugOverride.AssignOverrideObject(T, bool) instead", objectType.FullName));

            //Assign the updated values for this object
            this.usePreviousLogHandler = usePreviousLogHandler;
            overrideType = MinifyTypeAssemblyName(objectType);
            objectReference = null;

            //If this is at runtime, the instance may need to be updated
            if (
#if UNITY_EDITOR
                Application.isPlaying &&
#endif
                isActiveAndEnabled
                ) {
                //Look for this objects instance on the activation stack
                int ind = activationStack.FindIndex(x => x.overrider == this);

                //If nothing could be found then that's a problem that shouldn't happen
                if (ind == -1) throw new InvalidOperationException(string.Format("Unity Debug Override was unable to find it's override instance on the Activation Stack"));

                //Update the logger instance on the activation stack
                OverridePair over = activationStack[ind];
                over.instance = GetLogger();
                activationStack[ind] = over;

                //If this object is the latest on the stack, update the active index
                if (ind == activationStack.Count - 1)
                    UpdateAssignedLoggerInstance();
            }

            //If got this far, success
            return true;
        }

        /// <summary>
        /// Assign the override object to be used by this object to override the Debug Logging functionality
        /// </summary>
        /// <typeparam name="T">The type of object that is to be used by this object to override</typeparam>
        /// <param name="overrideAsset">The asset that will represent this Override object on the override stack</param>
        /// <param name="usePreviousLogHandler">Flags if the previous object in the stacks ILogHandler instance should be assigned to the new instance</param>
        /// <returns>Returns true if the object is able to be used and is assigned correctly</returns>
        /// <remarks>
        /// Objects assigned that don't apply 'useCurrentPreviousLogHandler' are responsible for assigning their own ILogHandler instance
        /// </remarks>
        public bool AssignOverrideObject<T>(T overrideAsset, bool usePreviousLogHandler) where T : ScriptableObject, ILogger {
            //Get the type that is be assigned
            Type type = typeof(T);

            //Check that the type can be used
            if (!IsTypeUsable(type)) return false;

            //Assign the updated values for this object
            this.usePreviousLogHandler = usePreviousLogHandler;
            overrideType = MinifyTypeAssemblyName(type);
            objectReference = overrideAsset;

            //If this is at runtime, the instance may need to be updated
            if (
#if UNITY_EDITOR
                Application.isPlaying &&
#endif
                isActiveAndEnabled
                ) {
                //Look for this objects instance on the activation stack
                int ind = activationStack.FindIndex(x => x.overrider == this);

                //If nothing could be found then that's a problem that shouldn't happen
                if (ind == -1) throw new InvalidOperationException(string.Format("Unity Debug Override was unable to find it's override instance on the Activation Stack"));

                //Update the logger instance on the activation stack
                OverridePair over = activationStack[ind];
                over.instance = GetLogger();
                activationStack[ind] = over;

                //If this object is the latest on the stack, update the active index
                if (ind == activationStack.Count - 1)
                    UpdateAssignedLoggerInstance();
            }

            //If got this far, success
            return true;
        }

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
                type.IsGenericType)
                return false;

            //Force all UnityEngine.Object types to be Scriptable Objects for ease of management
            if (UNITY_OBJ_TYPE.IsAssignableFrom(type)) {
                if (!SCRIPTABLE_OBJ_TYPE.IsAssignableFrom(type) &&
                    !MONO_BEHAV_TYPE.IsAssignableFrom(type)) {
                    Debug.LogWarningFormat("Unable to include '{0}' as it is a Unity Object that does not inherit ScriptableObject or MonoBehaviour. This is a requirement for management purposes", type.FullName);
                    return false;
                }

                //Otherwise, it's good
                else return true;
            }

            //All non-scriptable objects need to have a supported constructor
            else {
                //Don't allow abstract basic class objects (Abstract base type of Scriptable Objects are fine, they don't have to be dynamically instantiated)
                if (type.IsAbstract) return false;

                //Check to see if this object has a default or ILogHandler constructor that takes a LogHandler
                return (
                    type.GetConstructor(CONSTRUCTOR_BINDING_FLAGS, null, Type.EmptyTypes, null) != null ||
                    type.GetConstructor(CONSTRUCTOR_BINDING_FLAGS, null, LOG_HANDLER_CONSTRUCTOR_PARAMS, null) != null
                );
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Manage the displaying of different options that can be used to override the
        /// default Unity Debug handling behaviour
        /// </summary>
        [CustomEditor(typeof(UnityDebugOverride))]
        private sealed class UnityDebugOverrideEditor : Editor {
            /*----------Variables----------*/
            //SHARED
            
            /// <summary>
            /// Store the collection of type objects that can be used as Debug Log overrides
            /// </summary>
            private static readonly Type[] USABLE_TYPES;

            /// <summary>
            /// Maps the different identified Type objects back to their index
            /// </summary>
            private static readonly Dictionary<string, int> TYPE_TO_INDEX;

            /// <summary>
            /// Stores the different labels that can be used to represent the types
            /// </summary>
            private static readonly GUIContent[] TYPE_LABELS;

            //INVISIBLE

            /// <summary>
            /// Store a reference to the cached editor that is used to display Scriptable Objects assigned
            /// </summary>
            private Editor cachedEditor;

            /// <summary>
            /// Store a reference to the properties for the main data elements that can be set for this overrider
            /// </summary>
            private SerializedProperty dontDestroyProp,
                                       usePrevProp,
                                       typeProp,
                                       objProp;

            /// <summary>
            /// Store the GUI Content elements that will are displayed frequently 
            /// </summary>
            private GUIContent baseHeading,
                               overrideLabel,
                               noOptionsLabel,
                               objectsHeading,
                               objectRefLabel;

            /*----------Functions----------*/
            //PRIVATE

            /// <summary>
            /// Identify the types that can be displayed for use as an override
            /// </summary>
            static UnityDebugOverrideEditor() {
                //Get the currently loaded assemblies
                Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                //Active assembly will be the first to be processed
                Assembly current = Assembly.GetExecutingAssembly();
                Array.Sort(loadedAssemblies, (left, right) => (left == current ? -1 : (right == current ? 1 : 0)));

                //Store a collection of all found types that can be used
                List<Type> usable = new List<Type>();

                //Find all ILogger implementing objects within the application that can be used
                foreach (Assembly assembly in loadedAssemblies) {
                    foreach (Type type in assembly.GetTypes()) {
                        if (IsTypeUsable(type))
                            usable.Add(type);
                    }
                }

                //Do a basic sort of the found types so they are in a somewhat alphabetical order
                usable.Sort((l, r) => l.FullName.CompareTo(r.FullName));

                //Create the final containers
                USABLE_TYPES = new Type[usable.Count];
                TYPE_TO_INDEX = new Dictionary<string, int>(usable.Count);
                TYPE_LABELS = new GUIContent[usable.Count];

                //Categorise all of the stored types
                for (int i = 0; i < usable.Count; ++i) {
                    USABLE_TYPES[i] = usable[i];
                    TYPE_TO_INDEX[
MinifyTypeAssemblyName(usable[i])] = i;
                    TYPE_LABELS[i] = new GUIContent(usable[i].FullName.Replace('+', '/').Replace('.', '/'));
                }
            }

            /// <summary>
            /// Setup the values that can't be serialised for display use
            /// </summary>
            private void EstablishNonSerializable() {
                //Get the serialised property references needed for display
                dontDestroyProp = serializedObject.FindProperty("flagDontDestroyOnLoad");
                usePrevProp = serializedObject.FindProperty("usePreviousLogHandler");
                typeProp = serializedObject.FindProperty("overrideType");
                objProp = serializedObject.FindProperty("objectReference");

                //Create the UI Labels for the display
                baseHeading = new GUIContent("Override Settings");
                overrideLabel = new GUIContent("Override Type", typeProp.tooltip);
                noOptionsLabel = new GUIContent("No ILogger types available");
                objectsHeading = new GUIContent("Override Object Options");
                objectRefLabel = new GUIContent("Object Reference", objProp.tooltip);
            }

            //PUBLIC

            /// <summary>
            /// Render the window UI controls to the display area
            /// </summary>
            public override void OnInspectorGUI() {
                //Make sure that the serialised properties are valid
                if (dontDestroyProp == null)
                    EstablishNonSerializable();

                //Draw the default script field to the inspector
                DrawDefaultInspector();

                //Update the stored properties with the latest information
                serializedObject.UpdateIfRequiredOrScript();


                //Offer the option to toggle Don't Destroy on Load behaviour
                EditorGUI.BeginChangeCheck();

                //Display the basic loading settings that are needed
                EditorGUILayout.LabelField(baseHeading, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(dontDestroyProp, true);
                EditorGUILayout.PropertyField(usePrevProp, true);

                //Add some buffer space between the sections
                EditorGUILayout.Space();

                //Display additional options for Scriptable Objects
                EditorGUILayout.LabelField(objectsHeading, EditorStyles.boldLabel);

                //Get the index of the type that has currently been assigned to the object
                int curInd = (TYPE_TO_INDEX.ContainsKey(typeProp.stringValue) ? TYPE_TO_INDEX[typeProp.stringValue] : -1);

                //Display options select if there are values to pick
                bool displayAssetValues = false;
                if (USABLE_TYPES.Length > 0) {
                    //Display a pop-up to set the type that is used
                    int newInd = EditorGUILayout.Popup(
                        overrideLabel,
                        curInd,
                        TYPE_LABELS
                    );

                    //If the type has been modified, update the stored values
                    if (curInd != newInd) {
                        //Set the new type string
                        typeProp.stringValue = (newInd >= 0 && newInd < USABLE_TYPES.Length ?
                            UnityDebugOverride.MinifyTypeAssemblyName(USABLE_TYPES[newInd]) :
                            string.Empty
                        );

                        //Clear the cached editor reference
                        cachedEditor = null;

                        //Clear any previous object reference
                        objProp.objectReferenceValue = null;
                    }

                    //Get the type object that is being modified
                    Type currentType = (newInd >= 0 && newInd < USABLE_TYPES.Length ? USABLE_TYPES[newInd] : null);

                    //If this is a UnityObject type, need to offer a field to assign a reference
                    if (currentType != null && UNITY_OBJ_TYPE.IsAssignableFrom(currentType)) {
                        //Have all of the field options appear as a single line
                        EditorGUILayout.BeginHorizontal(); {
                            //Flag if this is a scriptable object asset field to display
                            bool isScriptableAsset = objProp.objectReferenceValue != null && SCRIPTABLE_OBJ_TYPE.IsAssignableFrom(currentType);

                            //If this is a scriptable object type and there is an assigned instance, display can be folded out
                            if (isScriptableAsset) {
                                displayAssetValues =
                                objProp.isExpanded = EditorGUILayout.Foldout(
                                    objProp.isExpanded,
                                    objectRefLabel,
                                    true
                                );
                            }

                            //Display the object field that is used for assigning the object
                            EditorGUILayout.ObjectField(
                                objProp,
                                currentType,
                                isScriptableAsset ? GUIContent.none : objectRefLabel
                            );

                            //If there is no assigned reference and this is a scriptable object, allow for the creation of one
                            //Implementation taken from https://gist.github.com/tomkail/ba4136e6aa990f4dc94e0d39ec6a058c
                            if (objProp.objectReferenceValue == null &&
                                SCRIPTABLE_OBJ_TYPE.IsAssignableFrom(currentType) &&
                                !currentType.IsAbstract &&
                                GUILayout.Button("Create", GUILayout.Width(60f))) {
                                //Offer a file save prompt
                                string path = EditorUtility.SaveFilePanelInProject(
                                    string.Format("Create new {0} Instance", currentType.Name),
                                    string.Format("{0}", currentType.Name),
                                    "asset",
                                    string.Format("Choose a name and location for the new '{0}' instance", currentType.Name)
                                );

                                //If a path was chosen, create the asset
                                if (!string.IsNullOrEmpty(path)) {
                                    //Create a new instance of the object
                                    ScriptableObject obj = ScriptableObject.CreateInstance(currentType);

                                    //Save it to the Asset database
                                    AssetDatabase.CreateAsset(obj, path);
                                    AssetDatabase.SaveAssets();
                                    AssetDatabase.Refresh();
                                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                                    //Assign the asset to the object field
                                    objProp.objectReferenceValue = obj;

                                    //Highlight the new asset in the project window
                                    EditorGUIUtility.PingObject(obj);
                                }
                            }
                        } EditorGUILayout.EndHorizontal();
                    }
                }

                //Otherwise, there is nothing to show (Shrug)
                else EditorGUILayout.LabelField(overrideLabel, noOptionsLabel);

                //Check if changes were made
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();

                //Check if the scriptable objects properties should be displayed for editing
                if (displayAssetValues && objProp.objectReferenceValue != null) {
                    //Cache a reference to the Editor required to display this object field
                    CreateCachedEditor(objProp.objectReferenceValue, null, ref cachedEditor);

                    //Increase the indentation for the display
                    ++EditorGUI.indentLevel;

                    //Display the editor GUI for the scriptable object
                    cachedEditor.OnInspectorGUI();

                    //Reset the indentation
                    --EditorGUI.indentLevel;
                }
            }
        }
#endif
    }
}