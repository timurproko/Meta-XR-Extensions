using System;
using System.Reflection;
using UnityEngine.UIElements;

static class UITKUtils
{
    private static int _hoverBit, _activeBit;
    private static bool _bound, _available;
    private static Type _pseudoEnumType;
    private static PropertyInfo _propPseudo;

    public static bool TrySetHover(VisualElement ve, bool on)
    {
        if (ve == null) return false;
        
        EnsureBound();
        
        if (!_available) return false;

        try
        {
            var curObj = _propPseudo.GetValue(ve);
            int cur = Convert.ToInt32(curObj);
            int next = on ? (cur | _hoverBit) : (cur & ~_hoverBit);
            var nextObj = Enum.ToObject(_pseudoEnumType, next);
            _propPseudo.SetValue(ve, nextObj);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public static bool TrySetActive(VisualElement ve, bool on)
    {
        if (ve == null) return false;
        EnsureBound();
        if (!_available) return false;

        try
        {
            var curObj = _propPseudo.GetValue(ve);
            int cur = Convert.ToInt32(curObj);
            int next = on ? (cur | _activeBit) : (cur & ~_activeBit);
            var nextObj = Enum.ToObject(_pseudoEnumType, next);
            _propPseudo.SetValue(ve, nextObj);
            return true;
        }
        catch { return false; }
    }

    public static bool TryGetPixelsPerUnit(UIDocument doc, out float ppu, float fallback = 1f)
    {
        FieldInfo PpuField =
            typeof(PanelSettings).GetField("m_PixelsPerUnit", BindingFlags.Instance | BindingFlags.NonPublic);

        ppu = fallback;
        try
        {
            var panelSettings = doc?.panelSettings;
            if (!panelSettings || PpuField == null) return false;

            var val = PpuField.GetValue(panelSettings);
            if (val is float f && !float.IsNaN(f) && float.IsFinite(f) && f > 0f)
            {
                ppu = f;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
    
    public static (float W, float H) GetPanelSize(UIDocument doc)
    {
        var root = doc?.rootVisualElement;
        return (root.worldBound.width, root.worldBound.height);
    }

    private static void EnsureBound()
    {
        if (_propPseudo != null) return;
        try
        {
            var veType = typeof(VisualElement);
            _propPseudo = veType.GetProperty("pseudoStates", BindingFlags.Instance | BindingFlags.NonPublic);
            _pseudoEnumType = veType.Assembly.GetType("UnityEngine.UIElements.PseudoStates");
            var hoverField  = _pseudoEnumType?.GetField("Hover",  BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var activeField = _pseudoEnumType?.GetField("Active", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            _hoverBit  = Convert.ToInt32(hoverField?.GetValue(null)  ?? 0);
            _activeBit = Convert.ToInt32(activeField?.GetValue(null) ?? 0);
            _available = _propPseudo != null && _pseudoEnumType != null && _activeBit != 0;
        }
        catch { _available = false; }
    }
}