using KomuraSoft.AlgebraicDataTypes.Documents;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 16 章: メソッドのシグネチャが仕様になる
public class GetDocumentResultTests
{
    [Fact]
    public void MatchはFoundケースを処理する()
    {
        var document = new Document(new DocumentId("DOC-001"), "設計書");
        GetDocumentResult result = GetDocumentResult.DocumentFound(document);

        string response = result.Match(
            found => "200 " + found.Document.Title,
            notFound => "404",
            forbidden => "403");

        Assert.Equal("200 設計書", response);
    }

    [Fact]
    public void MatchはNotFoundケースを処理する()
    {
        GetDocumentResult result = GetDocumentResult.DocumentNotFound(new DocumentId("DOC-404"));

        string response = result.Match(
            found => "200",
            notFound => "404 " + notFound.Id.Value,
            forbidden => "403");

        Assert.Equal("404 DOC-404", response);
    }

    [Fact]
    public void MatchはForbiddenケースを処理する()
    {
        GetDocumentResult result = GetDocumentResult.AccessForbidden(new UserId("U-0001"));

        string response = result.Match(
            found => "200",
            notFound => "404",
            forbidden => "403 " + forbidden.UserId.Value);

        Assert.Equal("403 U-0001", response);
    }
}
