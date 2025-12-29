using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Meta.XR.BuildingBlocks.Editor;
using UnityEditor;

namespace BuildingBlocks.Editor
{
    [InitializeOnLoad]
    public static class CustomBlockDataInjector
    {
        private static readonly FieldInfo FilteredRegistryField;
        private static readonly MethodInfo MarkDirtyMethod;
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public;

        static CustomBlockDataInjector()
        {
            var utils = FindType("Meta.XR.BuildingBlocks.Editor.Utils");
            FilteredRegistryField = utils?.GetField("_filteredRegistry", Flags);
            MarkDirtyMethod = utils?.GetMethod("MarkFilteredRegistryDirty", Flags);

            EditorApplication.projectChanged += () => EditorApplication.delayCall += InjectCustomBlocks;
            EditorApplication.delayCall += () => EditorApplication.delayCall += InjectCustomBlocks;
        }

        [MenuItem("Meta/Building Blocks/Refresh Building Blocks", false, 200)]
        public static void RefreshCustomBlocks()
        {
            ForceImportAll();
            AssetDatabase.SaveAssets();
            MarkDirty();
            EditorApplication.delayCall += InjectCustomBlocks;
        }

        private static void InjectCustomBlocks()
        {
            try
            {
                ForceImportAll();
                if (FilteredRegistryField == null) return;

                var registry = typeof(BlockBaseData).GetField("Registry", Flags)?.GetValue(null);
                if (registry == null) return;

                var allBlocks = registry.GetType().GetProperty("Values")?.GetValue(registry) as IReadOnlyList<BlockBaseData>;
                var custom = allBlocks?.OfType<CustomBlockData>().ToList();
                if (custom == null || custom.Count == 0) return;

                MarkDirty();

                var utils = FindType("Meta.XR.BuildingBlocks.Editor.Utils");
                var filtered = utils?.GetProperty("FilteredRegistry", Flags)?.GetValue(null) as IReadOnlyList<BlockBaseData>;
                var combined = new List<BlockBaseData>(filtered ?? Array.Empty<BlockBaseData>());

                foreach (var block in custom.Where(b => combined.All(x => x.Id != b.Id)))
                    combined.Add(block);

                FilteredRegistryField.SetValue(null, combined.AsReadOnly());
            }
            catch { }
        }

        private static void ForceImportAll()
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(CustomBlockData)}"))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }

        private static void MarkDirty()
        {
            try { MarkDirtyMethod?.Invoke(null, null); }
            catch { FilteredRegistryField?.SetValue(null, null); }
        }

        private static Type FindType(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(name);
                if (t != null) return t;
            }
            return null;
        }
    }
}
