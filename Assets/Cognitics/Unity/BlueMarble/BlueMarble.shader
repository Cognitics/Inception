
Shader "BlueMarble"
{
    Properties
    {
        _MainTex ("Imagery Texture", 2D) = "white" {}
        _SelectionTex ("Selection Texture", 2D) = "black" {}
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vertex_shader
            #pragma fragment fragment_shader

            #include "UnityCG.cginc"

            struct VertexShaderInput
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexShaderOutput
            {
                float4 sv_position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 position : COLOR0;   // using COLOR0 to store vertex position
            };


            sampler2D _MainTex;
            float4 _MainTex_ST;

            Texture2D _SelectionTex;
            SamplerState PointClampSampler;

            VertexShaderOutput vertex_shader(VertexShaderInput input)
            {
                VertexShaderOutput result;
                result.position = input.position;
                result.sv_position = UnityObjectToClipPos(input.position);
                result.uv = input.uv;
                return result;
            }

            fixed4 fragment_shader (VertexShaderOutput input) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, input.uv);
                col = (col * 0.8) + 0.2;

                fixed4 hicol = _SelectionTex.Sample(PointClampSampler, input.uv);
                col[0] = col[0] + (hicol[0] * hicol[3]);
                col[1] = col[1] + (hicol[1] * hicol[3]);
                col[2] = col[2] + (hicol[2] * hicol[3]);
                return col;
            }

            ENDCG
        }
    }
}
