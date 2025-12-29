using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BuildingBlocks.Editor
{
    public static class TagValidationService
    {
        private static HashSet<string> _cachedValidTagNames;
        
        public static HashSet<string> GetValidTagNames()
        {
            if (_cachedValidTagNames != null)
            {
                return _cachedValidTagNames;
            }
            
            var validTagNames = new HashSet<string>();
            
            try
            {
                var customTagBehaviorsType = System.Type.GetType("Meta.XR.BuildingBlocks.Editor.CustomTagBehaviors, Meta.XR.BuildingBlocks.Editor");
                if (customTagBehaviorsType == null)
                {
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        customTagBehaviorsType = assembly.GetType("Meta.XR.BuildingBlocks.Editor.CustomTagBehaviors");
                        if (customTagBehaviorsType != null)
                            break;
                    }
                }
                
                if (customTagBehaviorsType != null)
                {
                    var properties = customTagBehaviorsType.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    
                    foreach (var prop in properties)
                    {
                        if (prop.PropertyType.Name == "Tag" || prop.PropertyType.FullName?.Contains("Tag") == true)
                        {
                            try
                            {
                                var tag = prop.GetValue(null);
                                if (tag != null)
                                {
                                    var nameProperty = tag.GetType().GetProperty("Name");
                                    if (nameProperty != null)
                                    {
                                        var tagName = nameProperty.GetValue(tag) as string;
                                        if (!string.IsNullOrEmpty(tagName))
                                        {
                                            validTagNames.Add(tagName);
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                
                var utilsType = System.Type.GetType("Meta.XR.BuildingBlocks.Editor.Utils, Meta.XR.BuildingBlocks.Editor");
                if (utilsType == null)
                {
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        utilsType = assembly.GetType("Meta.XR.BuildingBlocks.Editor.Utils");
                        if (utilsType != null)
                            break;
                    }
                }
                
                if (utilsType != null)
                {
                    var experimentalTagField = utilsType.GetField("ExperimentalTag", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    var prototypingTagField = utilsType.GetField("PrototypingTag", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    var debugTagField = utilsType.GetField("DebugTag", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    void AddTagFromField(FieldInfo field)
                    {
                        if (field != null)
                        {
                            try
                            {
                                var tag = field.GetValue(null);
                                if (tag != null)
                                {
                                    var nameProperty = tag.GetType().GetProperty("Name");
                                    if (nameProperty != null)
                                    {
                                        var tagName = nameProperty.GetValue(tag) as string;
                                        if (!string.IsNullOrEmpty(tagName))
                                        {
                                            validTagNames.Add(tagName);
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    
                    AddTagFromField(experimentalTagField);
                    AddTagFromField(prototypingTagField);
                    AddTagFromField(debugTagField);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[TagValidationService] Failed to get valid tags from Meta SDK via reflection: {ex.Message}. No tags will be cleaned.");
            }
            
            _cachedValidTagNames = validTagNames;
            
            return validTagNames;
        }
        
        public static void ClearCache()
        {
            _cachedValidTagNames = null;
        }
    }
}

