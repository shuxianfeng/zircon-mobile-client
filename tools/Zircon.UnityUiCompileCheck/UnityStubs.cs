using System;
using System.Threading.Tasks;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.World;

namespace UnityEngine
{
    public sealed class SerializeField : Attribute { }
    public class Object
    {
        protected static T Instantiate<T>(T original, object parent) where T : class, new() => new T();
        protected static void Destroy(object target) { }
    }
    public class MonoBehaviour : Object { }
    public class RectTransform { public GameObject gameObject { get; } = new GameObject(); public Vector2 anchoredPosition { get; set; } }
    public readonly struct Vector2 { public Vector2(float x, float y) { } public static Vector2 zero => new Vector2(0, 0); }
    public class GameObject
    {
        public bool activeSelf { get; set; }
        public void SetActive(bool active) => activeSelf = active;
    }
    public class TextAsset { public string text { get; set; } = string.Empty; }
    public static class JsonUtility { public static T FromJson<T>(string json) where T : new() => new T(); }
    public readonly struct Color
    {
        public Color(float r, float g, float b, float a) { }
    }
    public static class Time { public static float unscaledTime { get; set; } }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
}

namespace UnityEngine.UI
{
    using UnityEngine;
    using UnityEngine.Events;

    public struct ColorBlock { public Color normalColor { get; set; } }
    public class Selectable { public bool interactable { get; set; } }
    public class Toggle : Selectable
    {
        public bool isOn { get; set; }
        public ToggleEvent onValueChanged { get; } = new ToggleEvent();
        public void SetIsOnWithoutNotify(bool value) => isOn = value;
    }
    public sealed class ToggleEvent { public void AddListener(Action<bool> action) { } }
    public class Button : Selectable
    {
        public GameObject gameObject { get; } = new GameObject();
        public ColorBlock colors { get; set; }
        public ButtonClickedEvent onClick { get; } = new ButtonClickedEvent();
        public T GetComponentInChildren<T>() where T : new() => new T();
    }
    public sealed class ButtonClickedEvent
    {
        public void AddListener(UnityAction action) { }
        public void RemoveListener(UnityAction action) { }
    }
}

namespace TMPro
{
    public class TMP_Text { public string text { get; set; } = string.Empty; }
    public class TMP_InputField { public string text { get; set; } = string.Empty; }
    public class TMP_Dropdown { public int value { get; set; } }
}

namespace Zircon.Mobile.UI.Login
{
    public sealed class ZirconProtocolProbeBehaviour
    {
        public ZirconWorldSnapshot GetWorldSnapshot() => null;
        public Task SendGamePacketCommandAsync(byte[] packet, string label) => Task.CompletedTask;
        public Task SendQuestAcceptCommandAsync(int index) => Task.CompletedTask;
        public Task SendQuestCompleteCommandAsync(int index, int choiceIndex) => Task.CompletedTask;
        public Task SendQuestTrackCommandAsync(int index, bool track) => Task.CompletedTask;
        public Task SendNpcSellCommandAsync(System.Collections.Generic.IReadOnlyList<ZirconCellLinkInfo> links) => Task.CompletedTask;
        public Task SendNpcRepairCommandAsync(System.Collections.Generic.IReadOnlyList<ZirconCellLinkInfo> links, bool special, bool guildFunds) => Task.CompletedTask;        public Task SendChatCommandAsync(string text) => Task.CompletedTask;
        public Task SendMagicKeyCommandAsync(int infoIndex, int magicType, byte set1Key, byte set2Key, byte set3Key, byte set4Key) => Task.CompletedTask;
        public Task SendSortStorageCommandAsync() => Task.CompletedTask;
        public Task SendItemMoveCommandAsync(ZirconGridType fromGrid, ZirconGridType toGrid, int fromSlot, int toSlot, bool mergeItem) => Task.CompletedTask;
        public Task SendItemUseCommandAsync(ZirconGridType grid, int slot, long count = 1) => Task.CompletedTask;
        public Task SendItemLockCommandAsync(ZirconGridType grid, int slot, bool locked) => Task.CompletedTask;
        public Task SendNpcButtonCommandAsync(int buttonId) => Task.CompletedTask;
        public Task SendNpcBuyCommandAsync(int itemInfoIndex, long amount) => Task.CompletedTask;
        public Task SendNpcCloseCommandAsync() => Task.CompletedTask;
    }
}
namespace Zircon.Mobile.Game.Skills
{
    public sealed class ZirconMobileSkillButtonBehaviour
    {
        public void Configure(int type, int mode, int delayMilliseconds) { }
    }
}