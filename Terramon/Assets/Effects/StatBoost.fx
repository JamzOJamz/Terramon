sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float2 uImageSize0;
float2 uImageSize1;
float uTime;
float uIntensity;
float uOpacity;
float3 uColor;

float4 Overlay(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 fragCoord = coords * uImageSize0;
    float2 tiled = frac(float2(fragCoord.x, fragCoord.y + uTime * uIntensity) / uImageSize1);
    float4 base = tex2D(uImage0, coords) * sampleColor;
    float4 targetBase = tex2D(uImage1, tiled) * float4(uColor, 1.0) * base.a;
    float4 final = lerp(base, targetBase, uOpacity);
    // to get cyan to turn blue you remove some green
    // to get yellow to turn orange you remove some green
    // texture is b&w so green is its own sort of lerping factor towards less green
    final.g = lerp(final.g, final.g * final.g, uOpacity);
    return final;
}

technique Technique1
{
    pass ShaderPass
    {
        PixelShader = compile ps_2_0 Overlay();
    }
}
