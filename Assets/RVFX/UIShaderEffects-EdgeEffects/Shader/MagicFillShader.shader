Shader "Custom/MagicFillShader"
{
    Properties
    {
        _MainTex ("Pattern Texture", 2D) = "white" {}
        _Color ("Fill Color", Color) = (1,1,1,1)
        _ScrollSpeed ("Scroll Speed", Vector) = (0.5, 0, 0, 0)
        _FillAmount ("Fill Amount", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ScrollSpeed;
            float _FillAmount;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Clipping based on progress
                if (i.uv.x > _FillAmount) discard;

                // Scrolling logic
                float2 scrollingUV = i.uv + _ScrollSpeed.xy * _Time.y;
                fixed4 tex = tex2D(_MainTex, scrollingUV);
                
                return tex * _Color;
            }
            ENDCG
        }
    }
}