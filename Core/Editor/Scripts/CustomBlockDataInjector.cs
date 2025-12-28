using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Meta.XR.BuildingBlocks.Editor;
using UnityEditor;
using UnityEngine;

namespace BuildingBlocks.Editor
{
    [InitializeOnLoad]
    public static class CustomBlockDataInjector
    {
        private static FieldInfo _filteredRegistryField;
        private static MethodInfo _markFilteredRegistryDirtyMethod;

        static CustomBlockDataInjector()
        {
            Type utilsType = Type.GetType("Meta.XR.BuildingBlocks.Editor.Utils, Meta.XR.BuildingBlocks.Editor");
            
            if (utilsType != null)
            {
                _filteredRegistryField = utilsType.GetField("_filteredRegistry", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                
                _markFilteredRegistryDirtyMethod = utilsType.GetMethod("MarkFilteredRegistryDirty", 
                    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
            }

            try
            {
                var registryField = typeof(BlockBaseData).GetField("Registry", 
                    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                if (registryField != null)
                {
                    var registry = registryField.GetValue(null);
                    if (registry != null)
                    {
                        var onRefreshEvent = registry.GetType().GetEvent("OnRefresh");
                        if (onRefreshEvent != null)
                        {
                            var handlerType = onRefreshEvent.EventHandlerType;
                            var handlerMethod = typeof(CustomBlockDataInjector).GetMethod("OnRegistryRefresh", 
                                BindingFlags.NonPublic | BindingFlags.Static);
                            if (handlerMethod != null)
                            {
                                var handler = Delegate.CreateDelegate(handlerType, handlerMethod);
                                onRefreshEvent.AddMethod.Invoke(registry, new object[] { handler });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            
            EditorApplication.projectChanged += OnProjectChanged;
            
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += InjectCustomBlocks;
            };
        }

        private static void OnRegistryRefresh(object registry)
        {
            InjectCustomBlocks();
        }

        private static void OnProjectChanged()
        {
            EditorApplication.delayCall += InjectCustomBlocks;
        }

        public static void InjectCustomBlocks()
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets($"t:{typeof(CustomBlockData).Name}");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                
                var blockBaseDataType = typeof(BlockBaseData);
                var registryField = blockBaseDataType.GetField("Registry", 
                    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                
                if (registryField == null)
                {
                    return;
                }
                
                var registry = registryField.GetValue(null);
                if (registry == null)
                {
                    return;
                }
                
                var valuesProperty = registry.GetType().GetProperty("Values");
                if (valuesProperty == null)
                {
                    return;
                }
                
                var allBlocks = valuesProperty.GetValue(registry) as IReadOnlyList<BlockBaseData>;
                if (allBlocks == null)
                {
                    return;
                }
                
                var customBlocks = allBlocks.OfType<CustomBlockData>().ToList();

                if (customBlocks.Count == 0)
                {
                    return;
                }

                if (_filteredRegistryField == null)
                {
                    return;
                }

                MarkFilteredRegistryDirty();
                
                var utilsType = Type.GetType("Meta.XR.BuildingBlocks.Editor.Utils, Meta.XR.BuildingBlocks.Editor");
                if (utilsType != null)
                {
                    var filteredRegistryProperty = utilsType.GetProperty("FilteredRegistry", 
                        BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                    
                    if (filteredRegistryProperty != null)
                    {
                        var filtered = filteredRegistryProperty.GetValue(null) as IReadOnlyList<BlockBaseData>;
                        
                        var customInFiltered = filtered?.OfType<CustomBlockData>().ToList() ?? new List<CustomBlockData>();
                        
                        if (customInFiltered.Count < customBlocks.Count)
                        {
                            var combinedList = new List<BlockBaseData>(filtered ?? Enumerable.Empty<BlockBaseData>());
                            
                            foreach (var customBlock in customBlocks)
                            {
                                if (!combinedList.Any(b => b.Id == customBlock.Id))
                                {
                                    combinedList.Add(customBlock);
                                }
                            }

                            _filteredRegistryField.SetValue(null, combinedList.AsReadOnly());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static void MarkFilteredRegistryDirty()
        {
            try
            {
                if (_markFilteredRegistryDirtyMethod != null)
                {
                    _markFilteredRegistryDirtyMethod.Invoke(null, null);
                }
                else
                {
                    if (_filteredRegistryField != null)
                    {
                        _filteredRegistryField.SetValue(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        [MenuItem("Meta/Building Blocks/Refresh Building Blocks", false, 200)]
        public static void RefreshCustomBlocks()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(CustomBlockData).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            MarkFilteredRegistryDirty();
            
            EditorApplication.delayCall += () =>
            {
                InjectCustomBlocks();
                
                try
                {
                    var windowType = Type.GetType("Meta.XR.BuildingBlocks.Editor.BuildingBlocksWindow, Meta.XR.BuildingBlocks.Editor");
                    if (windowType != null)
                    {
                        var windows = Resources.FindObjectsOfTypeAll(windowType);
                        foreach (var window in windows)
                        {
                            if (window is EditorWindow editorWindow)
                            {
                                var refreshMethod = windowType.GetMethod("Refresh", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                if (refreshMethod != null)
                                {
                                    refreshMethod.Invoke(window, null);
                                }
                                editorWindow.Repaint();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            };
        }
    }
}

