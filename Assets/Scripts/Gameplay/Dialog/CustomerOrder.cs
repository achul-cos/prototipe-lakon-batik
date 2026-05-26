using System;

/// <summary>
/// Lightweight container for customer order data.
/// </summary>
[Serializable]
public class CustomerOrder
{
    public string customerName;
    public string backStory;
    public BatikPattern desiredPattern;
    public BatikColor desiredColor;
    public string requestDialog;
}

public enum BatikColor { Merah, Hijau, Biru, Kuning, Ungu, Oranye }