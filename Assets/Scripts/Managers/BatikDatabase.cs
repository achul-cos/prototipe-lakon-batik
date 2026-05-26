using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Database of BatikPattern ScriptableObjects. Add patterns in Inspector.
/// Also holds last drawn/colored texture references (persisted via TextureUtils).
/// </summary>
public class BatikDatabase : Singleton<BatikDatabase>
{
    [Tooltip("List of Batik Pattern ScriptableObjects")]
    public List<BatikPattern> allPatterns = new List<BatikPattern>();

    // Last textures used (not serialized). Use TextureUtils to persist between scenes.
    [HideInInspector] public Texture2D lastDrawnTexture;
    [HideInInspector] public Texture2D lastColoredTexture;

    public BatikPattern GetPatternById(string id)
    {
        return allPatterns.Find(p => p.patternId == id);
    }

    public BatikPattern GetPatternByIndex(int i)
    {
        if (i >= 0 && i < allPatterns.Count) return allPatterns[i];
        return null;
    }
}