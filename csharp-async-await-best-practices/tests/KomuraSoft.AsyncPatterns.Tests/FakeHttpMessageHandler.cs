using System.Net;
using System.Text;

namespace KomuraSoft.AsyncPatterns.Tests;

/// <summary>
/// 外部ネットワークに出ずに HttpClient を検証するためのフェイクです。
/// URL ごとの応答（本文・遅延・ステータス）を登録でき、同時実行数も記録します。
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (string Body, TimeSpan Delay, HttpStatusCode StatusCode)> _responses = [];
    private int _currentConcurrency;
    private int _maxObservedConcurrency;
    private int _requestCount;

    public int MaxObservedConcurrency => _maxObservedConcurrency;

    public int RequestCount => _requestCount;

    public void Register(
        string url,
        string body,
        TimeSpan delay = default,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responses[url] = (body, delay, statusCode);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _requestCount);

        int current = Interlocked.Increment(ref _currentConcurrency);
        InterlockedExtensions.Max(ref _maxObservedConcurrency, current);

        try
        {
            (string body, TimeSpan delay, HttpStatusCode statusCode) =
                _responses[request.RequestUri!.AbsoluteUri];

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8)
            };
        }
        finally
        {
            Interlocked.Decrement(ref _currentConcurrency);
        }
    }

    private static class InterlockedExtensions
    {
        public static void Max(ref int location, int value)
        {
            int current = Volatile.Read(ref location);
            while (value > current)
            {
                int original = Interlocked.CompareExchange(ref location, value, current);
                if (original == current)
                {
                    return;
                }

                current = original;
            }
        }
    }
}
