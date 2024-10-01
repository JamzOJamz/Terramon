using System;
using System.Collections.Generic;

namespace Terramon.Core;

public class Tween
{
    public static readonly List<ITweener> ActiveTweens = [];

    public static ITweener To<T>(Func<T> getter, Action<T> setter, T endValue,
        float time) where T : struct
    {
        return To((getter, setter), tuple => tuple.getter(), (tuple, value) => tuple.setter(value), endValue, time);
    }

    private static Tweener<TFrom, TValue> To<TValue, TFrom>(TFrom from, Func<TFrom, TValue> getter,
        Action<TFrom, TValue> setter,
        TValue endValue, float time) where TValue : struct
    {
        var tweener = new Tweener<TFrom, TValue>
        {
            From = from,
            Setter = setter,
            StartTime = (float)Main.timeForVisualEffects,
            EndTime = (float)Main.timeForVisualEffects + time * 60,
            StartValue = getter.Invoke(from),
            EndValue = endValue
        };
        ActiveTweens.Add(tweener);
        return tweener;
    }
    
    /// <summary>
    ///     Updates all active tweens. Should be called once per frame.
    /// </summary>
    public static void DoUpdate()
    {
        for (var i = 0; i < ActiveTweens.Count; i++)
            if (!ActiveTweens[i].Update())
                ActiveTweens.RemoveAt(i--);
    }
}

public interface ITweener
{
    bool IsRunning { get; }
    Action OnComplete { set; }
    bool Update();
    void Kill();
    ITweener SetEase(Ease easeType);
}

public class Tweener<TFrom, TValue> : ITweener where TValue : struct
{
    private Ease _ease;

    private bool _killed;
    public float EndTime;
    public TValue EndValue;
    public TFrom From;
    public Action<TFrom, TValue> Setter;
    public float StartTime;
    public TValue StartValue;
    public bool IsRunning => !_killed;
    public Action OnComplete { get; set; }

    public bool Update()
    {
        if (_killed) return false;
        var t = (Main.timeForVisualEffects - StartTime) /
                (EndTime - StartTime);
        t = Math.Clamp(t, 0, 1);
        t = ApplyEasing(_ease, t);
        switch (StartValue, EndValue)
        {
            case (float s, float e):
                Setter.Invoke(From, (TValue)Convert.ChangeType(s + (e - s) * t, typeof(TValue)));
                break;
        }

        var isComplete = Main.timeForVisualEffects >= EndTime;
        if (!isComplete) return true;
        Setter.Invoke(From, EndValue);
        _killed = true;
        OnComplete?.Invoke();

        return false;
    }

    public void Kill()
    {
        _killed = true;
        Tween.ActiveTweens.Remove(this);
    }

    public ITweener SetEase(Ease easeType)
    {
        _ease = easeType;
        return this;
    }

