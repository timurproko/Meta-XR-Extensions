using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BuildingBlocks.Editor
{
    public class CustomBlockDataWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<CustomBlockData> customBlocks;
        private bool needsRefresh = true;

        [MenuItem("Meta/Building Blocks/Manage Building Blocks", false, 100)]
        public static void ShowWindow()
        {
            CustomBlockDataWindow window = GetWindow<CustomBlockDataWindow>("Custom Building Blocks");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshBlockList();
        }

        private void OnProjectChange()
        {
            needsRefresh = true;
        }

        private void OnGUI()
        {
            if (needsRefresh)
            {
                RefreshBlockList();
                needsRefresh = false;
            }

            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "This window shows all custom Building Blocks created from your prefabs. " +
                "These blocks will appear in the Meta XR SDK Building Blocks window.",
                MessageType.Info);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField($"Found {customBlocks.Count} custom Building Block(s)", EditorStyles.miniLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshBlockList();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (customBlocks.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No custom Building Blocks found.\n\n" +
                    "To create a Building Block:\n" +
                    "1. Select Project window\n" +
                    "2. Right-click and choose Create > Meta > Create Building Block",
                    MessageType.Info);
            }
            else
            {
                foreach (var blockData in customBlocks)
                {
                    if (blockData == null)
                    {
                        continue;
                    }

                    DrawBlockItem(blockData);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawBlockItem(CustomBlockData blockData)
        {
            Rect itemRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            Texture2D thumbnail = blockData.Thumbnail;
            if (thumbnail != null)
            {
                GUILayout.Label(thumbnail, GUILayout.Width(64), GUILayout.Height(64), GUILayout.ExpandHeight(false));
            }
            else
            {
                GUILayout.Box("", GUILayout.Width(64), GUILayout.Height(64), GUILayout.ExpandHeight(false));
            }

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            
            EditorGUILayout.LabelField($"ID: {blockData.Id}", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            
            string blockName = blockData.BlockName?.Value;
            if (string.IsNullOrEmpty(blockName))
            {
                var blockNameField = blockData.GetType().GetField("blockName", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (blockNameField != null)
                {
                    blockName = blockNameField.GetValue(blockData) as string;
                }
            }
            
            if (!string.IsNullOrEmpty(blockName))
            {
                EditorGUILayout.LabelField(blockName, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            }
            
            string description = blockData.Description?.Value;
            if (string.IsNullOrEmpty(description))
            {
                var descriptionField = blockData.GetType().GetField("description", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (descriptionField != null)
                {
                    description = descriptionField.GetValue(blockData) as string;
                }
            }
            
            if (!string.IsNullOrEmpty(description))
            {
                Rect descriptionRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2, EditorStyles.wordWrappedMiniLabel);
                EditorGUI.LabelField(descriptionRect, description, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            itemRect = GUILayoutUtility.GetLastRect();
            
            if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    Selection.activeObject = blockData;
                    EditorGUIUtility.PingObject(blockData);
                    Event.current.Use();
                    GUI.changed = true;
                }
            }
            
            if (itemRect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(itemRect, MouseCursor.Link);
            }
            
            EditorGUILayout.Space(5);
        }

        public void RefreshBlockList()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(CustomBlockData).Name}");
            customBlocks = new List<CustomBlockData>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CustomBlockData blockData = AssetDatabase.LoadAssetAtPath<CustomBlockData>(path);
                if (blockData != null)
                {
                    customBlocks.Add(blockData);
                }
            }

            customBlocks = customBlocks.OrderBy(b => b.BlockName.Value).ToList();
        }
    }
}

