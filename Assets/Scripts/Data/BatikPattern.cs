using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject data for a batik pattern. Create SO assets in project
/// (Assets -> Create -> LakonBatik/BatikPattern).
/// - maskTexture: black pixels = pattern (for accuracy testing).
/// - guideSprite: visual for guide overlay.
/// - keywords: list of keywords recognized in dialog.
/// - basePrice: price for the pattern.
/// </summary>
[CreateAssetMenu(fileName = "BatikPattern", menuName = "LakonBatik/BatikPattern", order = 0)]
public class BatikPattern : ScriptableObject
{
    public string patternId; // unique id (ex: "mega_mendung")
    public string humanName;
    public Sprite guideSprite;
    public Texture2D maskTexture; // black = pattern area
    public List<string> keywords = new List<string>();
    public int basePrice = 50000;
    [Range(0.5f, 2f)]
    public float difficultyMultiplier = 1f; // affects scoring threshold if needed
}