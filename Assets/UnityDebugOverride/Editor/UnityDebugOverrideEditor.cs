/******************************************************************************
*                                                                             *
*                            2/7/2020 10:31:49 PM                             *
*                                                                             *
* I heard there was a new store called Moderation. They have everything there *
*                                                                             *
******************************************************************************/

using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace UnityDebugOverride {
    /// <summary>
    /// Manage the displaying of the different options that can be used to override Debug Logger
    /// </summary>
    [CustomEditor(typeof(UnityDebugOverride))]
    public sealed class UnityDebugOverrideEditor : Editor {
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

        /// <summary>
        /// Store the Scriptable Object type for additional editing functionality
        /// </summary>
        private static readonly Type SCRIPTABLE_OBJ_TYPE = typeof(ScriptableObject);

        //INVISIBLE

        /// <summary>
        /// Store a reference to the cached editor that is used to display scriptable objects
        /// </summary>
        private Editor cachedEditor;

        //VISIBLE

        /// <summary>
        /// Store a reference to the properties for the main data elements that can be set for this overrider
        /// </summary>
        private SerializedProperty dontDestroyProp,
                                   usePrevProp,
                                   typeProp,
                                   objProp;

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
                    if (UnityDebugOverride.IsTypeUsable(type))
                        usable.Add(type);
                }
            }

            //Create the final containers
            USABLE_TYPES = new Type[usable.Count];
            TYPE_TO_INDEX = new Dictionary<string, int>(usable.Count);
            TYPE_LABELS = new GUIContent[usable.Count];

            //Categorise all of the stored types
            for (int i = 0; i < usable.Count; ++i) {
                USABLE_TYPES[i] = usable[i];
                TYPE_TO_INDEX[UnityDebugOverride.MinifyTypeAssemblyName(usable[i])] = i;
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
            //Make sure that the serialised properties are valid for use
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

                //Flag if the scriptable object editor should be displayed
                bool displayAssetValues = false;

                //Check if this is a scriptable object for the creation field
                if (currentType != null && SCRIPTABLE_OBJ_TYPE.IsAssignableFrom(currentType)) {
                    //Have the field appear as a single line
                    EditorGUILayout.BeginHorizontal(); {
                        //Display a foldout option for displaying the scriptable object values
                        if (objProp.objectReferenceValue != null) {
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
                            objProp.objectReferenceValue ? GUIContent.none : objectRefLabel
                        );

                        //If there is no object reference assigned, offer a button to create one
                        //Implementation taken from https://gist.github.com/tomkail/ba4136e6aa990f4dc94e0d39ec6a058c
                        if (objProp.objectReferenceValue == null && GUILayout.Button("Create", GUILayout.Width(60f))) {
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

                //Check if changes were made
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();

                //If the scriptable object values should be display, use their default editor
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

            //Otherwise, there is nothing to show (Shrug)
            else EditorGUILayout.LabelField(overrideLabel, noOptionsLabel);
        }
    }
}