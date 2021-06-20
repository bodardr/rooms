using System;
using UnityEngine;

[Serializable]
public class Room
{
    [SerializeField]
    private bool customCameraSize = false;

    [SerializeField]
    private BoundsInt cellBounds;

    /// <summary>
    /// Bounds in Cell Space
    /// </summary>
    public BoundsInt CellBounds
    {
        get => cellBounds;
        set => cellBounds = value;
    }

    public bool CustomCameraSize
    {
        get => customCameraSize;
        set => customCameraSize = value;
    }
}