using Meta.XR.BuildingBlocks;
using Meta.XR.BuildingBlocks.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildingBlocks.Editor
{
    [CustomEditor(typeof(CustomBlockData), true)]
    [CanEditMultipleObjects]
    public class CustomBlockDataEditor : BlockDataEditor
    {
        private SerializedProperty blockNameProperty;
        private SerializedProperty descriptionProperty;
        private SerializedProperty thumbnailProperty;
        private SerializedProperty tagsProperty;
        private SerializedProperty prefabProperty;
        private SerializedProperty dependenciesProperty;
        private SerializedProperty externalBlockDependenciesProperty;
        private SerializedProperty packageDependenciesProperty;
        private SerializedProperty isSingletonProperty;
        private SerializedProperty usageInstructionsProperty;
        private SerializedProperty featureDocumentationNameProperty;
        private SerializedProperty featureDocumentationUrlProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            blockNameProperty = serializedObject.FindProperty("blockName");
            descriptionProperty = serializedObject.FindProperty("description");
            thumbnailProperty = serializedObject.FindProperty("thumbnail");
            tagsProperty = serializedObject.FindProperty("tags");
            prefabProperty = serializedObject.FindProperty("prefab");
            dependenciesProperty = serializedObject.FindProperty("dependencies");
            externalBlockDependenciesProperty = serializedObject.FindProperty("externalBlockDependencies");
            packageDependenciesProperty = serializedObject.FindProperty("packageDependencies");
            isSingletonProperty = serializedObject.FindProperty("isSingleton");
            usageInstructionsProperty = serializedObject.FindProperty("usageInstructions");
            featureDocumentationNameProperty = serializedObject.FindProperty("featureDocumentationName");
            featureDocumentationUrlProperty = serializedObject.FindProperty("featureDocumentationUrl");
            
            var blockData = target as CustomBlockData;
            if (blockData != null && blockData.Tags != null)
            {
                ValidateAndCleanTags(blockData);
                
                EditorApplication.delayCall += () =>
                {
                    if (blockData != null)
                    {
                        ValidateAndCleanTags(blockData);
                        Repaint();
                    }
                };
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var blockData = target as CustomBlockData;
            string oldBlockName = blockData?.BlockName?.Value ?? "";

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Custom Building Block - All fields are editable. " +
                "Changes will be reflected in the Building Blocks window.",
                MessageType.Info);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Information", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(blockNameProperty, new GUIContent("Block Name"));
            EditorGUILayout.PropertyField(descriptionProperty, new GUIContent("Description"));
            EditorGUILayout.PropertyField(thumbnailProperty, new GUIContent("Thumbnail"));
            EditorGUILayout.PropertyField(tagsProperty, new GUIContent("Tags"), true);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                
                if (blockData != null)
                {
                    ValidateAndCleanTags(blockData);
                }
                
                string newBlockName = blockData?.BlockName?.Value ?? "";
                
                if (!string.IsNullOrEmpty(newBlockName) && oldBlockName != newBlockName)
                {
                    UpdateBlockName(blockData, newBlockName);
                }
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(prefabProperty, new GUIContent("Prefab"));
            EditorGUILayout.PropertyField(dependenciesProperty, new GUIContent("Dependencies"), true);
            EditorGUILayout.PropertyField(externalBlockDependenciesProperty, new GUIContent("External Block Dependencies"), true);
            EditorGUILayout.PropertyField(packageDependenciesProperty, new GUIContent("Package Dependencies"), true);
            EditorGUILayout.PropertyField(isSingletonProperty, new GUIContent("Is Singleton"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Additional Information", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(usageInstructionsProperty, new GUIContent("Usage Instructions"));
            EditorGUILayout.PropertyField(featureDocumentationNameProperty, new GUIContent("Feature Documentation Name"));
            EditorGUILayout.PropertyField(featureDocumentationUrlProperty, new GUIContent("Feature Documentation URL"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(10);

            if (blockData != null)
            {
                EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
                var blocksInScene = UnityEngine.Object.FindObjectsByType<Meta.XR.BuildingBlocks.BuildingBlock>(UnityEngine.FindObjectsSortMode.None);
                bool isInstalled = System.Linq.Enumerable.Any(blocksInScene, b => b.BlockId == blockData.Id);
                EditorGUILayout.LabelField(
                    isInstalled ? "Installed in Scene" : "Not Installed",
                    isInstalled ? EditorStyles.helpBox : EditorStyles.miniLabel);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateBlockName(CustomBlockData blockData, string newBlockName)
        {
            if (blockData == null || string.IsNullOrEmpty(newBlockName))
                return;

            string assetPath = AssetDatabase.GetAssetPath(blockData);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string directory = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
                string extension = System.IO.Path.GetExtension(assetPath);
                string newAssetName = SanitizeFileName(newBlockName) + extension;
                
                string currentFileName = System.IO.Path.GetFileName(assetPath);
                if (currentFileName != newAssetName)
                {
                    string newAssetPath = directory + "/" + newAssetName;
                    newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);
                    
                    string error = AssetDatabase.RenameAsset(assetPath, System.IO.Path.GetFileNameWithoutExtension(newAssetPath));
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogWarning($"[CustomBlockDataEditor] Could not rename asset: {error}");
                    }
                    else
                    {
                        Debug.Log($"[CustomBlockDataEditor] Renamed asset to match BlockName: {newBlockName}");
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            if (blockData.Prefab != null)
            {
                string prefabPath = AssetDatabase.GetAssetPath(blockData.Prefab);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    string directory = System.IO.Path.GetDirectoryName(prefabPath).Replace('\\', '/');
                    string extension = System.IO.Path.GetExtension(prefabPath);
                    string newPrefabName = SanitizeFileName(newBlockName) + extension;
                    
                    string currentPrefabFileName = System.IO.Path.GetFileName(prefabPath);
                    if (currentPrefabFileName != newPrefabName)
                    {
                        string newPrefabPath = directory + "/" + newPrefabName;
                        newPrefabPath = AssetDatabase.GenerateUniqueAssetPath(newPrefabPath);
                        
                        string error = AssetDatabase.RenameAsset(prefabPath, System.IO.Path.GetFileNameWithoutExtension(newPrefabPath));
                        if (!string.IsNullOrEmpty(error))
                        {
                            Debug.LogWarning($"[CustomBlockDataEditor] Could not rename prefab: {error}");
                        }
                        else
                        {
                            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath);
                            if (prefabAsset != null && prefabAsset.name != newBlockName)
                            {
                                prefabAsset.name = newBlockName;
                                EditorUtility.SetDirty(prefabAsset);
                                AssetDatabase.SaveAssets();
                                Debug.Log($"[CustomBlockDataEditor] Renamed prefab and GameObject to match BlockName: {newBlockName}");
                            }
                        }
                    }
                }
            }

            assetPath = AssetDatabase.GetAssetPath(blockData);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            
            CustomBlockDataInjector.RefreshCustomBlocks();
        }

        
        private void ValidateAndCleanTags(CustomBlockData blockData)
        {
            if (blockData == null || blockData.Tags == null || tagsProperty == null)
                return;

            serializedObject.Update();

            HashSet<string> validTagNames = TagValidationService.GetValidTagNames();

            Undo.RecordObject(blockData, "Clean Invalid Tags");
            
            var tagsCopy = blockData.Tags.ToList();
            bool hasChanges = false;
            int removedCount = 0;
            
            foreach (var tag in tagsCopy)
            {
                if (tag == null)
                {
                    blockData.Tags.Remove(tag);
                    hasChanges = true;
                    removedCount++;
                    Debug.Log($"[CustomBlockDataEditor] Removed invalid tag: (null)");
                    continue;
                }
                
                string tagName = tag.Name;
                if (string.IsNullOrWhiteSpace(tagName) ||
                    !validTagNames.Contains(tagName) ||
                    tagName == "Internal" ||
                    tagName == "Hidden")
                {
                    blockData.Tags.Remove(tag);
                    hasChanges = true;
                    removedCount++;
                    Debug.Log($"[CustomBlockDataEditor] Removed invalid tag: {tagName ?? "(empty)"}");
                }
            }

            if (hasChanges)
            {
                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();
                
                EditorUtility.SetDirty(blockData);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log($"[CustomBlockDataEditor] Cleaned {removedCount} invalid tag(s) from {blockData.name}");
                
                Repaint();
                
                serializedObject.Update();
            }
        }

        private string SanitizeFileName(string fileName)
        {
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            string sanitized = fileName;
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            return sanitized.Trim();
        }
    }
}

