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
            player.Terramon().HasExpShare = true;
    }
}

public struct ExpShareSettings
{
    private readonly float[] _relativePercentages = new float[6];
    private readonly byte[] _reverseAmounts = new byte[6];
    private readonly bool[] _disabled = new bool[6];
    private bool _changed = true;

    public ExpShareSettings()
    {
        Recalculate();
    }
    public ExpShareSettings(float initialPercentage)
    {
        Array.Fill(_reverseAmounts, ReverseFromPercentage(initialPercentage));
        Recalculate();
    }
    public void Disable(int i)
    {
        ref var d = ref _disabled[i];
        if (d)
            return;
        _disabled[i] = true;
        _changed = true;
    }
    public void Enable(int i)
    {
        ref var d = ref _disabled[i];
        if (!d)
            return;
        _disabled[i] = false;
        _changed = true;
    }
    public float this[int i]
    {
        readonly get
        {
            Recalculate();
            return _relativePercentages[i];
        }
        set
        {
            byte rev = ReverseFromPercentage(value);
            ref var a = ref _reverseAmounts[i];
            if (a == rev)
                return;
            a = rev;
            _changed = true;
        }
    }
    private static byte ReverseFromPercentage(float initialPercentage)
    {
        initialPercentage = Math.Clamp(initialPercentage, 0f, 1f);
        return (byte)(byte.MaxValue - (byte)(initialPercentage * byte.MaxValue));
    }
    public readonly void Recalculate()
    {
        if (!_changed)
            return;

        int sum = 0;

        for (int i = 0; i < _reverseAmounts.Length; i++)
        {
            if (!_disabled[i])
            {
                var amt = byte.MaxValue - _reverseAmounts[i];
                sum += amt;
                _relativePercentages[i] = amt;
                continue;
            }
            _relativePercentages[i] = 0f;
        }

        if (sum == 0)
            return;

        for (int i = 0; i < _reverseAmounts.Length; i++)
            _relativePercentages[i] /= sum;
    }
}