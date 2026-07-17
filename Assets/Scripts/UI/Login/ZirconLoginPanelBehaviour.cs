using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Core.Network;

namespace Zircon.Mobile.UI.Login
{
    public sealed class ZirconLoginPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private TMP_Text statusText;

        private bool submitting;

        private void OnEnable()
        {
            loginButton?.onClick.AddListener(OnLoginPressed);
            if (session != null)
                session.ConnectionStateChanged += OnConnectionStateChanged;
            Refresh();
        }

        private void OnDisable()
        {
            loginButton?.onClick.RemoveListener(OnLoginPressed);
            if (session != null)
                session.ConnectionStateChanged -= OnConnectionStateChanged;
        }

        private void OnLoginPressed()
        {
            _ = SubmitAsync();
        }

        public async Task SubmitAsync()
        {
            if (submitting || session == null)
                return;

            string email = emailInput?.text?.Trim() ?? string.Empty;
            string password = passwordInput?.text ?? string.Empty;
            if (email.Length == 0 || password.Length == 0)
            {
                SetStatus("请输入账号和密码");
                return;
            }

            submitting = true;
            Refresh();
            try
            {
                await session.ConnectAndLoginAsync(email, password);
            }
            finally
            {
                submitting = false;
                Refresh();
            }
        }

        private void OnConnectionStateChanged(ZirconConnectionState state)
        {
            SetStatus(StateText(state));
            Refresh();
        }

        private void Refresh()
        {
            if (loginButton != null)
                loginButton.interactable = !submitting;

            if (submitting)
                SetStatus("正在登录...");
            else if (session != null)
                SetStatus(StateText(session.ConnectionState));
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = value;
        }

        private static string StateText(ZirconConnectionState state)
        {
            switch (state)
            {
                case ZirconConnectionState.Connecting: return "正在连接服务器...";
                case ZirconConnectionState.VersionChecking: return "正在校验版本...";
                case ZirconConnectionState.ReadyForLogin: return "服务器已连接";
                case ZirconConnectionState.LoggingIn: return "正在验证账号...";
                case ZirconConnectionState.SelectingCharacter: return "请选择角色";
                case ZirconConnectionState.LoadingMap: return "正在进入游戏...";
                case ZirconConnectionState.InGame: return "已进入游戏";
                default: return "未连接";
            }
        }
    }
}