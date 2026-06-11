using System.Net;
using System.Text;

namespace KomuraSoft.AsyncPatterns.Demo;

/// <summary>
/// 外部ネットワークに出ずに HttpClient のパターンを実演するためのスタブです。
/// URL のパスをそのまま本文として返し、クエリ ?delay=ミリ秒 があればその分待ちます。
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Uri uri = request.RequestUri!;

        const string delayPrefix = "?delay=";
        if (uri.Query.StartsWith(delayPrefix, StringComparison.Ordinal))
        {
            int delayMs = int.Parse(uri.Query[delayPrefix.Length..]);
            await Task.Delay(delayMs, cancellationToken);
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"content of {uri.AbsolutePath}", Encoding.UTF8)
        };
    }
}
