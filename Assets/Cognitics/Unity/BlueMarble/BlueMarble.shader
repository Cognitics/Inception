
Shader "BlueMarble"
{
    Properties
    {
        _MainTex ("Imagery Texture", 2D) = "white" {}
        _SelectionTex ("Selection Texture", 2D) = "black" {}
        _DensityTex ("Density Texture", 2D) = "black" {}
        _DensityTexArray ("Density Texture Array", 2DArray) = "black" {}
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


            //sampler2D _MainTex;
            //float4 _MainTex_ST;
            UNITY_DECLARE_TEX2D(_MainTex);

            Texture2D _SelectionTex;
            SamplerState PointClampSampler;

            Texture2D _DensityTex;
            sampler2D _DensityTex_ST;

            UNITY_DECLARE_TEX2DARRAY(_DensityTexArray);
            //Texture2DArray _DensityTexArray;

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
                //fixed4 col = tex2D(_MainTex, input.uv);
                fixed4 col = UNITY_SAMPLE_TEX2D(_MainTex, input.uv);
                col = (col * 0.8) + 0.2;

                float density_index = _DensityTex.Sample(PointClampSampler, input.uv).x;
                float3 density_uv;


                // TODO: scale the UVs to the geocell
                float lon = input.uv.x * 360;
                float lat = input.uv.y * 180;
                uint ilon = floor(lon);
                uint ilat = floor(lat);
                density_uv.x = lon - ilon;
                density_uv.y = lat - ilat;

                density_uv.z = floor((density_index * 65536) + 0.5);
                if (density_uv.z > 0)
                {
                    // point clamp makes sharp edges
                    fixed4 density = UNITY_SAMPLE_TEX2DARRAY(_DensityTexArray, density_uv);
                    //fixed4 density = _DensityTexArray.Sample(PointClampSampler, density_uv);
                    col[0] = col[0] + (density[0] * density[3]);
                    col[1] = col[1] + (density[1] * density[3]);
                    col[2] = col[2] + (density[2] * density[3]);
                }

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
