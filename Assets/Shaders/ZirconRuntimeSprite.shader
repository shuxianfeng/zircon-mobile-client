Shader "Zircon/RuntimeSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _LegacyDitherShadow ("Legacy dither shadow", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment ZirconSpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnitySprites.cginc"

            fixed _LegacyDitherShadow;

            fixed4 ZirconSpriteFrag(v2f IN) : SV_Target
            {
                fixed4 colour = SampleSpriteTexture(IN.texcoord) * IN.color;
                // Legacy ZL exports can expose their transparent key colour on
                // Android. Remove only the saturated magenta key range.
                if (colour.r > 0.75 && colour.b > 0.75 && colour.g < 0.50)
                    discard;
                clip(colour.a - 0.01);
                // Legacy map objects encoded translucent shadows as isolated
                // opaque black pixels. Preserve real dark artwork while
                // restoring only the near-exact black dither to soft shadow.
                if (_LegacyDitherShadow > 0.5 &&
                    colour.a > 0.99 &&
                    max(colour.r, max(colour.g, colour.b)) < 0.02)
                    colour.a = 0.36;
                colour.rgb *= colour.a;
                return colour;
            }
            ENDCG
        }
    }
}
