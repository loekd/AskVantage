using Polly.Extensions.Http;
using Polly;
using AskVantage.Client.Services;
using Polly.Contrib.WaitAndRetry;

namespace AskVantage.Client;

public static class HttpClientPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider serviceProvider, int retryCount = 3)
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount);

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(delay,
                onRetry: (result, span, index, ctx) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<ImageService>>();
                    logger.LogWarning("Retry #{Index}, Status: {StatusCode}", index, result.Result.StatusCode);
                }
            );
    }

    /// <summary>
    /// Calls to Ollama can take a long time, we need to set a timeout policy to match.
    /// </summary>
    /// <param name="timeoutInSeconds"></param>
    /// <returns></returns>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(int timeoutInSeconds = 90)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(timeoutInSeconds));
    }
}
