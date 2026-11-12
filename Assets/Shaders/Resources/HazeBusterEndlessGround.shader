// Endless ground for the slanted camera.
//
// The texture coordinates come from the WORLD position of every pixel (not from the mesh), so the
// ground picture is glued to the world. The quad that carries it can move with the camera every
// frame without the picture ever sliding, popping or jumping, and the texture repeats forever.
//
// A slow, soft brightness variation (also based on world position) breaks up the repetition so a
// single painted tile does not look like wallpaper.
//
// Built-in render pipeline. Draws first (Background queue) and writes no depth, so sprites that
// lie on the ground at z = 0 never fight with it.
Shader "HazeBuster/Endless Ground"
{
    Properties
    {
        _MainTex ("Ground Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _TileSize ("World Units Per Repeat", Float) = 4
        _Variation ("Variation Strength", Range(0,1)) = 0.3
        _VariationScale ("Variation Scale (world units)", Float) = 16
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        ZWrite Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Tint;
            float _TileSize;
            float _Variation;
            float _VariationScale;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 world : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.world = worldPos.xy;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.world / max(_TileSize, 0.0001);
                fixed4 col = tex2D(_MainTex, uv) * _Tint;

                float scale = max(_VariationScale, 0.0001);
                float n = valueNoise(i.world / scale) * 0.65 + valueNoise(i.world / (scale * 0.37) + 17.3) * 0.35;
                col.rgb *= 1.0 + (n - 0.5) * _Variation * 1.4;

                col.a = 1.0;
                return col;
            }
            ENDCG
        }
    }
}
