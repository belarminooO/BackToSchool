Shader "Custom/Posterize"
{
    Properties
    {
        _Levels ("Posterize Levels", Range(2, 32)) = 8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZTest Always Cull Off ZWrite Off

        Pass
        {
            Name "Posterize"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Levels;

            half4 Frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

                // Quantize each channel to _Levels steps
                col.rgb = floor(col.rgb * _Levels) / (_Levels - 1.0);

                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
