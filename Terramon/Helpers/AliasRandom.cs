using System.Runtime.InteropServices;
using System.Text;
using Entry = (int Value, double Weight);

namespace Terramon.Helpers;

/// <summary>
///     Vose alias implementation
/// </summary>
/// <param name="elementCount"></param>
public sealed class AliasRandom
{
    private bool _mustRecalculate;
    public double Reverser
    {
        get => field;
        set
        {
            if (field == value) return;
            field = value;
            _mustRecalculate = true;
        }
    }
    private readonly List<Entry> _dist = [];
    private int[] _alias;
    private double[] _prob;
    public void Add(int element, double weight)
    {
        _dist.Add(new Entry(element, weight));
        _mustRecalculate = true;
    }
    public int Get()
    {
        // DEBUG
        _mustRecalculate = true;
        if (_mustRecalculate)
            Recalculate();
        var column = Main.rand.Next(_prob.Length);
        var coinToss = Main.rand.NextDouble() < _prob[column];
        return _dist[coinToss ? column : _alias[column]].Value;
    }
    public void Recalculate()
    {
        // construct probability and alias tables for the distribution
        var n = _dist.Count;
        var prob = new uint[n];
        var alias = new int[n];

        // normally you'd normalize the weights to sum to 1, but we can just use the sum instead of 1
        var sp = CollectionsMarshal.AsSpan(_dist);
        var sum = 0UL;
        foreach (ref var s in sp)
            sum += s.Weight;
        var avg = sum / (uint)n;


        var small = new Queue<int>(n);
        var large = new Queue<int>(n);

        for (int i = 0; i < n; i++)
        {
            // custom weight modifier
            var weight = sp[i].Weight;
            var magnitude = weight - avg;

            var realWeight = avg + (ulong)(magnitude * (Reverser + 1d));
            if ((prob[i] = realWeight / sum * n) < 1d)
                small.Enqueue(i);
            else
                large.Enqueue(i);
        }
        
        while (small.Count != 0 && large.Count != 0)
        {
            var s = small.Dequeue();
            var l = large.Dequeue();

            alias[s] = l;

            prob[l] = prob[l] + prob[s] - 1d;

            if (prob[l] < 1d)
                small.Enqueue(l);
            else
                large.Enqueue(l);
        }

        while (large.TryDequeue(out var l))
            prob[l] = 1d;
        while (small.TryDequeue(out var s))
            prob[s] = 1d;

        _prob = prob;
        _alias = alias;

        _mustRecalculate = false;
    }
    public override string ToString() => ToString(null);
    public string ToString(Func<int, string> nameResolver)
    {
        _mustRecalculate = true;
        if (_mustRecalculate)
            Recalculate();
        var sb = new StringBuilder();
        for (int i = 0; i < _prob.Length; i++)
        {
            var p = _prob[i];
            var toHund = (int)Math.Ceiling(p * 100);
            var val = _dist[i].Value;
            var name = nameResolver?.Invoke(val) ?? val.ToString();

            sb.Append('|', toHund)
                .Append('-')
                .AppendLine(name);
        }
        return sb.ToString();
    }
}
