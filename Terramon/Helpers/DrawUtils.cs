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
            spriteBatchData.SortMode, spriteBatchData.BlendState, spriteBatchData.SamplerState, spriteBatchData.DepthStencilState,
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
}

