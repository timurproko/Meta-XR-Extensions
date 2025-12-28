using System.Collections.Generic;
using Meta.XR.BuildingBlocks.Editor;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BuildingBlocks.Editor
{
    public class CustomBlockData : BlockData
    {
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (BlockName != null && !string.IsNullOrEmpty(BlockName.Value) && Prefab != null)
            {
                string prefabPath = AssetDatabase.GetAssetPath(Prefab);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefabAsset != null && prefabAsset.name != BlockName.Value)
                    {
                        prefabAsset.name = BlockName.Value;
                        EditorUtility.SetDirty(prefabAsset);
                    }
                }
            }
        }
#endif
    }
}

