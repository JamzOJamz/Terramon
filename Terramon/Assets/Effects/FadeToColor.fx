sampler uImage0 : register(s0);
sampler uImage1 : register(s1); 
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition; 
float2 uTargetPosition; 
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    // Fetch base texture color at the given UV coordinate
    float4 baseColor = tex2D(uImage0, uv); 

    // The final color we want to fade to, with uOpacity controlling the final alpha
    float4 finalColor = float4(uColor, uOpacity); 

    // Only apply color blending if the base color has some opacity (alpha > 0)
    if (baseColor.a > 0) 
    {
        // Blend the RGB channels between the base color and the final color based on uProgress
        baseColor.rgb = lerp(baseColor.rgb, finalColor.rgb, 1.0 - uProgress);
        
        // Directly set the alpha to the final color's alpha, which is controlled by uOpacity
        baseColor.a = finalColor.a;
    } 

    // Return the modified base color with the exact final alpha
    return baseColor;
}

technique Technique1
{
    pass FadePass
    {
        AlphaBlendEnable = TRUE;
        BlendOp = ADD;
        SrcBlend = SRCALPHA;
        DestBlend = INVSRCALPHA;
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}