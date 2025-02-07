using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class ServiceReferenceExtensions
{
    /// <summary>
    /// Adds an HTTP service reference. Configures a binding between the <typeparamref name="TClient"/> type and a named <see cref="HttpClient"/>
    /// with a base address. The client name will be set to the type name of <typeparamref name="TClient"/>.
    /// </summary>
    /// <typeparam name="TClient">
    /// The type of the typed client. The type specified will be registered in the service collection as a transient service. See
    /// <see cref="Microsoft.Extensions.Http.ITypedHttpClientFactory{TClient}"/> for more details about authoring typed clients.
    /// </typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="baseAddress">The value to assign to the <see cref="HttpClient.BaseAddress"/> property of the typed client's injected <see cref="HttpClient"/> instance.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseAddress"/> is not a valid URI value.</exception>
    public static IHttpClientBuilder AddHttpServiceReference<TClient>(this IServiceCollection services, string baseAddress)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute))
        {
            throw new ArgumentException("Base address must be a valid absolute URI.", nameof(baseAddress));
        }

        return services.AddHttpClient<TClient>(c => c.BaseAddress = new(baseAddress));
    }


    /// <summary>
    /// Adds an HTTP service reference. Configures a binding between the <typeparamref name="TClient"/> type and a named <see cref="HttpClient"/>
    /// with a base address and health check. The client name will be set to the type name of <typeparamref name="TClient"/>.
    /// </summary>
    /// <typeparam name="TClient">
    /// The type of the typed client. The type specified will be registered in the service collection as a transient service. See
    /// <see cref="Microsoft.Extensions.Http.ITypedHttpClientFactory{TClient}"/> for more details about authoring typed clients.
    /// </typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="baseAddress">The value to assign to the <see cref="HttpClient.BaseAddress"/> property of the typed client's injected <see cref="HttpClient"/> instance.</param>
    /// <param name="healthRelativePath">The relative path of the health check endpoint for this HTTP service.</param>
    /// <param name="healthCheckName">The name for the health check. Defaults to <c>"{typeof(TClient).Name}-health"</c> if not provided.</param>
    /// <param name="failureStatus">The <see cref="HealthStatus"/> that should be reported if the health check fails. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseAddress"/> or <paramref name="healthRelativePath"/> are not valid URI values.</exception>
    public static IHttpClientBuilder AddHttpServiceReference<TClient>(this IServiceCollection services, string baseAddress, string healthRelativePath, string? healthCheckName = default, HealthStatus failureStatus = default)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(healthRelativePath);

        if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute))
        {
            throw new ArgumentException("Base address must be a valid absolute URI.", nameof(baseAddress));
        }

        if (!Uri.IsWellFormedUriString(healthRelativePath, UriKind.Relative))
        {
            throw new ArgumentException("Health check path must be a valid relative URI.", nameof(healthRelativePath));
        }

        var uri = new Uri(baseAddress);
        var builder = services.AddHttpClient<TClient>(c => c.BaseAddress = uri);

        services.AddHealthChecks()
            .AddUrlGroup(
                new Uri(uri, healthRelativePath),
                healthCheckName ?? $"{typeof(TClient).Name}-health",
                failureStatus,
                //configureClient: (s, c) => s.GetRequiredService<IHttpClientFactory>().CreateClient
                configurePrimaryHttpMessageHandler: s => s.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler()
                );

        return builder;
    }

}