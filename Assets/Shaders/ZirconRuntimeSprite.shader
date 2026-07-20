Shader "Zircon/RuntimeSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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

            fixed4 ZirconSpriteFrag(v2f IN) : SV_Target
            {
                fixed4 colour = SampleSpriteTexture(IN.texcoord) * IN.color;
                // Legacy ZL exports can expose their transparent key colour on
                // Android. Remove only the saturated magenta key range.
                if (colour.r > 0.75 && colour.b > 0.75 && colour.g < 0.50)
                    discard;
                clip(colour.a - 0.01);
                colour.rgb *= colour.a;
                return colour;
            }
            ENDCG
        }
    }
}
