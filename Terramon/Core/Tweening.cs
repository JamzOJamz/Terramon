namespace Terramon.Core;

public static class Tween
{
    public const float TweenStep = 1f / 60f;
    public static readonly List<ITweener> ActiveTweens = [];

    public static float SimulationTime { get; private set; }

    public static ITweener To<T>(T startValue, Action<T> setter, T endValue, float time) where T : struct
    {
        return To((startValue, setter), tuple => tuple.startValue, (tuple, v) => tuple.setter(v), endValue, time);
    }

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
            StartTime = SimulationTime,
            EndTime = SimulationTime + time,
            StartValue = getter.Invoke(from),
            EndValue = endValue
        };
        ActiveTweens.Add(tweener);
        return tweener;
    }

    public static void DoUpdate(float elapsedSeconds)
    {
        SimulationTime += elapsedSeconds;
        for (var i = 0; i < ActiveTweens.Count; i++)
            if (!ActiveTweens[i].Update())
                ActiveTweens.RemoveAt(i--);
    }

    public static float ApplyEasing(Ease easing, float time)
    {
        return ApplyEasing(easing, time, EaseParams.Default);
    }

    public static float ApplyEasing(Ease easing, float time, EaseParams p)
    {
        var backConstant = p.BackConstant == 0f ? EaseParams.Default.BackConstant : p.BackConstant;
        return ApplyEasingCore(easing, time, backConstant);
    }

    private static float ApplyEasingCore(Ease easing, float time, float backConstant)
    {
        var backConst = backConstant;
        var backConst2 = backConst * 1.525f;

        const float elasticConst = 2f * MathF.PI / .3f;
        const float elasticConst2 = .3f / 4f;
        const float bounceConst = 1f / 2.75f;

        switch (easing)
        {
            case Ease.None:
            default:
                return time;

            case Ease.InQuad:
                return time * time;
            case Ease.OutQuad:
                return time * (2f - time);
            case Ease.InOutQuad:
                if (time < .5f) return time * time * 2f;
                return --time * time * -2f + 1f;

            case Ease.InCubic:
                return time * time * time;
            case Ease.OutCubic:
                return --time * time * time + 1f;
            case Ease.InOutCubic:
                if (time < .5f) return time * time * time * 4f;
                return --time * time * time * 4f + 1f;

            case Ease.InQuart:
                return time * time * time * time;
            case Ease.OutQuart:
                return 1f - --time * time * time * time;
            case Ease.InOutQuart:
                if (time < .5f) return time * time * time * time * 8f;
                return --time * time * time * time * -8f + 1f;

            case Ease.InQuint:
                return time * time * time * time * time;
            case Ease.OutQuint:
                return --time * time * time * time * time + 1f;
            case Ease.InOutQuint:
                if (time < .5f) return time * time * time * time * time * 16f;
                return --time * time * time * time * time * 16f + 1f;

            case Ease.InSine:
                return 1f - MathF.Cos(time * MathF.PI * .5f);
            case Ease.OutSine:
                return MathF.Sin(time * MathF.PI * .5f);
            case Ease.InOutSine:
                return .5f - .5f * MathF.Cos(MathF.PI * time);

            case Ease.InExpo:
                return MathF.Pow(2f, 10f * (time - 1f));
            case Ease.OutExpo:
                return -MathF.Pow(2f, -10f * time) + 1f;
            case Ease.InOutExpo:
                if (time < .5f) return .5f * MathF.Pow(2f, 20f * time - 10f);
                return 1f - .5f * MathF.Pow(2f, -20f * time + 10f);

            case Ease.InCirc:
                return 1f - MathF.Sqrt(1f - time * time);
            case Ease.OutCirc:
                return MathF.Sqrt(1f - --time * time);
            case Ease.InOutCirc:
                if ((time *= 2f) < 1f) return .5f - .5f * MathF.Sqrt(1f - time * time);
                return .5f * MathF.Sqrt(1f - (time -= 2f) * time) + .5f;

            case Ease.InElastic:
                return -MathF.Pow(2f, -10f + 10f * time) * MathF.Sin((1f - elasticConst2 - time) * elasticConst);
            case Ease.OutElastic:
                return MathF.Pow(2f, -10f * time) * MathF.Sin((time - elasticConst2) * elasticConst) + 1f;
            case Ease.OutElasticHalf:
                return MathF.Pow(2f, -10f * time) * MathF.Sin((.5f * time - elasticConst2) * elasticConst) + 1f;
            case Ease.OutElasticQuarter:
                return MathF.Pow(2f, -10f * time) * MathF.Sin((.25f * time - elasticConst2) * elasticConst) + 1f;
            case Ease.InOutElastic:
                if ((time *= 2f) < 1f)
                    return -.5f * MathF.Pow(2f, -10f + 10f * time) *
                           MathF.Sin((1f - elasticConst2 * 1.5f - time) * elasticConst / 1.5f);
                return .5f * MathF.Pow(2f, -10f * --time) *
                    MathF.Sin((time - elasticConst2 * 1.5f) * elasticConst / 1.5f) + 1f;

            case Ease.InBack:
                return time * time * ((backConst + 1f) * time - backConst);
            case Ease.OutBack:
                return --time * time * ((backConst + 1f) * time + backConst) + 1f;
            case Ease.InOutBack:
                if ((time *= 2f) < 1f) return .5f * time * time * ((backConst2 + 1f) * time - backConst2);
                return .5f * ((time -= 2f) * time * ((backConst2 + 1f) * time + backConst2) + 2f);

            case Ease.InBounce:
                time = 1f - time;
                return time switch
                {
                    < bounceConst => 1f - 7.5625f * time * time,
                    < 2f * bounceConst => 1f - (7.5625f * (time -= 1.5f * bounceConst) * time + .75f),
                    < 2.5f * bounceConst => 1f - (7.5625f * (time -= 2.25f * bounceConst) * time + .9375f),
                    _ => 1f - (7.5625f * (time -= 2.625f * bounceConst) * time + .984375f)
                };
            case Ease.OutBounce:
                return time switch
                {
                    < bounceConst => 7.5625f * time * time,
                    < 2f * bounceConst => 7.5625f * (time -= 1.5f * bounceConst) * time + .75f,
                    < 2.5f * bounceConst => 7.5625f * (time -= 2.25f * bounceConst) * time + .9375f,
                    _ => 7.5625f * (time -= 2.625f * bounceConst) * time + .984375f
                };
            case Ease.InOutBounce:
                if (time < .5f) return .5f - .5f * ApplyEasingCore(Ease.OutBounce, 1f - time * 2f, backConstant);
                return ApplyEasingCore(Ease.OutBounce, (time - .5f) * 2f, backConstant) * .5f + .5f;

            case Ease.InBackExpo:
                var backBase = time * time * ((backConst + 1f) * time - backConst);
                var expoMultiplier = 0.1f + 0.9f * MathF.Pow(2f, 8f * (time - 1f));
                return backBase * expoMultiplier;

            case Ease.OutPow10:
                return --time * MathF.Pow(time, 10f) + 1f;
        }
    }
}

