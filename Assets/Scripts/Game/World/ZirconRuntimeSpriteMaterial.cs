using UnityEngine;

namespace Zircon.Mobile.Game.World
{
    public static class ZirconRuntimeSpriteMaterial
    {
        private static Material shared;

        public static Material Shared
        {
            get
            {
                if (shared != null)
                    return shared;

                shared = Resources.Load<Material>("ZirconRuntimeSprites");
                if (shared != null)
                {
                    Debug.Log("P2 runtime sprite material asset: shader=" + shared.shader.name + " supported=" + shared.shader.isSupported);
                    return shared;
                }

                Shader shader = Shader.Find("Zircon/RuntimeSprite");
                if (shader == null || !shader.isSupported)
                    shader = Shader.Find("Sprites/Default");
                if (shader == null || !shader.isSupported)
                    shader = Shader.Find("UI/Default");
                if (shader == null)
                {
                    Debug.LogError("P2 runtime sprite shader is unavailable.");
                    return null;
                }

                shared = new Material(shader)
                {
                    name = "P2_RuntimeSprites",
                    hideFlags = HideFlags.HideAndDontSave,
                };
                Debug.Log("P2 runtime sprite material: shader=" + shader.name + " supported=" + shader.isSupported);
                return shared;
            }
        }
    }
}
