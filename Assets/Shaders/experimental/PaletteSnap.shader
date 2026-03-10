Shader "Custom/PaletteSnap"
{
    Properties
    {
        _Palette ("Palette Texture", 2D) = "white" {}
        _PaletteSize ("Palette Size", Range(1, 64)) = 16
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZTest Always Cull Off ZWrite Off

        Pass
        {
            Name "PaletteSnap"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_Palette);
            SAMPLER(sampler_Palette);
            float _PaletteSize;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv  = input.texcoord;
                half4  col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 bestUV   = float2(0.5 / _PaletteSize, 0.5);
                float  bestDist = 1e9;

                for (int j = 0; j < (int)_PaletteSize; j++)
                {
                    float u = (j + 0.5) / _PaletteSize;
                    half4 pcol = SAMPLE_TEXTURE2D(_Palette, sampler_Palette, float2(u, 0.5));
                    float d = distance(col.rgb, pcol.rgb);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestUV   = float2(u, 0.5);
                    }
                }

                return SAMPLE_TEXTURE2D(_Palette, sampler_Palette, bestUV);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
