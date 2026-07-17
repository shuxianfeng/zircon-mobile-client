using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Zircon.Mobile.Core.Network;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>
    /// Captures the appearance fields that the first-generation mobile decoder
    /// intentionally skipped. Kept separate so the live protocol model remains
    /// compatible while the composed player renderer is introduced.
    /// </summary>
    public sealed class ZirconPlayerAppearanceCaptureBehaviour : MonoBehaviour
    {
        public readonly struct Appearance
        {
            public Appearance(byte characterClass, byte gender, byte direction, int hairType, int hairColour,
                int weapon, int armour, int shield, int armourColour, int armourImage, int helmet)
            {
                CharacterClass = characterClass;
                Gender = gender;
                Direction = direction;
                HairType = hairType;
                HairColour = hairColour;
                Weapon = weapon;
                Armour = armour;
                Shield = shield;
                ArmourColour = armourColour;
                ArmourImage = armourImage;
                Helmet = helmet;
            }

            public byte CharacterClass { get; }
            public byte Gender { get; }
            public byte Direction { get; }
            public int HairType { get; }
            public int HairColour { get; }
            public int Weapon { get; }
            public int Armour { get; }
            public int Shield { get; }
            public int ArmourColour { get; }
            public int ArmourImage { get; }
            public int Helmet { get; }
        }

        [SerializeField] private ZirconProtocolProbeBehaviour session;
        private ZirconNetworkClient subscribedClient;

        public static Appearance? Current { get; private set; }

        private void Update()
        {
            if (session == null)
                return;

            FieldInfo field = typeof(ZirconProtocolProbeBehaviour).GetField("client", BindingFlags.Instance | BindingFlags.NonPublic);
            ZirconNetworkClient client = field?.GetValue(session) as ZirconNetworkClient;
            if (client == null || ReferenceEquals(client, subscribedClient))
                return;

            if (subscribedClient != null)
                subscribedClient.PacketReceived -= OnPacketReceived;
            subscribedClient = client;
            subscribedClient.PacketReceived += OnPacketReceived;
        }

        private void OnDestroy()
        {
            if (subscribedClient != null)
                subscribedClient.PacketReceived -= OnPacketReceived;
        }

        private static void OnPacketReceived(object sender, ZirconPacketEventArgs e)
        {
            if (e.Frame.PacketId != ZirconPacketIds.Server.StartGame)
                return;

            try
            {
                using (var stream = new MemoryStream(e.Frame.Payload))
                using (var reader = new BinaryReader(stream))
                {
                    reader.ReadByte(); // result
                    reader.ReadString();
                    reader.ReadInt64();
                    if (!reader.ReadBoolean())
                        return;

                    reader.ReadInt32(); // character index
                    reader.ReadUInt32();
                    reader.ReadString();
                    reader.ReadInt32(); // name colour
                    reader.ReadString();
                    reader.ReadString();
                    byte characterClass = reader.ReadByte();
                    byte gender = reader.ReadByte();
                    reader.ReadInt32();
                    reader.ReadInt32();
                    byte direction = reader.ReadByte();
                    reader.ReadInt32(); // map
                    reader.ReadInt64();
                    reader.ReadInt32();
                    reader.ReadInt32(); // level
                    int hairType = reader.ReadInt32();
                    int hairColour = reader.ReadInt32();
                    int weapon = reader.ReadInt32();
                    int armour = reader.ReadInt32();
                    int shield = reader.ReadInt32();
                    int armourColour = reader.ReadInt32();
                    int armourImage = reader.ReadInt32();

                    Current = new Appearance(characterClass, gender, direction, hairType, hairColour,
                        weapon, armour, shield, armourColour, armourImage, 0);
                    Debug.Log($"P0 appearance class={characterClass} gender={gender} direction={direction} hair={hairType} hairColour={hairColour} weapon={weapon} armour={armour} shield={shield} armourColour={armourColour} armourImage={armourImage}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("P0 appearance capture failed: " + ex.Message);
            }
        }
    }
}
