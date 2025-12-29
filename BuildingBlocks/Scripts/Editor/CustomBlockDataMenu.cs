using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildingBlocks.Editor
{
    public static class CustomBlockDataMenu
    {
        [MenuItem("Assets/Create/Meta/Create Building Block", false, 1)]
        public static void CreateBuildingBlock()
        {
            var folder = GetSelectedFolder();
            var block = ScriptableObject.CreateInstance<CustomBlockData>();
            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, "New Building Block.asset").Replace('\\', '/'));

            AssetDatabase.CreateAsset(block, path);
            block = AssetDatabase.LoadAssetAtPath<CustomBlockData>(path);

            var so = new SerializedObject(block);
            so.FindProperty("blockName").stringValue = "New Building Block";
            so.FindProperty("description").stringValue = "A custom Building Block.";
            so.ApplyModifiedProperties();

            TagValidationService.CleanInvalidTags(block);
            EditorUtility.SetDirty(block);
            AssetDatabase.SaveAssets();

            Selection.activeObject = block;
            EditorGUIUtility.PingObject(block);
        }

        private static string GetSelectedFolder()
        {
            if (Selection.activeObject)
            {
                var path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(path))
                    return AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
            }
            return "Assets";
        }
    }
}
