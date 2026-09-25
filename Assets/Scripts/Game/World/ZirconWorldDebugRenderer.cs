using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Entities;

namespace Zircon.Mobile.Game.World
{
    [DefaultExecutionOrder(-100)]
    public sealed class ZirconWorldDebugRenderer : MonoBehaviour
    {
        [SerializeField] private float tileScale = 0.32f;
        [SerializeField] private float tileHeightRatio = 2f / 3f;
        [SerializeField] private float markerSize = 0.12f;
        // PC walking and running both occupy one six-frame (about 600 ms)
        // action. Running covers two cells during that single action.
        [SerializeField] private float movementSecondsPerCell = 0.60f;
        [SerializeField] private float predictionTimeoutSeconds = 1.25f;
        [SerializeField] private int maxPredictedSteps = 2;
        [SerializeField] private float movementGraceSeconds = 0.06f;
        [SerializeField] private int teleportCellThreshold = 3;
        [SerializeField] private bool useGeneratedSprites = true;
        [SerializeField] private string generatedTextureRoot = "Generated/Textures";
        [SerializeField] private float spritePixelsPerUnit = 100f;
        [SerializeField] private float spriteAnimationFps = 4f;
        [SerializeField] private bool showFallbackMarkers;

        private readonly Dictionary<uint, SpriteRenderer> markers = new Dictionary<uint, SpriteRenderer>();
        private readonly Dictionary<uint, MovementState> movementByObjectId =
            new Dictionary<uint, MovementState>();
        private readonly Dictionary<ZirconEntityKind, List<Sprite>> spritesByKind = new Dictionary<ZirconEntityKind, List<Sprite>>();
        private readonly HashSet<uint> seenObjectIds = new HashSet<uint>();
        private readonly List<uint> removedObjectIds = new List<uint>();
        private Sprite markerSprite;
        private bool generatedSpritesLoaded;
        private uint localPlayerObjectId;
        private bool hasLocalPlayerObjectId;
        private uint selectedObjectId;
        private bool hasSelectedObjectId;
        private ZirconWorldSnapshot renderedSnapshot;

        private sealed class MovementState
        {
            public Vector3 From;
            public Vector3 Target;
            public float StartedAt;
            public float Duration;
            public bool HasAuthoritativeCell;
            public Vector2Int AuthoritativeCell;
            public Vector2Int PlannedCell;
            public byte AuthoritativeDirection;
            public byte VisualDirection;
            public int VisualMoveDistance;
            public long LastPositionSequence;
            public bool IsCorrection;
            public float LastMovementEndedAt = float.NegativeInfinity;
            public readonly Queue<MovementSegment> QueuedTargets =
                new Queue<MovementSegment>();
            public readonly List<Prediction> Unconfirmed = new List<Prediction>();
        }

        private readonly struct MovementSegment
        {
            public MovementSegment(Vector3 target, float duration, byte direction, int distance)
            {
                Target = target;
                Duration = duration;
                Direction = direction;
                Distance = distance;
            }

            public Vector3 Target { get; }
            public float Duration { get; }
            public byte Direction { get; }
            public int Distance { get; }
        }

        private readonly struct Prediction
        {
            public Prediction(Vector2Int cell, float sentAt)
            {
                Cell = cell;
                SentAt = sentAt;
            }

            public Vector2Int Cell { get; }
            public float SentAt { get; }
        }

        public float TileScale => tileScale;

        public void Render(ZirconWorldSnapshot snapshot)
        {
            if (snapshot == null)
                return;
            if (ReferenceEquals(snapshot, renderedSnapshot))
                return;
            renderedSnapshot = snapshot;

            EnsureSprite();
            hasLocalPlayerObjectId = snapshot.LocalPlayer != null;
            localPlayerObjectId = hasLocalPlayerObjectId ? snapshot.LocalPlayer.ObjectId : 0u;

            seenObjectIds.Clear();
            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                seenObjectIds.Add(entity.ObjectId);
                SpriteRenderer renderer = GetOrCreate(entity.ObjectId);
                Sprite sprite = GetSprite(entity, snapshot);
                bool isLocalPlayer = snapshot.LocalPlayer != null &&
                                     entity.ObjectId == snapshot.LocalPlayer.ObjectId;
                bool showMonsterCollisionFallback = entity.Kind == ZirconEntityKind.Monster && !entity.Dead;
                renderer.enabled = !isLocalPlayer &&
                                   (sprite != markerSprite || showFallbackMarkers || showMonsterCollisionFallback);
                SetMovementTarget(entity.ObjectId, renderer,
                    entity.Location.X, entity.Location.Y, entity.Direction,
                    entity.PositionSequence, entity.MoveDistance);
                renderer.transform.localScale = sprite == markerSprite ? Vector3.one * markerSize : Vector3.one;
                renderer.sprite = sprite;
                renderer.color = sprite == markerSprite ? GetColour(entity, snapshot) : Color.white;
                if (hasSelectedObjectId && entity.ObjectId == selectedObjectId)
                {
                    renderer.color = new Color(1f, 0.82f, 0.2f, 1f);
                    renderer.transform.localScale *= 1.12f;
                }
                renderer.sortingOrder = entity.Location.Y * 4 + 2;
            }

