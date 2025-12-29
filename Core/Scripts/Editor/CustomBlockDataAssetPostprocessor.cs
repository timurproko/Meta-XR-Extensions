using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildingBlocks.Editor
{
    public class CustomBlockDataAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool hasChanges = false;
            
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.EndsWith(".asset"))
                {
                    CustomBlockData blockData = AssetDatabase.LoadAssetAtPath<CustomBlockData>(assetPath);
                    if (blockData != null)
                    {
                        if (CleanTags(blockData))
                        {
                            hasChanges = true;
                        }
                    }
                }
            }
            
            if (hasChanges)
            {
                AssetDatabase.SaveAssets();
            }
        }
        
        
        private static bool CleanTags(CustomBlockData blockData)
        {
            if (blockData == null || blockData.Tags == null)
                return false;

            var validTagNames = TagValidationService.GetValidTagNames();

            var tagsCopy = blockData.Tags.ToList();
            bool hasChanges = false;
            
            foreach (var tag in tagsCopy)
            {
                if (tag == null)
                {
                    blockData.Tags.Remove(tag);
                    hasChanges = true;
                    continue;
                }
                
                string tagName = tag.Name;
                bool shouldRemove = string.IsNullOrWhiteSpace(tagName) ||
                                    !validTagNames.Contains(tagName) ||
                                    tagName == "Internal" ||
                                    tagName == "Hidden";
                
                if (shouldRemove)
                {
                    blockData.Tags.Remove(tag);
                    hasChanges = true;
                }
            }
            
            if (hasChanges)
            {
                EditorUtility.SetDirty(blockData);
            }
            
            return hasChanges;
        }
    }
}