    private static double ApplyEasing(Ease easing, double time)
    {
        const double elasticConst = 2 * Math.PI / .3;
        const double elasticConst2 = .3 / 4;

        const double backConst = 1.70158;
        const double backConst2 = backConst * 1.525;

        const double bounceConst = 1 / 2.75;

        switch (easing)
        {
            case Ease.None:
                break;
            default:
                return time;

            case Ease.InQuad:
                return time * time;
            case Ease.OutQuad:
                return time * (2 - time);
            case Ease.InOutQuad:
                if (time < .5) return time * time * 2;
                return --time * time * -2 + 1;

            case Ease.InCubic:
                return time * time * time;
            case Ease.OutCubic:
                return --time * time * time + 1;
            case Ease.InOutCubic:
                if (time < .5) return time * time * time * 4;
                return --time * time * time * 4 + 1;

            case Ease.InQuart:
                return time * time * time * time;
            case Ease.OutQuart:
                return 1 - --time * time * time * time;
            case Ease.InOutQuart:
                if (time < .5) return time * time * time * time * 8;
                return --time * time * time * time * -8 + 1;

            case Ease.InQuint:
                return time * time * time * time * time;
            case Ease.OutQuint:
                return --time * time * time * time * time + 1;
            case Ease.InOutQuint:
                if (time < .5) return time * time * time * time * time * 16;
                return --time * time * time * time * time * 16 + 1;

            case Ease.InSine:
                return 1 - Math.Cos(time * Math.PI * .5);
            case Ease.OutSine:
                return Math.Sin(time * Math.PI * .5);
            case Ease.InOutSine:
                return .5 - .5 * Math.Cos(Math.PI * time);

            case Ease.InExpo:
                return Math.Pow(2, 10 * (time - 1));
            case Ease.OutExpo:
                return -Math.Pow(2, -10 * time) + 1;
            case Ease.InOutExpo:
                if (time < .5) return .5 * Math.Pow(2, 20 * time - 10);
                return 1 - .5 * Math.Pow(2, -20 * time + 10);

            case Ease.InCirc:
                return 1 - Math.Sqrt(1 - time * time);
            case Ease.OutCirc:
                return Math.Sqrt(1 - --time * time);
            case Ease.InOutCirc:
                if ((time *= 2) < 1) return .5 - .5 * Math.Sqrt(1 - time * time);
                return .5 * Math.Sqrt(1 - (time -= 2) * time) + .5;

            case Ease.InElastic:
                return -Math.Pow(2, -10 + 10 * time) * Math.Sin((1 - elasticConst2 - time) * elasticConst);
            case Ease.OutElastic:
                return Math.Pow(2, -10 * time) * Math.Sin((time - elasticConst2) * elasticConst) + 1;
            case Ease.OutElasticHalf:
                return Math.Pow(2, -10 * time) * Math.Sin((.5 * time - elasticConst2) * elasticConst) + 1;
            case Ease.OutElasticQuarter:
                return Math.Pow(2, -10 * time) * Math.Sin((.25 * time - elasticConst2) * elasticConst) + 1;
            case Ease.InOutElastic:
                if ((time *= 2) < 1)
                    return -.5 * Math.Pow(2, -10 + 10 * time) *
                           Math.Sin((1 - elasticConst2 * 1.5 - time) * elasticConst / 1.5);
                return .5 * Math.Pow(2, -10 * --time) * Math.Sin((time - elasticConst2 * 1.5) * elasticConst / 1.5) +
                       1;

            case Ease.InBack:
                return time * time * ((backConst + 1) * time - backConst);
            case Ease.OutBack:
                return --time * time * ((backConst + 1) * time + backConst) + 1;
            case Ease.InOutBack:
                if ((time *= 2) < 1) return .5 * time * time * ((backConst2 + 1) * time - backConst2);
                return .5 * ((time -= 2) * time * ((backConst2 + 1) * time + backConst2) + 2);

            case Ease.InBounce:
                time = 1 - time;
                return time switch
                {
                    < bounceConst => 1 - 7.5625 * time * time,
                    < 2 * bounceConst => 1 - (7.5625 * (time -= 1.5 * bounceConst) * time + .75),
                    < 2.5 * bounceConst => 1 - (7.5625 * (time -= 2.25 * bounceConst) * time + .9375),
                    _ => 1 - (7.5625 * (time -= 2.625 * bounceConst) * time + .984375)
                };
            case Ease.OutBounce:
                return time switch
                {
                    < bounceConst => 7.5625 * time * time,
                    < 2 * bounceConst => 7.5625 * (time -= 1.5 * bounceConst) * time + .75,
                    < 2.5 * bounceConst => 7.5625 * (time -= 2.25 * bounceConst) * time + .9375,
                    _ => 7.5625 * (time -= 2.625 * bounceConst) * time + .984375
                };
            case Ease.InOutBounce:
                if (time < .5) return .5 - .5 * ApplyEasing(Ease.OutBounce, 1 - time * 2);
                return ApplyEasing(Ease.OutBounce, (time - .5) * 2) * .5 + .5;

            case Ease.OutPow10:
                return --time * Math.Pow(time, 10) + 1;
        }

        return time;
    }
}

public enum Ease
{
    None,
    InQuad,
    OutQuad,
    InOutQuad,
    InCubic,
    OutCubic,
    InOutCubic,
    InQuart,
    OutQuart,
    InOutQuart,
    InQuint,
    OutQuint,
    InOutQuint,
    InSine,
    OutSine,
    InOutSine,
    InExpo,
    OutExpo,
    InOutExpo,
    InCirc,
    OutCirc,
    InOutCirc,
    InElastic,
    OutElastic,
    OutElasticHalf,
    OutElasticQuarter,
    InOutElastic,
    InBack,
    OutBack,
    InOutBack,
    InBounce,
    OutBounce,
    InOutBounce,
    OutPow10
}