using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

[ExecuteAlways]
[RequireComponent(typeof(Grid))]
[DisallowMultipleComponent]
public class RoomManager : MonoBehaviour
{
    private static GUIStyle guiStyle;

    public static Color primaryColor = new Color(0.28f, 0.75f, 1f, 0.1f);
    public static Color acccentColor = new Color(0f, 0.17f, 0.35f);

    private static int roomLayer;

    [HideInInspector]
    [SerializeField]
    private Transform roomParent;

    [SerializeField]
    private Transform mainTarget;

    [SerializeField]
    private List<Room> rooms;

    [SerializeField]
    private bool pixelPerfect = true;

    [SerializeField]
    private float maxOrthoSize = 12;

    private CinemachineVirtualCamera activeCamera;

    private List<CinemachineVirtualCamera> cameras = new List<CinemachineVirtualCamera>();

    private Grid grid;

    public List<Room> Rooms => rooms;

    private void Awake()
    {
        roomLayer = LayerMask.NameToLayer("Room");
        roomParent = GameObject.Find($"{gameObject.name} Rooms")?.transform;


#if UNITY_EDITOR
        if (!roomParent)
        {
            InstantiateRoomParent();
            return;
        }
#else
        if(!roomParent)
            Debug.LogError("Room parent not assigned!")
#endif

        roomParent.GetComponentsInChildren(true, cameras);
        grid = GetComponent<Grid>();

        foreach (var cam in cameras)
            cam.enabled = false;
    }

    public void TriggerTransition(int nextRoom)
    {
        if (cameras.Count < 1)
            roomParent.GetComponentsInChildren(true, cameras);


        if (activeCamera)
            activeCamera.enabled = false;

        activeCamera = cameras[nextRoom];
        activeCamera.enabled = true;
    }

    #region EDITOR_PART

#if UNITY_EDITOR
    public void OnValidate()
    {
        if (!roomParent)
            Awake();

        EditorApplication.delayCall += OnValidateDelayed;
    }

    private void OnValidateDelayed()
    {
        if (roomParent == null)
            InstantiateRoomParent();

        roomParent.name = $"{gameObject.name} Rooms";

        UpdateRoomChildren();

        EditorApplication.delayCall -= OnValidateDelayed;
    }

    private void InstantiateRoomParent()
    {
        cameras.Clear();

        roomParent = new GameObject {name = $"{name} Rooms", layer = roomLayer}.transform;
        roomParent.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    private void UpdateRoomChildren()
    {
        var roomCount = Rooms.Count;
        var childCount = roomParent.childCount - 1;

        var exceedingRooms = childCount - roomCount;
        if (exceedingRooms > 0)
        {
            for (int i = exceedingRooms; i >= 0; i--)
            {
                DestroyImmediate(roomParent.GetChild(roomCount + i).gameObject);
            }
        }

        for (int i = 0; i < roomCount; i++)
        {
            Transform child;
            if (i > childCount)
            {
                child = CreateChildRoom(i);
            }
            else
            {
                child = roomParent.GetChild(i);
                child.name = $"Room {i + 1}";
            }

            var roomBounds = Rooms[i].CellBounds;
            child.position = roomBounds.center;

            UpdatePolygonCollider(roomBounds, child.GetComponent<PolygonCollider2D>(),
                grid.cellSize);
        }

        while (cameras.Count > rooms.Count)
            cameras.RemoveAt(cameras.Count - 1);

        for (var i = 0; i < rooms.Count; i++)
        {
            UpdateCamera(cameras[i], rooms[i]);
        }
    }

    private Transform CreateChildRoom(int i)
    {
        var newChild = new GameObject($"Room {i + 1}", typeof(PolygonCollider2D)) {layer = roomLayer};
        var polygonCollider = newChild.GetComponent<PolygonCollider2D>();
        polygonCollider.isTrigger = true;
        var child = newChild.transform;
        child.SetParent(roomParent);

        var cameraGameObject = new GameObject(newChild.name + " Camera", typeof(CinemachineVirtualCamera))
            {layer = roomLayer};
        cameraGameObject.transform.SetParent(child);
        cameraGameObject.transform.rotation = Quaternion.identity;

        var cam = cameraGameObject.GetComponent<CinemachineVirtualCamera>();
        cam.LookAtTargetAttachment = 0;
        cam.AddCinemachineComponent<CinemachineFramingTransposer>();

        var confiner = cameraGameObject.AddComponent<CinemachineConfiner>();
        confiner.m_BoundingShape2D = polygonCollider;
        cam.AddExtension(confiner);

        cameras.Add(cam);

        return child;
    }

    private void UpdatePolygonCollider(BoundsInt bounds, PolygonCollider2D polygonCollider, Vector2 cellSize)
    {
        var halfSize = (Vector3) bounds.size * 0.5f;

        var bottomLeft = halfSize;
        var bottomRight = new Vector2(halfSize.x, -halfSize.y);

        var topRight = -halfSize;
        var topLeft = new Vector2(-halfSize.x, halfSize.y);


        polygonCollider.SetPath(0, new[]
        {
            Vector2.Scale(bottomLeft, cellSize),
            Vector2.Scale(bottomRight, cellSize),
            Vector2.Scale(topRight, cellSize),
            Vector2.Scale(topLeft, cellSize),
        });
    }

    private void UpdateCamera(CinemachineVirtualCamera camera, Room room)
    {
        camera.Follow = mainTarget;

        var hasPixelPerfectComponent = camera.TryGetComponent(typeof(CinemachinePixelPerfect), out _);

        switch (pixelPerfect)
        {
            case true when !hasPixelPerfectComponent:
                camera.AddExtension(camera.gameObject.AddComponent<CinemachinePixelPerfect>());
                break;
            case false when hasPixelPerfectComponent:
                camera.RemoveExtension(camera.GetComponent<CinemachinePixelPerfect>());
                break;
        }

        if (room.CustomCameraSize)
            return;

        float ratio = camera.m_Lens.Aspect;
        var size = Vector3.Scale(room.CellBounds.size, grid.cellSize);
        var fittingOrthoSize = Mathf.Min(size.x / ratio, size.y) / 2f;


        camera.m_Lens.OrthographicSize = Mathf.Min(fittingOrthoSize, maxOrthoSize);
    }

    private void OnDrawGizmosSelected()
    {
        guiStyle ??= new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter, stretchWidth = true, stretchHeight = true,
            normal = new GUIStyleState
            {
                textColor = acccentColor
            }
        };

        if (!grid)
            grid = gameObject.GetComponent<Grid>();

        guiStyle.fontSize = (int) (1500 / SceneView.currentDrawingSceneView.size);

        //Draw currentRooms.
        for (var i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            var rect = new Rect(grid.CellToWorld(room.CellBounds.min),
                Vector3.Scale(room.CellBounds.size, grid.cellSize));

            Handles.DrawSolidRectangleWithOutline(rect, primaryColor, acccentColor);
            Handles.Label(rect.center + new Vector2(-grid.cellSize.x, grid.cellSize.y) / 2, (i + 1).ToString(),
                guiStyle);
        }
    }

    private void Reset()
    {
        OnValidate();
    }
#endif

    #endregion
}