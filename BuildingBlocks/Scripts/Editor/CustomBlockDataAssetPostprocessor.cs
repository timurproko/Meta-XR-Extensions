using UnityEditor;

namespace BuildingBlocks.Editor
{
    public class CustomBlockDataAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var changed = false;
            foreach (var path in imported)
            {
                if (!path.EndsWith(".asset")) continue;
                var block = AssetDatabase.LoadAssetAtPath<CustomBlockData>(path);
                if (block && TagValidationService.CleanInvalidTags(block)) changed = true;
            }
            if (changed) AssetDatabase.SaveAssets();
        }
    }
}
