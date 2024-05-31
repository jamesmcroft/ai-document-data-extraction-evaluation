using Microsoft.Extensions.Configuration;

namespace EvaluationTests.Shared;

public class EndpointSettings(
    string endpoint,
    string? apiKey = null,
    string? deploymentName = null)
{
    public string Endpoint { get; init; } = endpoint;

    public string? ApiKey { get; init; } = apiKey;

    public string? DeploymentName { get; init; } = deploymentName;

    public static EndpointSettings FromConfiguration(IConfiguration configuration)
    {
        var configEndpoint = configuration.GetValue<string>(nameof(Endpoint)) ??
                             throw new InvalidOperationException(
                                 $"{nameof(Endpoint)} is not configured.");

        return new EndpointSettings(
            configEndpoint,
            configuration.GetValue<string>(nameof(ApiKey)),
            configuration.GetValue<string>(nameof(DeploymentName)));
    }
}
