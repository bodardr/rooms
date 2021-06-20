using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Bodardr.Rooms.Editor
{
    [EditorTool("Edit Rooms", typeof(Grid))]
    public class RoomEditTool : EditorTool
    {
        private const int MIN_ROOM_SIZE = 4;

        private static readonly Color creationInsideColor = new Color(0.09f, 1f, 0.79f, 0.4f);
        private static readonly Color creationOutlineColor = new Color(0f, 1f, 0.6f);

        private static readonly Color collisionInsideColor = new Color(1, 0, 0, 0.4f);
        private static readonly Color collisionOutlineColor = new Color(1, 0, 0, 1);

        private static readonly Color warningInsideColor = new Color(0.82f, 0.79f, 0f, 0.4f);
        private static readonly Color warningOutlineColor = new Color(1f, 0.94f, 0f, 0.6f);

        private Grid grid;
        private RoomManager roomManager;
        private List<Room> rooms;

        private Vector2Int firstPoint;
        private Vector2Int lastPoint;

        private List<BoxBoundsHandle> boxHandles;
        private List<Rect> intersectionRects;

        private Vector2Int previousMousePos;

        private bool isCreatingNewRoom;
        private bool isIntersecting;
        private bool isHotControl;

        public override GUIContent toolbarIcon { get; }

        private void OnEnable()
        {
            ToolManager.activeToolChanged += OnActiveToolChanged;
        }

        private void OnDisable()
        {
            ToolManager.activeToolChanged -= OnActiveToolChanged;
        }

        private void OnActiveToolChanged()
        {
            if (!ToolManager.IsActiveTool(this))
            {
                grid = null;
                rooms = null;
                roomManager = null;
                boxHandles = null;
                return;
            }

            grid = (Grid) target;
            bool hasRooms = grid.TryGetComponent(typeof(RoomManager), out var component);

            if (!hasRooms)
                roomManager = grid.gameObject.AddComponent<RoomManager>();
            else
                roomManager = grid.GetComponent<RoomManager>();

            rooms = roomManager.Rooms;

            CreateBoundHandles();
        }

        private void CreateBoundHandles()
        {
            boxHandles = new List<BoxBoundsHandle>();

            foreach (var room in rooms)
            {
                var handle = GetBoxBoundsHandle();
                boxHandles.Add(handle);
            }
        }

        public override void OnToolGUI(EditorWindow window)
        {
            isHotControl = false;
            var currentEvent = Event.current;

            UpdateRoomHandles();

            switch (currentEvent.type)
            {
                case EventType.Repaint:
                    Render();
                    break;
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                    break;
                default:
                    HandleInput(currentEvent);
                    break;
            }

            if (isHotControl)
                GUIUtility.hotControl = 0;
        }

        private void HandleInput(Event currentEvent)
        {
            var worldPos = EditorExtensions.MouseTo2DFlatPos(currentEvent.mousePosition);
            var cellPos = (Vector2Int) grid.WorldToCell(worldPos);

            if (currentEvent.button == (int) MouseButton.LeftMouse)
            {
                previousMousePos = cellPos;
                if (currentEvent.type == EventType.MouseDrag)
                {
                    if (isCreatingNewRoom)
                        isIntersecting = CheckRoomIntersections(
                            MathExtensions.PointsToBounds(firstPoint, previousMousePos),
                            out intersectionRects);

                    currentEvent.Use();
                }

                switch (currentEvent.type)
                {
                    case EventType.MouseDown:
                        BeginRoomCreation(cellPos);
                        break;
                    case EventType.MouseUp:
                        FinishRoomCreation(cellPos);
                        break;
                }
            }
        }

        private void Render()
        {
            if (isCreatingNewRoom)
            {
                var bounds = MathExtensions.PointsToBounds(firstPoint, previousMousePos);
                var rect = new Rect(grid.CellToWorld(bounds.min), Vector3.Scale(bounds.size, grid.cellSize));
                bool tooSmall = bounds.size.x < MIN_ROOM_SIZE || bounds.size.y < MIN_ROOM_SIZE;

                Handles.Label(rect.position, tooSmall ? "TOO SMALL" : $"({rect.width}, {rect.height})");

                Handles.DrawSolidRectangleWithOutline(rect,
                    isIntersecting || tooSmall ? warningInsideColor : creationInsideColor,
                    isIntersecting || tooSmall ? warningOutlineColor : creationOutlineColor);

                if (!isIntersecting)
                    return;

                foreach (var intersectionRect in intersectionRects)
                {
                    Handles.DrawSolidRectangleWithOutline(intersectionRect, collisionInsideColor,
                        collisionOutlineColor);
                }
            }
            else
            {
                bool isInsideARoom = rooms.Any(room => MathExtensions.Contains(room.CellBounds, previousMousePos));

                Handles.DrawSolidRectangleWithOutline(new Rect(previousMousePos, grid.cellSize), Color.clear,
                    isInsideARoom ? RoomManager.acccentColor : Color.white);
            }
        }

        private bool CheckRoomIntersections(BoundsInt bounds, out List<Rect> intersectionRectsWorldSpace)
        {
            var output = false;
            intersectionRectsWorldSpace = new List<Rect>();

            foreach (var room in rooms)
            {
                if (MathExtensions.Intersection(bounds, room.CellBounds, out var intersection))
                {
                    output = true;
                    intersectionRectsWorldSpace.Add(new Rect(grid.CellToWorld(intersection.min),
                        Vector3.Scale(intersection.size, grid.cellSize)));
                }
            }

            return output;
        }

        private void UpdateRoomHandles()
        {
            for (var i = 0; i < rooms.Count; i++)
            {
                if (boxHandles.Count < i + 1)
                {
                    boxHandles.Add(GetBoxBoundsHandle());
                }

                boxHandles[i].size = rooms[i].CellBounds.size;
                boxHandles[i].center = rooms[i].CellBounds.center;

                EditorGUI.BeginChangeCheck();

                boxHandles[i].DrawHandle();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(roomManager, "Change Room Bounds");

                    var center = boxHandles[i].center;
                    var halfsize = boxHandles[i].size / 2;

                    var newBounds = MathExtensions.PointsToBounds(Vector2Int.RoundToInt(center - halfsize),
                        Vector2Int.RoundToInt(center + halfsize));

                    rooms[i].CellBounds = newBounds;
                    roomManager.OnValidate();
                }
            }
        }

        private static BoxBoundsHandle GetBoxBoundsHandle()
        {
            return new BoxBoundsHandle
            {
                handleColor = RoomManager.acccentColor,
                wireframeColor = RoomManager.acccentColor,
                midpointHandleSizeFunction = (val) => .2f,
            };
        }

        private void BeginRoomCreation(Vector2Int cellPosition)
        {
            firstPoint = previousMousePos = cellPosition;
            isCreatingNewRoom = true;

            if (!roomManager)
            {
                roomManager = grid.gameObject.AddComponent<RoomManager>();
                rooms = roomManager.Rooms;
            }

            Event.current.Use();
            isHotControl = true;
        }

        private void FinishRoomCreation(Vector2Int cellPosition)
        {
            if (!isIntersecting)
            {
                lastPoint = cellPosition;
                var roomBounds = MathExtensions.PointsToBounds(firstPoint, lastPoint);

                if (roomBounds.size.x >= MIN_ROOM_SIZE && roomBounds.size.y >= MIN_ROOM_SIZE)
                {
                    Undo.RecordObject(roomManager, "Add Room");
                    rooms.Add(new Room {CellBounds = roomBounds});
                    roomManager.OnValidate();
                }
            }

            isCreatingNewRoom = false;
            Event.current.Use();
            isHotControl = false;
        }
    }
}