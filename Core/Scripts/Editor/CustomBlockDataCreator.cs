using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Meta.XR.BuildingBlocks;
using Meta.XR.BuildingBlocks.Editor;
using UnityEditor;
using UnityEngine;

namespace BuildingBlocks.Editor
{
    public static class CustomBlockDataCreator
    {
        private const string BlockDataFolder = "Assets/Tools/BuildingBlocks/BlockData";
        private const string BlockPublicTag = "[BuildingBlock]";

        public static CustomBlockData CreateBlockDataFromPrefab(GameObject prefab, string targetFolder = null)
        {
            if (prefab == null)
            {
                Debug.LogError("Cannot create Building Block: Prefab is null.");
                return null;
            }

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError($"Cannot create Building Block: {prefab.name} is not a prefab asset.");
                return null;
            }

            string folderToUse = string.IsNullOrEmpty(targetFolder) ? BlockDataFolder : targetFolder.Replace('\\', '/');
            
            if (!AssetDatabase.IsValidFolder(folderToUse))
            {
                string[] folderParts = folderToUse.Split('/');
                string currentPath = folderParts[0];
                
                for (int i = 1; i < folderParts.Length; i++)
                {
                    string nextPath = currentPath + "/" + folderParts[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        string parentFolder = currentPath;
                        string folderName = folderParts[i];
                        AssetDatabase.CreateFolder(parentFolder, folderName);
                    }
                    currentPath = nextPath;
                }
            }

            string blockId = Guid.NewGuid().ToString();

            CustomBlockData blockData = ScriptableObject.CreateInstance<CustomBlockData>();
            
            SetBlockDataFields(blockData, prefab, blockId);

            SetPrefabReference(blockData, prefab);

            EnsurePrefabHasBuildingBlock(prefab, blockId);

            string prefabName = prefab.name;
            if (prefabName.StartsWith(BlockPublicTag))
            {
                prefabName = prefabName.Substring(BlockPublicTag.Length).TrimStart();
            }
            string assetName = $"{prefabName}BlockData.asset";
            string assetPath = Path.Combine(folderToUse, assetName).Replace('\\', '/');

            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(blockData, assetPath);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            blockData = AssetDatabase.LoadAssetAtPath<CustomBlockData>(assetPath);
            
            if (blockData != null)
            {
                if (blockData.Tags != null)
                {
                    var tagsToRemove = blockData.Tags.Where(t => 
                        t != null && (t.Name == "Internal" || t.Name == "Hidden" || t.Name == "Custom")).ToList();
                    foreach (var tag in tagsToRemove)
                    {
                        blockData.Tags.Remove(tag);
                    }
                    
                    EditorUtility.SetDirty(blockData);
                    AssetDatabase.SaveAssets();
                }
                
                EditorUtility.SetDirty(blockData);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                CustomBlockDataInjector.InjectCustomBlocks();
                
                Debug.Log($"Created Building Block: {assetPath}");
                Debug.Log($"Block ID: {blockData.Id}");
                Debug.Log($"Block Name: {blockData.BlockName?.Value ?? "null"}");
                Debug.Log($"Is Hidden: {blockData.Hidden}");
                if (blockData.Tags != null)
                {
                    var tagNames = blockData.Tags.Select(t => t != null ? t.Name : "null");
                    Debug.Log($"Tags: {string.Join(", ", tagNames)}");
                }
                else
                {
                    Debug.Log("Tags: null");
                }
            }

            return blockData;
        }

        public static CustomBlockData[] CreateBlockDataFromPrefabs(GameObject[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                Debug.LogWarning("No prefabs provided to create Building Blocks from.");
                return Array.Empty<CustomBlockData>();
            }

            var createdBlocks = new System.Collections.Generic.List<CustomBlockData>();
            
            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var blockData = CreateBlockDataFromPrefab(prefab);
                    if (blockData != null)
                    {
                        createdBlocks.Add(blockData);
                    }
                }
            }

            return createdBlocks.ToArray();
        }

        private static void SetBlockDataFields(CustomBlockData blockData, GameObject prefab, string blockId)
        {
            Type blockDataType = typeof(BlockData);
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

            FieldInfo idField = blockDataType.GetField("id", flags);
            if (idField != null)
            {
                idField.SetValue(blockData, blockId);
            }

            FieldInfo versionField = blockDataType.GetField("version", flags);
            if (versionField != null)
            {
                versionField.SetValue(blockData, 1);
            }

            string prefabName = prefab.name;
            if (prefabName.StartsWith(BlockPublicTag))
            {
                prefabName = prefabName.Substring(BlockPublicTag.Length).TrimStart();
            }
            
            FieldInfo blockNameField = blockDataType.GetField("blockName", flags);
            if (blockNameField != null)
            {
                blockNameField.SetValue(blockData, prefabName);
            }

            FieldInfo descriptionField = blockDataType.GetField("description", flags);
            if (descriptionField != null)
            {
                string description = $"Custom Building Block created from {prefabName} prefab.";
                descriptionField.SetValue(blockData, description);
            }

            FieldInfo orderField = blockDataType.GetField("order", flags);
            if (orderField != null)
            {
                orderField.SetValue(blockData, 0);
            }
        }

        private static void SetPrefabReference(CustomBlockData blockData, GameObject prefab)
        {
            Type blockDataType = typeof(BlockData);
            FieldInfo prefabField = blockDataType.GetField("prefab", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (prefabField != null)
            {
                prefabField.SetValue(blockData, prefab);
            }
            else
            {
                Debug.LogError("Could not set prefab reference: 'prefab' field not found in BlockData.");
            }
        }

        private static void EnsurePrefabHasBuildingBlock(GameObject prefab, string blockId)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                return;
            }

            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;

            BuildingBlock buildingBlock = prefabInstance.GetComponent<BuildingBlock>();
            if (buildingBlock == null)
            {
                buildingBlock = prefabInstance.AddComponent<BuildingBlock>();
                modified = true;
            }

            Type buildingBlockType = typeof(BuildingBlock);
            FieldInfo blockIdField = buildingBlockType.GetField("blockId", BindingFlags.NonPublic | BindingFlags.Instance);
            if (blockIdField != null)
            {
                string currentBlockId = blockIdField.GetValue(buildingBlock) as string;
                if (currentBlockId != blockId)
                {
                    blockIdField.SetValue(buildingBlock, blockId);
                    modified = true;
                }
            }

            FieldInfo versionField = buildingBlockType.GetField("version", BindingFlags.NonPublic | BindingFlags.Instance);
            if (versionField != null)
            {
                versionField.SetValue(buildingBlock, 1);
                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabInstance);
        }
    }
}

