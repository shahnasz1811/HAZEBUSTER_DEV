// Sprite shader that always stands upright and faces the camera ("billboard").
//
// Used with the slanted orthographic camera: the GROUND is seen at a slant, but characters,
// enemies, pickups and props must not be squashed with it. The sprite quad is rebuilt in camera
// space around the object's position (the sprite pivot), so the object's transform is never
// rotated. That keeps Rigidbody2D, colliders and flipX working exactly as before.
//
// Notes:
//  - Rotation around Z is ignored (an upright character never rolls). Do not use this material
//    on things that must turn to a direction on the ground, like projectiles or ground decals.
//  - Dynamic batching is disabled for this shader (it needs each object's own position).
//  - Based on Unity's built-in Sprites/Default shader (Built-in Render Pipeline).
Shader "HazeBuster/Sprite Billboard"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "DisableBatching" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex BillboardVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            v2f BillboardVert(appdata_t IN)
            {
                v2f OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_OUTPUT(v2f, OUT);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Sprite corner relative to the pivot, with flipX / flipY applied.
                float4 local = UnityFlipSprite(IN.vertex, _Flip);

                // Object position (the pivot) and object scale, taken from the object-to-world matrix.
                float3 pivot = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float scaleX = length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
                float scaleY = length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));

                // The camera's right and up directions in world space.
                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;

                float3 worldPos = pivot + (camRight * (local.x * scaleX)) + (camUp * (local.y * scaleY));

                OUT.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }
            ENDCG
        }
    }
}
