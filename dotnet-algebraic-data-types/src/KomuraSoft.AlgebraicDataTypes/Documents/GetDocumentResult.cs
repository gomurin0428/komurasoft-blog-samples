namespace KomuraSoft.AlgebraicDataTypes.Documents;

// 記事 16 章: 呼び出し側に処理漏れを意識させられる
// 「見つかった」「見つからない」「権限がない」のいずれかを返す API 契約
public abstract class GetDocumentResult
{
    private GetDocumentResult()
    {
    }

    public sealed class Found : GetDocumentResult
    {
        internal Found(Document document)
        {
            Document = document;
        }

        public Document Document { get; }
    }

    public sealed class NotFound : GetDocumentResult
    {
        internal NotFound(DocumentId id)
        {
            Id = id;
        }

        public DocumentId Id { get; }
    }

    public sealed class Forbidden : GetDocumentResult
    {
        internal Forbidden(UserId userId)
        {
            UserId = userId;
        }

        public UserId UserId { get; }
    }

    public static GetDocumentResult DocumentFound(Document document)
        => new Found(document);

    public static GetDocumentResult DocumentNotFound(DocumentId id)
        => new NotFound(id);

    public static GetDocumentResult AccessForbidden(UserId userId)
        => new Forbidden(userId);

    public T Match<T>(
        Func<Found, T> found,
        Func<NotFound, T> notFound,
        Func<Forbidden, T> forbidden)
    {
        if (found == null) throw new ArgumentNullException(nameof(found));
        if (notFound == null) throw new ArgumentNullException(nameof(notFound));
        if (forbidden == null) throw new ArgumentNullException(nameof(forbidden));

        var f = this as Found;
        if (f != null) return found(f);

        var n = this as NotFound;
        if (n != null) return notFound(n);

        var d = this as Forbidden;
        if (d != null) return forbidden(d);

        throw new InvalidOperationException("Unknown result type: " + GetType().FullName);
    }
}
