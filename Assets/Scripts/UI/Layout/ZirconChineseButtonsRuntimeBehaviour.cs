using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zircon.Mobile.UI.Layout
{
    public sealed class ZirconChineseButtonsRuntimeBehaviour : MonoBehaviour
    {
        private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "LOGIN", "登录" }, { "PLAY", "进入游戏" }, { "Character", "角色" },
            { "Inventory", "背包" }, { "Skills", "技能" }, { "Quests", "任务" },
            { "Map", "地图" }, { "NPC", "NPC服务" }, { "Storage", "仓库" },
            { "Social", "社交" }, { "Mail", "邮件" }, { "Market", "市场" },
            { "CLOSE", "关闭" }, { "TARGET", "选怪" }, { "ATTACK", "攻击" },
            { "PICK", "拾取" }, { "S1", "技能1" }, { "S2", "技能2" },
            { "S3", "技能3" }, { "S4", "技能4" }, { "CHAT", "聊天" },
            { "BUFF", "增益" }, { "USE", "使用" }, { "LOCK", "锁定" },
            { "Unlock", "解锁" }, { "SET 1", "设为技能1" }, { "SET 2", "设为技能2" },
            { "SET 3", "设为技能3" }, { "SET 4", "设为技能4" }, { "SELL", "出售" },
            { "REPAIR", "修理" }, { "SORT STORAGE", "整理仓库" }, { "SEND", "发送" },
            { "Track", "追踪" }, { "Untrack", "取消追踪" }, { "BUY", "购买" },
            { "ACCEPT", "接受" }, { "DECLINE", "拒绝" }, { "CONFIRM", "确认" },
            { "CANCEL", "取消" }, { "BACK", "返回" }, { "NEXT", "下一步" },
            { "PREVIOUS", "上一步" }, { "REFRESH", "刷新" }, { "SEARCH", "搜索" },
            { "CLAIM", "领取" }, { "READY", "准备" }, { "Action", "操作" },
        };

        [SerializeField] private float refreshSeconds = .25f;
        private float nextRefreshTime;

        private void OnEnable()
        {
            nextRefreshTime = 0f;
            RefreshNow();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
                return;
            nextRefreshTime = Time.unscaledTime + Mathf.Max(.1f, refreshSeconds);
            RefreshNow();
        }

        public void RefreshNow()
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null)
                    continue;
                string translated = Translate(label.text);
                if (!string.Equals(translated, label.text, StringComparison.Ordinal))
                    label.text = translated;
            }
        }

        public static string Translate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;
            string trimmed = value.Trim();
            if (Labels.TryGetValue(trimmed, out string translated))
                return translated;
            if (trimmed.StartsWith("Buff ", StringComparison.OrdinalIgnoreCase))
                return "增益 " + trimmed.Substring(5);
            if (trimmed.StartsWith("Server ", StringComparison.OrdinalIgnoreCase))
                return "服务器 " + trimmed.Substring(7);
            if (trimmed.StartsWith("Hunt Gold", StringComparison.OrdinalIgnoreCase))
                return "狩猎金币" + trimmed.Substring(9);
            return value;
        }
    }
}
