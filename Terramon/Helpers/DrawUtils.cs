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

public static class DrawUtils
{
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