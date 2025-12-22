sampler uImage0 : register(s0);
float3 uColors[8];

float4 Palette(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 px = tex2D(uImage0, coords);
    return float4(uColors[px.r * 255], 1.0) * px.a;
}

technique Technique1
{
    pass ShaderPass
    {
        PixelShader = compile ps_2_0 Palette();
    }
}
