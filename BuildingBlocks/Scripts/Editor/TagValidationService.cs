using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace BuildingBlocks.Editor
{
    public static class TagValidationService
    {
        private static readonly string[] ForbiddenTags = { "Internal", "Hidden", "Custom" };
        private static HashSet<string> _validTags;

        public static bool CleanInvalidTags(CustomBlockData blockData)
        {
            if (!blockData || blockData.Tags == null) return false;

            _validTags ??= CollectValidTags();
            bool changed = false;

            foreach (var tag in blockData.Tags.ToList())
            {
                string name = GetTagName(tag);
                if (string.IsNullOrWhiteSpace(name) || !_validTags.Contains(name) || ForbiddenTags.Contains(name))
                {
                    blockData.Tags.Remove(tag);
                    changed = true;
                }
            }

            if (changed) EditorUtility.SetDirty(blockData);
            return changed;
        }

        private static HashSet<string> CollectValidTags()
        {
            var tags = new HashSet<string>();
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

            var behaviorType = FindType("Meta.XR.BuildingBlocks.Editor.CustomTagBehaviors");
            if (behaviorType != null)
            {
                foreach (var prop in behaviorType.GetProperties(flags))
                {
                    if (prop.PropertyType.Name.Contains("Tag"))
                    {
                        try { tags.Add(GetTagName(prop.GetValue(null))); } catch { }
                    }
                }
            }

            var utilsType = FindType("Meta.XR.BuildingBlocks.Editor.Utils");
            if (utilsType != null)
            {
                foreach (var name in new[] { "ExperimentalTag", "PrototypingTag", "DebugTag" })
                {
                    try
                    {
                        var field = utilsType.GetField(name, flags);
                        if (field != null) tags.Add(GetTagName(field.GetValue(null)));
                    }
                    catch { }
                }
            }

            tags.Remove(null);
            return tags;
        }

        private static string GetTagName(object tag)
        {
            return tag?.GetType().GetProperty("Name")?.GetValue(tag) as string;
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
