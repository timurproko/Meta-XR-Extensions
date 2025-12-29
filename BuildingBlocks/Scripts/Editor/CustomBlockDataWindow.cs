using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildingBlocks.Editor
{
    public class CustomBlockDataWindow : EditorWindow
    {
        private Vector2 _scroll;
        private List<CustomBlockData> _blocks = new();

        [MenuItem("Meta/Building Blocks/Manage Building Blocks", false, 100)]
        public static void ShowWindow() => GetWindow<CustomBlockDataWindow>("Custom Building Blocks").Show();

        private void OnEnable() => Refresh();
        private void OnProjectChange() => Refresh();

        private void Refresh()
        {
            _blocks = AssetDatabase.FindAssets($"t:{nameof(CustomBlockData)}")
                .Select(g => AssetDatabase.LoadAssetAtPath<CustomBlockData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(b => b && b.BlockName != null)
                .OrderBy(b => b.BlockName.Value)
                .ToList();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{_blocks.Count} Block(s)", EditorStyles.miniLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(60))) Refresh();
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var block in _blocks.Where(b => b))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                if (block.Thumbnail) GUILayout.Label(block.Thumbnail, GUILayout.Width(48), GUILayout.Height(48));
                else GUILayout.Box("", GUILayout.Width(48), GUILayout.Height(48));

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(block.BlockName?.Value ?? "Unnamed", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(block.Description?.Value ?? "", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    Selection.activeObject = block;
                    Event.current.Use();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
