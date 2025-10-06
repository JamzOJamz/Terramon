sampler uImage0 : register(s0);
float2 uImageSize0;

static const float4 outlineColorMain = float4(1.0f, 0.9f, 0.27f, 1.0f);
static const float4 outlineColorSecond = float4(1.0f, 0.5f, 0.0f, 1.0f);

float4 Outline(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 center = tex2D(uImage0, coords) * sampleColor;
    if (center.a > 0)
        return center;

    float2 uvPx = float2(2 / uImageSize0.x, 2 / uImageSize0.y);
	
    float neigh = 0;
	
    // everything here is essentially the two loops below but manually unrolled.
    // orth checks are functionally inlined.
    float ortho = 0;
    
    // tl
    neigh += tex2D(uImage0, coords - uvPx).a;
    // tm
    ortho += tex2D(uImage0, float2(coords.x, coords.y - uvPx.y)).a;
        // tr
    neigh += tex2D(uImage0, float2(coords.x + uvPx.x, coords.y - uvPx.y)).a;
        // ml
    ortho += tex2D(uImage0, float2(coords.x - uvPx.x, coords.y)).a;
        // mr
    ortho += tex2D(uImage0, float2(coords.x + uvPx.x, coords.y)).a;
        // bl
    neigh += tex2D(uImage0, float2(coords.x - uvPx.x, coords.y + uvPx.y)).a;
        // bm
    ortho += tex2D(uImage0, float2(coords.x, coords.y + uvPx.y)).a;
        // br
    neigh += tex2D(uImage0, coords + uvPx).a;
    
    if (ortho > 1)
        return outlineColorSecond;
    return neigh == 0 ? center : outlineColorMain;
    
    /*
    // sample everything
    float alpha[3][3];
    
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 offset = float2(x, y) * uvPx;
            neigh += alpha[y + 1][x + 1] = tex.Sample(smp, coords + offset).a;
        }
    }

	if (neigh == 0)
        return center;

    // orthogonal check for corners
    float aUp    = alpha[0][1];
    float aDown  = alpha[2][1];
    float aLeft  = alpha[1][0];
    float aRight = alpha[1][2];

	// ints are ints are ints
    int orthCount = (aUp > 0) + (aDown > 0) + (aLeft > 0) + (aRight > 0);

    if (orthCount >= 2)
        return outlineColorSecond;
    return outlineColorMain;
    */
}

technique Technique1
{
    pass ShaderPass
    {
        PixelShader = compile ps_2_0 Outline();
    }
}