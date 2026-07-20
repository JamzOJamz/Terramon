using System.Runtime.InteropServices;
using System.Text;

namespace Terramon.Helpers;

/// <summary>
///     Implementation of the Vose alias method for weighted random selection.
/// </summary>
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

    private readonly List<int> _distValues = [];
    private readonly List<double> _distWeights = [];
    private int _count;
    private int[] _alias;
    private double[] _prob;

    public void Add(int element, double weight)
    {
        _distValues.Add(element);
        _distWeights.Add(weight);
        _count++;
        _mustRecalculate = true;
    }

    public int Get()
    {
        if (_mustRecalculate)
            Recalculate();
        var column = Main.rand.Next(_count);
        var coinToss = Main.rand.NextDouble() < _prob[column];
        return _distValues[coinToss ? column : _alias[column]];
    }

    public void Recalculate()
    {
        var n = _count;
        if (_prob is null || n > _prob.Length)
        {
            _prob = new double[n];
            _alias = new int[n];
        }
        else
        {
            Array.Clear(_prob);
            Array.Clear(_alias);
        }

        var sp = CollectionsMarshal.AsSpan(_distWeights);
        var sum = 0d;
        foreach (ref var s in sp)
            sum += s;

        var small = new Queue<int>(n);
        var large = new Queue<int>(n);

        for (int i = 0; i < n; i++)
        {
            var realWeight = sp[i];
            if ((_prob[i] = realWeight / sum * n) < 1d)
                small.Enqueue(i);
            else
                large.Enqueue(i);
        }

        while (small.Count != 0 && large.Count != 0)
        {
            var s = small.Dequeue();
            var l = large.Dequeue();

            _alias[s] = l;

            _prob[l] += _prob[s] - 1d;

            if (_prob[l] < 1d)
                small.Enqueue(l);
            else
                large.Enqueue(l);
        }

        while (large.TryDequeue(out var l))
            _prob[l] = 1d;
        while (small.TryDequeue(out var s))
            _prob[s] = 1d;

        _mustRecalculate = false;
    }

    public override string ToString() => ToString(null);

    public string ToString(Func<int, string> nameResolver)
    {
        if (_mustRecalculate)
            Recalculate();
        var sb = new StringBuilder();
        for (int i = 0; i < _prob.Length; i++)
        {
            var p = _prob[i];
            var toHund = (int)Math.Ceiling(p * 100);
            var val = _distValues[i];
            var name = nameResolver?.Invoke(val) ?? val.ToString();

            sb.Append('|', toHund)
                .Append('-')
                .AppendLine(name);
        }
        return sb.ToString();
    }
}