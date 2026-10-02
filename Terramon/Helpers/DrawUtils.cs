namespace Terramon.Helpers;

public struct SpriteBatchData
{
    public SpriteSortMode SortMode;
    public BlendState BlendState;
    public SamplerState SamplerState;
    public DepthStencilState DepthStencilState;
    public RasterizerState RasterizerState;
    public Effect Effect;
    public Matrix Matrix;

    public SpriteBatchData(SpriteBatch sb)
    {
        if (sb is null)
            return;

        SortMode = sb.sortMode;
        BlendState = sb.blendState;
        SamplerState = sb.samplerState;
        DepthStencilState = sb.depthStencilState;
        RasterizerState = sb.rasterizerState;
        Effect = sb.customEffect;
        Matrix = sb.transformMatrix;
    }
}

public readonly record struct TrailStyle(Texture2D Texture, Color Color, float Scale = 1f, float Scroll = 0f);

public static class DrawUtils
{
    private static VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[32];
    private static short[] _indices = new short[32];
    public static void DrawDirectTrail(TrailStyle style, Vector2 start, Vector2 end, float thickness, int sections = 1, int growIn = 1, int shrinkOut = 0)
    {
        if (sections <= 0 || start == end)
            return;

        var triCount = sections * 2;
        var vertCount = (sections + 1) * 2; // vertices are shared so not 4 per section but 2 per section and one extra to close the mesh
        if (_vertices.Length < vertCount)
            Array.Resize(ref _vertices, vertCount);
        if (_indices.Length < triCount * 3)
            Array.Resize(ref _indices, triCount * 3);

        var vert = new VertexPositionColorTexture(default, style.Color, default);

        var to = end - start;
        var perp = Vector2.Normalize(new Vector2(-to.Y, to.X)) * thickness;

        // iterates thru each row of 2 vertices to set proper position
        for (int k = 0; k <= sections; k++)
        {
            var prog = k / (float)sections;
            var pos = Vector2.Lerp(start, end, prog);

            // these scale things currently just allow you to define how much of the trail each section occupies (taper in section, normal size section, taper out section)
            // but they're actually meant to let you do cool things with the trail shape, like have it be more rounded, but i haven't added that yet
            // spacing also needs to be variable to avoid using too many triangles for any given shape, and so the tapers are always the same size

            var scaleIn = 1f;
            if (growIn != 0)
            {
                // 0 when starts growing, 1 when at full size
                float t = k / (float)growIn;
                scaleIn = Math.Min(t, 1f);
            }

            var scaleOut = 1f;
            if (shrinkOut != 0)
            {
                // 1 when starts sgrinking, 0 when fully shrunk
                float t = (sections - k) / (float)shrinkOut;
                scaleOut = Math.Min(t, 1f);
            }

            var scale = scaleIn * scaleOut;

            vert.Position = new Vector3(pos - perp * scale, 0f);
            vert.TextureCoordinate = new Vector2(prog, 1f);
            _vertices[k * 2] = vert;

            vert.Position = new Vector3(pos + perp * scale, 0f);
            vert.TextureCoordinate = new Vector2(prog, 1f);
            _vertices[k * 2 + 1] = vert;
        }

        // iterate thru each section to set indices for both triangles in the section
        for (int i = 0; i < sections; i++)
        {
            // two triangles use 6 indices
            var m = i * 6;
            // going row by row so two vertices
            var n = (short)(i * 2);

            _indices[m+0] = _indices[m + 3] = n++;
            _indices[m+5] = n++;
            _indices[m+1] = n++;
            _indices[m+2] = _indices[m + 4] = n;
        }

        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        Main.graphics.graphicsDevice.Textures[0] = style.Texture;
        Main.graphics.graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, vertCount, _indices, 0, triCount);
    }

    public static float TexelWidth(this Texture2D tex) => 1f / tex.Width;
    public static float TexelHeight(this Texture2D tex) => 1f / tex.Height;
    public static void Begin(this SpriteBatch spriteBatch, in SpriteBatchData spriteBatchData)
    {
        spriteBatch.Begin
        (
            spriteBatchData.SortMode, spriteBatchData.BlendState, spriteBatchData.SamplerState,
            spriteBatchData.DepthStencilState,
            spriteBatchData.RasterizerState, spriteBatchData.Effect, spriteBatchData.Matrix
        );
    }

    public static void End(this SpriteBatch spriteBatch, out SpriteBatchData spriteBatchData)
    {
        spriteBatchData = new SpriteBatchData(spriteBatch);
        spriteBatch.End();
    }

    public static SpriteBatchData Restart(this SpriteBatch sb)
    {
        sb.End(out var data);
        sb.Begin(in data);
        return data;
    }

    public static SpriteBatchData Restart(this SpriteBatch sb, SpriteSortMode newSortMode)
    {
        sb.End(out var data);
        data.SortMode = newSortMode;
        sb.Begin(in data);
        return data;
    }

    public static SpriteBatchData Restart(this SpriteBatch sb, SamplerState newSamplerState)
    {
        sb.End(out var data);
        data.SamplerState = newSamplerState;
        sb.Begin(in data);
        return data;
    }

    public static SpriteBatchData Restart(this SpriteBatch sb, Effect newEffect)
    {
        sb.End(out var data);
        data.Effect = newEffect;
        sb.Begin(in data);
        return data;
    }

    public static SpriteBatchOverride Override(
        this SpriteBatch sb,
        SpriteSortMode? sort = null,
        BlendState blend = null,
        SamplerState sampler = null,
        DepthStencilState depth = null,
        RasterizerState rasterizer = null,
        Effect effect = null,
        Matrix? matrix = null
    )
        => new(
            sb, sort, blend, sampler, depth, rasterizer, effect, matrix
        );

    public readonly ref struct SpriteBatchOverride
    {
        private readonly SpriteBatch _sb;
        private readonly SpriteBatchData _backup;

        public SpriteBatchOverride(
            SpriteBatch sb,
            SpriteSortMode? sort,
            BlendState blend,
            SamplerState sampler,
            DepthStencilState depth,
            RasterizerState rasterizer,
            Effect effect,
            Matrix? matrix
        )
        {
            _sb = sb;

            // Save previous state
            _backup = new SpriteBatchData(sb);

            // Collect new state
            sb.End(out var modified);

            if (sort is not null) modified.SortMode = sort.Value;
            if (blend is not null) modified.BlendState = blend;
            if (sampler is not null) modified.SamplerState = sampler;
            if (depth is not null) modified.DepthStencilState = depth;
            if (rasterizer is not null) modified.RasterizerState = rasterizer;
            if (effect is not null) modified.Effect = effect;
            if (matrix is not null) modified.Matrix = matrix.Value;

            // Restart with modified settings
            sb.Begin(in modified);
        }

        public void Dispose()
        {
            _sb.End();
            _sb.Begin(in _backup); // Restore previous state
        }
    }
}