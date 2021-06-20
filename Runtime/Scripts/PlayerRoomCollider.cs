using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PlayerRoomCollider : MonoBehaviour
{
    private RoomManager roomManager;
    private BoxCollider2D boxCollider2D;
    private readonly HashSet<int> rooms = new HashSet<int>();

    private int layerMask;
    private Collider2D[] boxes = new Collider2D[10];
    private ContactFilter2D contactFilter2D;

    private void Awake()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        roomManager = FindObjectOfType<RoomManager>();
        layerMask = LayerMask.GetMask("Room");
        contactFilter2D = new ContactFilter2D
            {layerMask = layerMask, useTriggers = true, useLayerMask = true, useDepth = false, useOutsideDepth = false};
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
            return;

        if (!boxCollider2D)
            Awake();

        var count = boxCollider2D.OverlapCollider(contactFilter2D, boxes);
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                if (!rooms.Contains(boxes[i].transform.GetSiblingIndex()))
                {
                    rooms.Clear();
                    OnTriggerEnter2D(boxes[i]);
                    break;
                }
            }
        }
#endif
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var siblingIndex = other.transform.GetSiblingIndex();

        if (rooms.Add(siblingIndex))
            roomManager.TriggerTransition(siblingIndex);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        rooms.Remove(other.transform.GetSiblingIndex());
    }
}