using System;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class DefaultExecutionOrder : Attribute
    {
        public DefaultExecutionOrder(int order) { }
    }

    public class Object
    {
        protected static void Destroy(object target) { }
    }

    public class MonoBehaviour : Object
    {
        private readonly GameObject owner = new GameObject();
        public Transform transform => owner.transform;
    }

    public sealed class GameObject
    {
        public GameObject(string name = "")
        {
            this.name = name;
            transform = new Transform();
        }

        public string name { get; }
        public Transform transform { get; }

        public T AddComponent<T>() where T : new()
        {
            T component = new T();
            if (component is SpriteRenderer renderer)
                renderer.Attach(this);
            return component;
        }
    }

    public sealed class Transform
    {
        public Vector3 localPosition { get; set; }
        public Vector3 position => localPosition;
        public Vector3 localScale { get; set; } = Vector3.one;
        public void SetParent(Transform parent, bool worldPositionStays) { }
    }

    public sealed class SpriteRenderer : Object
    {
        private GameObject owner = new GameObject();
        internal void Attach(GameObject gameObject) => owner = gameObject;
        public GameObject gameObject => owner;
        public Transform transform => owner.transform;
        public bool enabled { get; set; }
        public Sprite sprite { get; set; }
        public Color color { get; set; }
        public int sortingOrder { get; set; }
    }

    public struct Vector2
    {
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float x;
        public float y;
    }

    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int x;
        public int y;
        public static Vector2Int zero => new Vector2Int(0, 0);
        public static Vector2Int operator +(Vector2Int left, Vector2Int right) =>
            new Vector2Int(left.x + right.x, left.y + right.y);
        public static Vector2Int operator *(Vector2Int value, int scale) =>
            new Vector2Int(value.x * scale, value.y * scale);
        public static bool operator ==(Vector2Int left, Vector2Int right) => left.Equals(right);
        public static bool operator !=(Vector2Int left, Vector2Int right) => !left.Equals(right);
        public bool Equals(Vector2Int other) => x == other.x && y == other.y;
        public override bool Equals(object value) => value is Vector2Int other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y);
        public override string ToString() => $"({x}, {y})";
    }

    public struct Vector3
    {
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float x;
        public float y;
        public float z;
        public float sqrMagnitude => x * x + y * y + z * z;
        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 one => new Vector3(1f, 1f, 1f);
        public static Vector3 operator -(Vector3 left, Vector3 right) =>
            new Vector3(left.x - right.x, left.y - right.y, left.z - right.z);
        public static Vector3 operator *(Vector3 value, float scale) =>
            new Vector3(value.x * scale, value.y * scale, value.z * scale);
        public static Vector3 LerpUnclamped(Vector3 from, Vector3 to, float t) =>
            new Vector3(
                from.x + (to.x - from.x) * t,
                from.y + (to.y - from.y) * t,
                from.z + (to.z - from.z) * t);
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public float x;
        public float y;
        public float width;
        public float height;
    }

    public struct Color
    {
        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public float r;
        public float g;
        public float b;
        public float a;
        public static Color white => new Color(1f, 1f, 1f, 1f);
    }

    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point }
    public enum TextureWrapMode { Clamp }

    public sealed class Texture2D : Object
    {
        public Texture2D(int width, int height, TextureFormat format, bool mipChain)
        {
            this.width = width;
            this.height = height;
        }

        public int width { get; private set; }
        public int height { get; private set; }
        public string name { get; set; }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public void SetPixel(int x, int y, Color color) { }
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable) { }
        public bool LoadImage(byte[] bytes) => bytes != null && bytes.Length > 0;
    }

    public sealed class Sprite : Object
    {
        private Sprite(Texture2D texture, Rect rect)
        {
            this.texture = texture;
            this.rect = rect;
        }

        public Texture2D texture { get; }
        public Rect rect { get; }
        public string name { get; set; }
        public static Sprite Create(
            Texture2D texture,
            Rect rect,
            Vector2 pivot,
            float pixelsPerUnit) => new Sprite(texture, rect);
    }

    public static class Mathf
    {
        public const float Rad2Deg = 57.29578f;
        public static int FloorToInt(float value) => (int)MathF.Floor(value);
        public static int Max(int left, int right) => Math.Max(left, right);
        public static float Max(float left, float right) => Math.Max(left, right);
        public static int Clamp(int value, int minimum, int maximum) =>
            Math.Clamp(value, minimum, maximum);
        public static int Abs(int value) => Math.Abs(value);
        public static float Abs(float value) => Math.Abs(value);
        public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
    }

    public static class Time
    {
        public static float time { get; set; }
        public static float unscaledTime { get; set; }
    }

    public static class Application
    {
        public static string dataPath { get; set; } =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "zircon-missing-assets");
    }
}
