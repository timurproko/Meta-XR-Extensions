using System.Linq;
using Meta.XR.BuildingBlocks;
using Meta.XR.BuildingBlocks.Editor;
using UnityEditor;
using UnityEngine;

namespace BuildingBlocks.Editor
{
    [CustomEditor(typeof(BlockData), true)]
    [CanEditMultipleObjects]
    public class CustomBlockDataEditor : BlockDataEditor
    {
        private SerializedProperty _blockName, _description, _thumbnail, _tags, _prefab;
        private SerializedProperty _dependencies, _externalDeps, _packageDeps, _singleton;
        private SerializedProperty _usage, _docName, _docUrl;

        protected override void OnEnable()
        {
            base.OnEnable();
            _blockName = serializedObject.FindProperty("blockName");
            _description = serializedObject.FindProperty("description");
            _thumbnail = serializedObject.FindProperty("thumbnail");
            _tags = serializedObject.FindProperty("tags");
            _prefab = serializedObject.FindProperty("prefab");
            _dependencies = serializedObject.FindProperty("dependencies");
            _externalDeps = serializedObject.FindProperty("externalBlockDependencies");
            _packageDeps = serializedObject.FindProperty("packageDependencies");
            _singleton = serializedObject.FindProperty("isSingleton");
            _usage = serializedObject.FindProperty("usageInstructions");
            _docName = serializedObject.FindProperty("featureDocumentationName");
            _docUrl = serializedObject.FindProperty("featureDocumentationUrl");

            if (target is CustomBlockData block) TagValidationService.CleanInvalidTags(block);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var block = target as CustomBlockData;

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Custom Building Block", MessageType.Info);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_blockName);
            EditorGUILayout.PropertyField(_description);
            EditorGUILayout.PropertyField(_thumbnail);
            EditorGUILayout.PropertyField(_tags, true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (block) TagValidationService.CleanInvalidTags(block);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(_prefab);
            EditorGUILayout.PropertyField(_dependencies, true);
            EditorGUILayout.PropertyField(_externalDeps, true);
            EditorGUILayout.PropertyField(_packageDeps, true);
            EditorGUILayout.PropertyField(_singleton);

            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(_usage);
            EditorGUILayout.PropertyField(_docName);
            EditorGUILayout.PropertyField(_docUrl);

            if (block)
            {
                EditorGUILayout.Space(10);
                bool installed = FindObjectsByType<BuildingBlock>(FindObjectsSortMode.None).Any(b => b.BlockId == block.Id);
                EditorGUILayout.LabelField(installed ? "Installed in Scene" : "Not Installed", EditorStyles.miniLabel);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
