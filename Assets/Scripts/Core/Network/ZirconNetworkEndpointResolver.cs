using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zircon.Mobile.Core.Network
{
    public enum ZirconNetworkTransport
    {
        Unknown,
        Wifi,
        Cellular,
    }

    public sealed class ZirconNetworkEndpoint
    {
        public string Profile;
        public string Host;
        public int Port;
        public bool PreferIpv6;
        public ZirconNetworkTransport Transport;

        public override string ToString() => Profile + " " + Host + ":" + Port;
    }

    /// <summary>Selects a server endpoint from the phone's active network transport at runtime.</summary>
    public static class ZirconNetworkEndpointResolver
    {
        public static IReadOnlyList<ZirconNetworkEndpoint> Resolve(
            string lanHost, string publicHost, int port, bool allowWifiPublicFallback = true)
        {
            ZirconNetworkTransport transport = DetectTransport();
            var endpoints = new List<ZirconNetworkEndpoint>();
            if (transport == ZirconNetworkTransport.Wifi)
            {
                endpoints.Add(Endpoint("Wi-Fi内网", lanHost, port, false, transport));
                if (allowWifiPublicFallback)
                    endpoints.Add(Endpoint("Wi-Fi外网回退", publicHost, port, true, transport));
            }
            else if (transport == ZirconNetworkTransport.Cellular)
            {
                endpoints.Add(Endpoint("移动网络外网", publicHost, port, true, transport));
            }
            else
            {
                // Unknown is possible briefly while Android changes transports. Public DNS is
                // the safest first attempt, with LAN retained as a compatibility fallback.
                endpoints.Add(Endpoint("自动外网", publicHost, port, true, transport));
                endpoints.Add(Endpoint("自动内网回退", lanHost, port, false, transport));
            }
            return endpoints;
        }

        public static ZirconNetworkTransport DetectTransport()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getSystemService", "connectivity"))
                using (AndroidJavaObject network = manager.Call<AndroidJavaObject>("getActiveNetwork"))
                {
                    if (network == null) return ZirconNetworkTransport.Unknown;
                    using (AndroidJavaObject capabilities = manager.Call<AndroidJavaObject>("getNetworkCapabilities", network))
                    {
                        if (capabilities == null) return ZirconNetworkTransport.Unknown;
                        // android.net.NetworkCapabilities: TRANSPORT_CELLULAR=0, TRANSPORT_WIFI=1.
                        if (capabilities.Call<bool>("hasTransport", 1)) return ZirconNetworkTransport.Wifi;
                        if (capabilities.Call<bool>("hasTransport", 0)) return ZirconNetworkTransport.Cellular;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Android network transport detection failed: " + ex.Message);
            }

            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
                return ZirconNetworkTransport.Cellular;
            if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
                return ZirconNetworkTransport.Wifi;
            return ZirconNetworkTransport.Unknown;
#else
            return ZirconNetworkTransport.Wifi;
#endif
        }

        private static ZirconNetworkEndpoint Endpoint(string profile, string host, int port, bool preferIpv6,
            ZirconNetworkTransport transport)
        {
            return new ZirconNetworkEndpoint
            {
                Profile = profile,
                Host = host,
                Port = port,
                PreferIpv6 = preferIpv6,
                Transport = transport,
            };
        }
    }
}
