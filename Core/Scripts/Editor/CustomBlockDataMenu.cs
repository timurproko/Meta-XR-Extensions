using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildingBlocks.Editor
{
    public static class CustomBlockDataMenu
    {
        private const string ContextMenuPath = "Assets/Create/Meta/Create Building Block";

        [MenuItem(ContextMenuPath, false, 1)]
        public static void CreateBuildingBlock()
        {
            string targetFolder = "Assets";
            
            Object selectedObject = Selection.activeObject;
            if (selectedObject != null)
            {
                string selectedPath = AssetDatabase.GetAssetPath(selectedObject);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (AssetDatabase.IsValidFolder(selectedPath))
                    {
                        targetFolder = selectedPath;
                    }
                    else
                    {
                        targetFolder = System.IO.Path.GetDirectoryName(selectedPath).Replace('\\', '/');
                    }
                }
            }
            else
            {
                string[] guids = Selection.assetGUIDs;
                if (guids != null && guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (AssetDatabase.IsValidFolder(path))
                        {
                            targetFolder = path;
                        }
                        else
                        {
                            targetFolder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                        }
                    }
                }
            }

            CustomBlockData blockData = ScriptableObject.CreateInstance<CustomBlockData>();
            
            string assetName = "New Building Block.asset";
            string assetPath = System.IO.Path.Combine(targetFolder, assetName).Replace('\\', '/');
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            
            AssetDatabase.CreateAsset(blockData, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            blockData = AssetDatabase.LoadAssetAtPath<CustomBlockData>(assetPath);
            
            if (blockData != null)
            {
                var serializedObject = new SerializedObject(blockData);
                serializedObject.Update();
                
                var blockNameProperty = serializedObject.FindProperty("blockName");
                var descriptionProperty = serializedObject.FindProperty("description");
                
                if (blockNameProperty != null)
                {
                    blockNameProperty.stringValue = "New Building Block";
                }
                
                if (descriptionProperty != null)
                {
                    descriptionProperty.stringValue = "A custom Building Block created from a prefab.";
                }
                
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(blockData);
                AssetDatabase.SaveAssets();
                
                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
                
                blockData = AssetDatabase.LoadAssetAtPath<CustomBlockData>(assetPath);
                
                if (blockData != null)
                {
                    EditorUtility.SetDirty(blockData);
                    AssetDatabase.SaveAssets();
                }
            }
            
            if (blockData != null && blockData.Tags != null)
            {
                CleanInvalidTags(blockData);
                EditorUtility.SetDirty(blockData);
                AssetDatabase.SaveAssets();
            }
            
            EditorApplication.delayCall += () =>
            {
                var window = EditorWindow.GetWindow<CustomBlockDataWindow>(false);
                if (window != null)
                {
                    window.RefreshBlockList();
                }
            };
            
            Selection.activeObject = blockData;
            EditorGUIUtility.PingObject(blockData);
        }
        
        
        private static void CleanInvalidTags(CustomBlockData blockData)
        {
            if (blockData == null || blockData.Tags == null)
                return;

            var validTagNames = TagValidationService.GetValidTagNames();

            bool hasChanges = false;
            
            var tagsToRemove = blockData.Tags.Where(t => 
            {
                if (t == null)
                    return true;
                    
                string tagName = t.Name;
                return string.IsNullOrWhiteSpace(tagName) ||
                       !validTagNames.Contains(tagName) ||
                       tagName == "Internal" ||
                       tagName == "Hidden";
            }).ToArray();
            
            foreach (var tag in tagsToRemove)
            {
                blockData.Tags.Remove(tag);
                hasChanges = true;
            }
            
            if (hasChanges)
            {
                UnityEditor.EditorUtility.SetDirty(blockData);
            }
        }

    }
}


