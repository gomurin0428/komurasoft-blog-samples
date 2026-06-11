namespace KomuraSoft.AlgebraicDataTypes;

// 記事 11 章: Option型 ── null の代わりに「ない」を表す
// .NET Framework でも使える簡単な実装
public abstract class Option<T>
{
    private Option()
    {
    }

    public sealed class Some : Option<T>
    {
        internal Some(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }

    public sealed class None : Option<T>
    {
        internal None()
        {
        }
    }

    private static readonly None NoneValue = new None();

    public static Option<T> Of(T value)
    {
        if (object.Equals(value, null))
        {
            return NoneValue;
        }

        return new Some(value);
    }

    public static Option<T> Empty()
    {
        return NoneValue;
    }

    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none)
    {
        if (some == null) throw new ArgumentNullException(nameof(some));
        if (none == null) throw new ArgumentNullException(nameof(none));

        var s = this as Some;
        if (s != null) return some(s.Value);

        return none();
    }
}