public interface ITweener
{
    bool IsRunning { get; }
    Action OnComplete { set; }
    bool Update();
    void Kill();
    ITweener SetEase(Ease easeType);
    ITweener SetEase(Ease easeType, EaseParams parameters);
}

public struct EaseParams
{
    public float BackConstant;

    public static EaseParams Default { get; } = new() { BackConstant = 1.70158f };

    public static EaseParams Back(float overshoot = 1.70158f)
    {
        return new EaseParams
        {
            BackConstant = overshoot
        };
    }
}

public class Tweener<TFrom, TValue> : ITweener where TValue : struct
{
    public float EndTime;
    public TValue EndValue;
    public TFrom From;
    public Action<TFrom, TValue> Setter;
    public float StartTime;
    public TValue StartValue;
    private Ease _ease;
    private EaseParams _easeParams = EaseParams.Default;
    private bool _killed;
    public bool IsRunning => !_killed;
    public Action OnComplete { get; set; }

    public bool Update()
    {
        if (_killed) return false;

        var currentTime = Tween.SimulationTime;

        const float maxTime = 216000f;
        float timeDiff;

        if (EndTime >= StartTime)
            timeDiff = EndTime - StartTime;
        else
            timeDiff = maxTime - StartTime + EndTime;

        var t = (currentTime - StartTime) / timeDiff;

        if (currentTime < StartTime) t = (currentTime + maxTime - StartTime) / timeDiff;

        t = Math.Clamp(t, 0f, 1f);
        t = Tween.ApplyEasing(_ease, t, _easeParams);

        switch (StartValue, EndValue)
        {
            case (float s, float e):
                Setter.Invoke(From, (TValue)Convert.ChangeType(s + (e - s) * t, typeof(TValue)));
                break;
            case (double s, double e):
                Setter.Invoke(From, (TValue)Convert.ChangeType(s + (e - s) * t, typeof(TValue)));
                break;
            case (int s, int e):
                Setter.Invoke(From, (TValue)Convert.ChangeType((int)MathF.Round(s + (e - s) * t), typeof(TValue)));
                break;
            case (Vector2 s, Vector2 e):
                Setter.Invoke(From, (TValue)(object)Vector2.Lerp(s, e, t));
                break;
            case (Color s, Color e):
                Setter.Invoke(From, (TValue)(object)Color.Lerp(s, e, t));
                break;
            default:
                throw new NotSupportedException(
                    $"Tween<{typeof(TValue).Name}> is not supported. Add an interpolation case to {nameof(Tweener<,>)}.{nameof(Update)}.");
        }

        var isComplete = currentTime >= EndTime;
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
        _easeParams = EaseParams.Default;
        return this;
    }

    public ITweener SetEase(Ease easeType, EaseParams parameters)
    {
        _ease = easeType;
        _easeParams = parameters;
        return this;
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
    OutPow10,
    InBackExpo
}