            removedObjectIds.Clear();
            foreach (uint objectId in markers.Keys)
            {
                if (!seenObjectIds.Contains(objectId))
                    removedObjectIds.Add(objectId);
            }

            foreach (uint objectId in removedObjectIds)
            {
                if (markers.TryGetValue(objectId, out SpriteRenderer renderer) && renderer != null)
                    Destroy(renderer.gameObject);

                markers.Remove(objectId);
                movementByObjectId.Remove(objectId);
            }
        }

        public void Clear()
        {
            foreach (SpriteRenderer renderer in markers.Values)
            {
                if (renderer != null)
                    Destroy(renderer.gameObject);
            }

            markers.Clear();
            movementByObjectId.Clear();
            renderedSnapshot = null;
            hasLocalPlayerObjectId = false;
            hasSelectedObjectId = false;
        }

        public void SetSelectedObject(uint objectId)
        {
            selectedObjectId = objectId;
            hasSelectedObjectId = true;
            renderedSnapshot = null;
        }

        public bool IsSelectedObject(uint objectId)
        {
            return hasSelectedObjectId && selectedObjectId == objectId;
        }

        public void ClearSelectedObject()
        {
            hasSelectedObjectId = false;
            renderedSnapshot = null;
        }

        public bool TryGetEntityWorldPosition(uint objectId, out Vector3 position)
        {
            if (markers.TryGetValue(objectId, out SpriteRenderer renderer) && renderer != null)
            {
                position = renderer.transform.position;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        public bool TryGetEntityRenderer(uint objectId, out SpriteRenderer renderer)
        {
            return markers.TryGetValue(objectId, out renderer) && renderer != null;
        }

        public bool IsUsingFallbackMarker(uint objectId)
        {
            return markerSprite != null &&
                   markers.TryGetValue(objectId, out SpriteRenderer renderer) &&
                   renderer != null && renderer.sprite == markerSprite;
        }

        public bool TryGetLocalPlayerWorldPosition(out Vector3 position)
        {
            if (hasLocalPlayerObjectId)
                return TryGetEntityWorldPosition(localPlayerObjectId, out position);

            position = Vector3.zero;
            return false;
        }

        public bool TryGetLocalPlayerPlannedCell(out Vector2Int cell)
        {
            if (hasLocalPlayerObjectId &&
                movementByObjectId.TryGetValue(localPlayerObjectId, out MovementState movement) &&
                movement.HasAuthoritativeCell)
            {
                cell = movement.PlannedCell;
                return true;
            }

            cell = Vector2Int.zero;
            return false;
        }

        public bool IsObjectMoving(uint objectId)
        {
            if (!movementByObjectId.TryGetValue(objectId, out MovementState movement))
                return false;
            if (movement.IsCorrection)
                return false;
            if (movement.QueuedTargets.Count > 0)
                return true;
            if (movement.Duration > 0f &&
                Time.unscaledTime <= movement.StartedAt + movement.Duration +
                Mathf.Max(0f, movementGraceSeconds))
                return true;
            return Time.unscaledTime <= movement.LastMovementEndedAt +
                   Mathf.Max(0f, movementGraceSeconds);
        }

        public bool TryGetVisualDirection(uint objectId, out byte direction)
        {
            if (movementByObjectId.TryGetValue(objectId, out MovementState movement))
            {
                direction = movement.VisualDirection;
                return true;
            }
            direction = 0;
            return false;
        }

        public bool TryGetVisualMoveDistance(uint objectId, out int distance)
        {
            if (movementByObjectId.TryGetValue(objectId, out MovementState movement))
            {
                distance = movement.VisualMoveDistance;
                return true;
            }
            distance = 0;
            return false;
        }

        public bool TryPredictLocalMove(byte direction)
        {
            return TryPredictLocalMove(direction, 1);
        }

        public bool TryPredictLocalMove(byte direction, int distance)
        {
            if (!hasLocalPlayerObjectId ||
                !markers.TryGetValue(localPlayerObjectId, out SpriteRenderer renderer) ||
                renderer == null ||
                !movementByObjectId.TryGetValue(localPlayerObjectId, out MovementState movement) ||
                !movement.HasAuthoritativeCell ||
                (movement.IsCorrection && movement.Duration > 0f) ||
                movement.Unconfirmed.Count >= Mathf.Max(1, maxPredictedSteps) ||
                movement.QueuedTargets.Count >= Mathf.Max(1, maxPredictedSteps))
                return false;

            Vector2Int delta = DirectionToDelta(direction);
            if (delta == Vector2Int.zero || distance <= 0)
                return false;

            int moveDistance = Mathf.Clamp(distance, 1, 2);
            Vector2Int next = movement.PlannedCell + delta * moveDistance;
            movement.PlannedCell = next;
            movement.Unconfirmed.Add(new Prediction(next, Time.unscaledTime));
            if (movement.Unconfirmed.Count > 32)
                movement.Unconfirmed.RemoveAt(0);
            EnqueueMovement(renderer, movement, ToWorldPosition(next.x, next.y),
                MovementActionDuration(), direction, moveDistance);
            return true;
        }

        private SpriteRenderer GetOrCreate(uint objectId)
        {
            if (markers.TryGetValue(objectId, out SpriteRenderer renderer) && renderer != null)
                return renderer;

            var marker = new GameObject($"ZirconEntity_{objectId}");
            marker.transform.SetParent(transform, false);
            renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = markerSprite;
            markers[objectId] = renderer;
            return renderer;
        }

        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            foreach (KeyValuePair<uint, MovementState> pair in movementByObjectId)
            {
                if (!markers.TryGetValue(pair.Key, out SpriteRenderer renderer) || renderer == null)
                    continue;
                MovementState movement = pair.Value;
                if (pair.Key == localPlayerObjectId)
                    RetireStalePredictions(renderer, movement, now);
                if (movement.Duration <= 0f && movement.QueuedTargets.Count == 0)
                    continue;
                AdvanceMovement(renderer, movement, now);
            }
        }

        private void SetMovementTarget(
            uint objectId,
            SpriteRenderer renderer,
            int x,
            int y,
            byte direction,
            long positionSequence,
            int moveDistance)
        {
            var cell = new Vector2Int(x, y);
            Vector3 target = ToWorldPosition(x, y);
            int normalizedMoveDistance = Mathf.Clamp(moveDistance, 0, 2);
            if (!movementByObjectId.TryGetValue(objectId, out MovementState movement))
            {
                renderer.transform.localPosition = target;
                movementByObjectId[objectId] = new MovementState
                {
                    From = target,
                    Target = target,
                    StartedAt = Time.unscaledTime,
                    Duration = 0f,
                    HasAuthoritativeCell = true,
                    AuthoritativeCell = cell,
                    PlannedCell = cell,
                    AuthoritativeDirection = direction,
                    VisualDirection = direction,
                    VisualMoveDistance = normalizedMoveDistance,
                    LastPositionSequence = positionSequence,
                };
                return;
            }

            bool sameCell = movement.HasAuthoritativeCell && movement.AuthoritativeCell == cell;
            movement.LastPositionSequence = positionSequence;
            movement.AuthoritativeDirection = direction;
            // Repeated packets for the current authoritative cell must never restart
            // or reverse a local predicted segment. A genuine rejection is handled by
            // the bounded prediction timeout below.
            if (sameCell)
            {
                if (movement.Duration <= 0f && movement.QueuedTargets.Count == 0 &&
                    (objectId != localPlayerObjectId || movement.Unconfirmed.Count == 0))
                {
                    movement.VisualDirection = direction;
                    movement.VisualMoveDistance = normalizedMoveDistance;
                }
                return;
            }

            Vector2Int previousCell = movement.HasAuthoritativeCell
                ? movement.AuthoritativeCell
                : cell;
            movement.HasAuthoritativeCell = true;
            movement.AuthoritativeCell = cell;

            if (objectId == localPlayerObjectId && ConfirmPrediction(movement, cell))
                return;

            bool correctingPrediction = objectId == localPlayerObjectId &&
                                        movement.Unconfirmed.Count > 0;
            movement.Unconfirmed.Clear();
            movement.QueuedTargets.Clear();
            movement.PlannedCell = cell;
            movement.VisualDirection = direction;
            movement.VisualMoveDistance = normalizedMoveDistance;
            if (correctingPrediction)
            {
                // A server correction must not be rendered as a standing character
                // slowly sliding across the ground. Align immediately to the
                // authoritative cell and let the next real move start normally.
                SnapToAuthoritativeCell(renderer, movement, target);
                return;
            }

            float cells = CellDistance(previousCell, cell);
            int teleportThreshold = Mathf.Max(
                Mathf.Max(2, teleportCellThreshold), normalizedMoveDistance + 1);
            bool teleport = cells >= teleportThreshold;
            if (teleport)
            {
                movement.From = target;
                movement.Target = target;
                movement.StartedAt = Time.unscaledTime;
                movement.Duration = 0f;
                movement.IsCorrection = true;
                renderer.transform.localPosition = target;
            }
            else
            {
                BeginMovement(renderer, movement, target,
                    normalizedMoveDistance > 0 &&
                    cells <= Mathf.Max(1f, normalizedMoveDistance)
                        ? MovementActionDuration()
                        : Mathf.Max(0.05f,
                            movementSecondsPerCell * Mathf.Max(1f, cells)),
                    Time.unscaledTime,
                    false, direction, normalizedMoveDistance);
            }
        }

        private bool ConfirmPrediction(MovementState movement, Vector2Int authoritativeCell)
        {
            for (int i = 0; i < movement.Unconfirmed.Count; i++)
            {
                if (movement.Unconfirmed[i].Cell != authoritativeCell)
                    continue;
                movement.Unconfirmed.RemoveRange(0, i + 1);
                return true;
            }
            return false;
        }

        private void RetireStalePredictions(
            SpriteRenderer renderer,
            MovementState movement,
            float now)
        {
            float timeout = Mathf.Max(0.25f, predictionTimeoutSeconds);
            if (movement.Unconfirmed.Count == 0 ||
                now - movement.Unconfirmed[0].SentAt <= timeout)
                return;

            movement.Unconfirmed.Clear();
            movement.QueuedTargets.Clear();
            movement.PlannedCell = movement.AuthoritativeCell;
            movement.VisualDirection = movement.AuthoritativeDirection;
            movement.VisualMoveDistance = 0;
            Vector3 target = ToWorldPosition(
                movement.AuthoritativeCell.x, movement.AuthoritativeCell.y);
            // Timed-out prediction rollback is a network correction, not a new
            // movement action. Snapping avoids the disturbing idle glide that
            // otherwise happens after the joystick has already been released.
            SnapToAuthoritativeCell(renderer, movement, target);
        }

        private static void SnapToAuthoritativeCell(
            SpriteRenderer renderer,
            MovementState movement,
            Vector3 target)
        {
            movement.From = target;
            movement.Target = target;
            movement.StartedAt = Time.unscaledTime;
            movement.Duration = 0f;
            movement.IsCorrection = false;
            movement.LastMovementEndedAt = float.NegativeInfinity;
            renderer.transform.localPosition = target;
        }

        private void EnqueueMovement(
            SpriteRenderer renderer,
            MovementState movement,
            Vector3 target,
            float duration,
            byte direction,
            int distance)
        {
            float now = Time.unscaledTime;
            AdvanceMovement(renderer, movement, now);
            if (movement.Duration <= 0f && movement.QueuedTargets.Count == 0)
            {
                float startedAt = now;
                float endedAgo = now - movement.LastMovementEndedAt;
                if (movement.LastMovementEndedAt > 0f && endedAgo >= 0f && endedAgo <= 0.10f)
                    startedAt = movement.LastMovementEndedAt;
                BeginMovement(renderer, movement, target,
                    duration, startedAt, false, direction, distance);
                // If this frame arrived just after the previous segment ended,
                // catch up by that fraction instead of visibly pausing on the cell.
                AdvanceMovement(renderer, movement, now);
                return;
            }
            movement.QueuedTargets.Enqueue(new MovementSegment(
                target, duration, direction, distance));
        }

        private void BeginMovement(
            SpriteRenderer renderer,
            MovementState movement,
            Vector3 target,
            float duration,
            float startedAt,
            bool isCorrection,
            byte direction,
            int distance)
        {
            Vector3 from = renderer.transform.localPosition;
            movement.IsCorrection = isCorrection;
            movement.VisualDirection = direction;
            movement.VisualMoveDistance = distance;
            if ((from - target).sqrMagnitude < 0.000001f)
            {
                movement.From = target;
                movement.Target = target;
                movement.StartedAt = startedAt;
                movement.Duration = 0f;
                renderer.transform.localPosition = target;
                return;
            }

            movement.From = from;
            movement.Target = target;
            movement.StartedAt = startedAt;
            movement.Duration = duration;
        }

        private void AdvanceMovement(
            SpriteRenderer renderer,
            MovementState movement,
            float now)
        {
            while (movement.Duration > 0f)
            {
                float finishedAt = movement.StartedAt + movement.Duration;
                if (now < finishedAt)
                {
                    float t = Mathf.Clamp01((now - movement.StartedAt) / movement.Duration);
                    renderer.transform.localPosition = Vector3.LerpUnclamped(
                        movement.From, movement.Target, t);
                    return;
                }

                renderer.transform.localPosition = movement.Target;
                if (movement.QueuedTargets.Count == 0)
                {
                    movement.From = movement.Target;
                    movement.Duration = 0f;
                    if (!movement.IsCorrection)
                        movement.LastMovementEndedAt = finishedAt;
                    return;
                }

                MovementSegment next = movement.QueuedTargets.Dequeue();
                movement.From = movement.Target;
                movement.Target = next.Target;
                movement.StartedAt = finishedAt;
                movement.IsCorrection = false;
                movement.Duration = Mathf.Max(0.05f, next.Duration);
                movement.VisualDirection = next.Direction;
                movement.VisualMoveDistance = next.Distance;
            }

            renderer.transform.localPosition = movement.Target;
        }

        private float MovementActionDuration()
        {
            return Mathf.Max(0.05f, movementSecondsPerCell);
        }

        private float WorldCellDistance(Vector3 from, Vector3 to)
        {
            float cellsX = Mathf.Abs(to.x - from.x) / Mathf.Max(0.0001f, tileScale);
            float cellsY = Mathf.Abs(to.y - from.y) /
                           Mathf.Max(0.0001f, tileScale * tileHeightRatio);
            return Mathf.Max(cellsX, cellsY);
        }

        private static float CellDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y));
        }

        private void EnsureSprite()
        {
            if (markerSprite != null)
                return;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            markerSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private Sprite GetSprite(ZirconEntityState entity, ZirconWorldSnapshot snapshot)
        {
            LoadGeneratedSprites();

            ZirconEntityKind kind = entity.Kind;
            if (snapshot.LocalPlayer != null && entity.ObjectId == snapshot.LocalPlayer.ObjectId)
                kind = ZirconEntityKind.Player;

            if (!spritesByKind.TryGetValue(kind, out List<Sprite> sprites) || sprites.Count == 0)
                return markerSprite;

            // The tracked production sample currently contains only monster
            // models 0 and 1. Keep a diagnostic marker for any other model so
            // an unrelated creature is never presented as the final artwork.
            if (kind == ZirconEntityKind.Monster && (entity.ModelIndex < 0 || entity.ModelIndex >= 2))
                return markerSprite;

            if (kind == ZirconEntityKind.Npc)
                return GetStableGroupedSprite(sprites, entity.ModelIndex, entity.Direction);

            if (kind == ZirconEntityKind.Player)
            {
                int directionGroup = entity.Direction >= 4 ? 1 : 0;
                int frame = entity.Action == ZirconMirAction.Moving
                    ? Mathf.FloorToInt(Time.time * spriteAnimationFps)
                    : entity.Direction;
                return GetStableGroupedSprite(sprites, directionGroup, frame);
            }

            if (kind == ZirconEntityKind.Monster)
            {
                int frame = Mathf.FloorToInt(Time.time * spriteAnimationFps);
                return GetStableGroupedSprite(sprites, entity.ModelIndex, frame);
            }

            if (kind == ZirconEntityKind.Item)
                return sprites[PositiveModulo(entity.ModelIndex, sprites.Count)];

            int animatedFrame = Mathf.FloorToInt(Time.time * spriteAnimationFps);
            int objectOffset = (int)(entity.ObjectId % 2147483647u);
            return sprites[PositiveModulo(animatedFrame + objectOffset, sprites.Count)];
        }

        private static Sprite GetStableGroupedSprite(List<Sprite> sprites, int groupValue, int frameValue)
        {
            const int FramesPerGroup = 4;
            if (sprites.Count < FramesPerGroup)
                return sprites[PositiveModulo(frameValue, sprites.Count)];

            int groupCount = Mathf.Max(1, sprites.Count / FramesPerGroup);
            int group = PositiveModulo(groupValue, groupCount);
            int frame = PositiveModulo(frameValue, FramesPerGroup);
            int index = group * FramesPerGroup + frame;
            return sprites[index < sprites.Count ? index : sprites.Count - 1];
        }

        private static int PositiveModulo(int value, int divisor)
        {
            if (divisor <= 0)
                return 0;
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private void LoadGeneratedSprites()
        {
            EnsureSprite();

            if (generatedSpritesLoaded)
                return;

            generatedSpritesLoaded = true;
            if (!useGeneratedSprites)
                return;

            string normalizedRoot = generatedTextureRoot.Replace('/', Path.DirectorySeparatorChar);
            string root = Path.Combine(Application.dataPath, normalizedRoot);
            LoadSpriteSequence(root, "M-Hum", ZirconEntityKind.Player);
            LoadSpriteSequence(root, "Mon-1", ZirconEntityKind.Monster);
            LoadSpriteSequence(root, "NPC", ZirconEntityKind.Npc);
            LoadSpriteSequence(root, "MIcon", ZirconEntityKind.Spell);
            LoadSpriteSequence(root, "Items", ZirconEntityKind.Item);
        }

        private void LoadSpriteSequence(string root, string folder, ZirconEntityKind kind)
        {
            string directory = Path.Combine(root, folder);
            if (!Directory.Exists(directory))
                return;

            string[] files = Directory.GetFiles(directory, "*_image.png");
            System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);

            var sprites = new List<Sprite>();
            foreach (string file in files)
            {
                byte[] bytes = File.ReadAllBytes(file);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    Destroy(texture);
                    continue;
                }

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                string fileName = Path.GetFileNameWithoutExtension(file);
                texture.name = fileName;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0f), spritePixelsPerUnit);
                sprite.name = fileName;
                sprites.Add(sprite);
            }

            if (sprites.Count > 0)
                spritesByKind[kind] = sprites;
        }

        private Vector3 ToWorldPosition(int x, int y)
        {
            return new Vector3(x * tileScale, -y * tileScale * tileHeightRatio, 0f);
        }

        private static Vector2Int DirectionToDelta(byte direction)
        {
            switch (direction)
            {
                case 0: return new Vector2Int(0, -1);
                case 1: return new Vector2Int(1, -1);
                case 2: return new Vector2Int(1, 0);
                case 3: return new Vector2Int(1, 1);
                case 4: return new Vector2Int(0, 1);
                case 5: return new Vector2Int(-1, 1);
                case 6: return new Vector2Int(-1, 0);
                case 7: return new Vector2Int(-1, -1);
                default: return Vector2Int.zero;
            }
        }

        private static Color GetColour(ZirconEntityState entity, ZirconWorldSnapshot snapshot)
        {
            if (snapshot.LocalPlayer != null && entity.ObjectId == snapshot.LocalPlayer.ObjectId)
                return new Color(0.1f, 0.9f, 0.35f, 1f);

            switch (entity.Kind)
            {
                case ZirconEntityKind.Player:
                    return new Color(0.2f, 0.75f, 1f, 1f);
                case ZirconEntityKind.Monster:
            return entity.Dead ? new Color(0.45f, 0.15f, 0.15f, 0.35f) : new Color(1f, 0.25f, 0.2f, 0.55f);
                case ZirconEntityKind.Npc:
                    return new Color(1f, 0.85f, 0.15f, 1f);
                case ZirconEntityKind.Spell:
                    return new Color(0.9f, 0.35f, 1f, 1f);
                case ZirconEntityKind.Item:
                    return new Color(0.25f, 0.95f, 0.8f, 1f);
                default:
                    return new Color(0.75f, 0.75f, 0.75f, 1f);
            }
        }
    }
}
