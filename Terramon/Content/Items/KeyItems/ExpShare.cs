namespace Terramon.Content.Items;

public sealed class ExpShare : KeyItem
{
    public bool Enabled;
    public override void SetDefaults()
    {
        base.SetDefaults();
        Enabled = true;
    }
    public override bool ConsumeItem(Player player) => false;
    public override bool CanRightClick() => true;
    public override void RightClick(Player player)
    {
        Enabled = !Enabled;
        // shift + right click to open exp share settings UI
        // (probably in client config if not its own UI)
    }
    public override void UpdateInventory(Player player)
    {
        if (Enabled)
            player.Terramon().ExpShareOn = true;
    }
}

public struct ExpShareSettings
{
    private float BaseMultiplier = 1f;
    private readonly byte[] _reverseAmounts = new byte[6];
    public readonly float[] RelativePercentages = new float[6];
    public readonly bool[] Disabled = new bool[6];
    public ExpShareSettings()
    {

    }
    public ExpShareSettings(float mult)
    {
        BaseMultiplier = mult;
    }
    public readonly float this[int i]
    {
        get => RelativePercentages[i];
        set => RelativePercentages[i] = ReverseFromPercentage(value);
    }
    private static byte ReverseFromPercentage(float initialPercentage)
    {
        initialPercentage = Math.Clamp(initialPercentage, 0f, 1f);
        return (byte)(byte.MaxValue - (byte)(initialPercentage * byte.MaxValue));
    }
    /// <summary>
    ///     Recalculates the relative percentages (real EXP multipliers) based on the given <see cref="TerramonPlayer.Party"/>
    /// </summary>
    /// <param name="p">The <see cref="TerramonPlayer.Party"/> to base the calculations on</param>
    /// <param name="countParticipants">
    ///     Controls how Pokémon are counted based on their participation in the battle
    ///     <list type="bullet">
    ///         <item>
    ///             <see langword="true"/>: Only Pokémon who participated in battle are counted
    ///         </item>
    ///         <item>
    ///             <see langword="false"/>: Only Pokémon who didn't participate in battle are counted
    ///         </item>
    ///         <item>
    ///             <see langword="null"/>: Both are counted.
    ///         </item>
    ///     </list>
    /// </param>
    public readonly void Recalculate(PokemonData[] p, bool? countParticipants)
    {
        var sum = 0;

        for (int i = 0; i < _reverseAmounts.Length; i++)
        {
            var mon = p[i];
            if (!Disabled[i] && mon != null &&
                mon.Status != NonVolatileStatus.Fnt &&
                (countParticipants.HasValue ? countParticipants.Value == mon.Participated : true))
            {
                var amt = byte.MaxValue - _reverseAmounts[i];
                sum += amt;
                RelativePercentages[i] = amt;
                continue;
            }
            RelativePercentages[i] = 0f;
        }

        if (sum == 0)
            return;

        for (int i = 0; i < _reverseAmounts.Length; i++)
        {
            ref var relativePercentage = ref RelativePercentages[i];
            relativePercentage = relativePercentage / sum * BaseMultiplier;
        }
    }
}