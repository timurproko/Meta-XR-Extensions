using System.Collections.Generic;

public static class UITKInteractionBlocker
{
    public static bool IsBlocked => _activeBlockers.Count > 0;
    
    private static readonly HashSet<object> _activeBlockers = new();
    
    public static void AddBlock(object blocker)
    {
        if (blocker != null)
        {
            _activeBlockers.Add(blocker);
        }
    }
    
    public static void RemoveBlock(object blocker)
    {
        if (blocker != null)
        {
            _activeBlockers.Remove(blocker);
        }
    }
    
    public static void ClearAll()
    {
        _activeBlockers.Clear();
    }
}