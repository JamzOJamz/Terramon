using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
    public SpriteBatchData(SpriteBatch spriteBatch)
    {
        if (spriteBatch is null)
            return;

        SortMode = GetSortMode(spriteBatch);
        BlendState = GetBlendState(spriteBatch);
        SamplerState = GetSamplerState(spriteBatch);
        DepthStencilState = GetDepthStencilState(spriteBatch);
        RasterizerState = GetRasterizerState(spriteBatch);
        Effect = GetCustomEffect(spriteBatch);
        Matrix = GetTransformMatrix(spriteBatch);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "sortMode")]
    public static extern ref SpriteSortMode GetSortMode(SpriteBatch self);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "blendState")]
    public static extern ref BlendState GetBlendState(SpriteBatch self);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "samplerState")]
    public static extern ref SamplerState GetSamplerState(SpriteBatch self);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "depthStencilState")]
    public static extern ref DepthStencilState GetDepthStencilState(SpriteBatch self);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "rasterizerState")]
    public static extern ref RasterizerState GetRasterizerState(SpriteBatch self);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "customEffect")]
    public static extern ref Effect GetCustomEffect(SpriteBatch self);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "transformMatrix")]
    public static extern ref Matrix GetTransformMatrix(SpriteBatch self);
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

