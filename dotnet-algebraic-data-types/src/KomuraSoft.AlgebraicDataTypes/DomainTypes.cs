namespace KomuraSoft.AlgebraicDataTypes;

// 記事の各章のコードが参照している、補助的なドメイン型・値オブジェクトです。
// 記事本文では省略されている部分を、サンプルが動くように最小限の形で定義しています。

public sealed class User
{
    public User(string id, string name)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        if (name == null) throw new ArgumentNullException(nameof(name));
        Id = id;
        Name = name;
    }

    public string Id { get; }
    public string Name { get; }
}

public sealed class Session
{
    public Session(string token)
    {
        if (token == null) throw new ArgumentNullException(nameof(token));
        Token = token;
    }

    public string Token { get; }
}

public sealed class UserId
{
    public UserId(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public sealed class OrderId
{
    public OrderId(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public sealed class DocumentId
{
    public DocumentId(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public sealed class Document
{
    public Document(DocumentId id, string title)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        if (title == null) throw new ArgumentNullException(nameof(title));
        Id = id;
        Title = title;
    }

    public DocumentId Id { get; }
    public string Title { get; }
}

public sealed class MemberId
{
    public MemberId(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public sealed class ReservationId
{
    public ReservationId(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public sealed class Sku
{
    public Sku(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public interface IClock
{
    DateTimeOffset Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
