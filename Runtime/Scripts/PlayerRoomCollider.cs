using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR

#endif

[AddComponentMenu("Room System/Player Room Collider")]
public class PlayerRoomCollider : MonoBehaviour
{
    private readonly HashSet<int> rooms = new HashSet<int>();
    private BoxCollider2D boxCollider2D;
    private Collider2D[] boxes = new Collider2D[10];
    private ContactFilter2D contactFilter2D;

    private int layerMask;
    private RoomManager roomManager;

    private void Awake()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        roomManager = FindObjectOfType<RoomManager>();
        layerMask = LayerMask.GetMask("Room");
        contactFilter2D = new ContactFilter2D
            {layerMask = layerMask, useTriggers = true, useLayerMask = true, useDepth = false, useOutsideDepth = false};
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Room"))
            return;

        var siblingIndex = other.transform.GetSiblingIndex();

        if (rooms.Add(siblingIndex))
            roomManager.TriggerTransition(siblingIndex);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Room"))
            return;

        rooms.Remove(other.transform.GetSiblingIndex());
    }
}