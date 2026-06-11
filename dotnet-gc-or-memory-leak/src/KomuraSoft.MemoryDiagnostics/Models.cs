namespace KomuraSoft.MemoryDiagnostics;

/// <summary>
/// 記事のコード例に登場するドメインモデルです。
/// Payload を持たせて、ヒープ増加をデモで観測しやすくしています。
/// </summary>
public sealed class Customer
{
    public Customer(string name)
    {
        Name = name;
        Payload = new byte[8 * 1024];
    }

    public string Name { get; }

    public byte[] Payload { get; }
}

public sealed class Report
{
    public Report(string userId, DateTime date)
    {
        UserId = userId;
        Date = date;
        Payload = new byte[16 * 1024];
    }

    public string UserId { get; }

    public DateTime Date { get; }

    public byte[] Payload { get; }
}

public sealed record RequestAudit(string Path, DateTimeOffset Timestamp);

public sealed record RequestInfo(string RequestId, string UserName);
