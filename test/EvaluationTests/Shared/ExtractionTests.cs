using System.Globalization;
using System.Text.Json;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Identity;
using EvaluationTests.Shared.Extraction;
using EvaluationTests.Shared.Markdown;
using Microsoft.Extensions.Configuration;

namespace EvaluationTests.Shared;

public abstract class ExtractionTests<T>
{
    private IConfigurationRoot _configuration;
    private EndpointSettings _documentIntelligenceSettings;
    private DefaultAzureCredential _defaultCredential;
    private readonly JsonSerializerOptions _indentSerializerSettings = new() { WriteIndented = true };

    public virtual void Initialize()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        _documentIntelligenceSettings =
            EndpointSettings.FromConfiguration(_configuration.GetRequiredSection("DocumentIntelligence"));

        _defaultCredential = new DefaultAzureCredential();
    }

    public IDocumentDataExtractor GetDocumentDataExtractor(ExtractionTestCase extractionTest)
    {
        var endpointSettings =
            EndpointSettings.FromConfiguration(_configuration.GetRequiredSection(extractionTest.EndpointSettingKey));

        var markdownConverter = extractionTest.AsMarkdown
            ? new AzureAIDocumentIntelligenceMarkdownConverter(
                new DocumentIntelligenceClient(new Uri(_documentIntelligenceSettings.Endpoint), _defaultCredential))
            : null;
        IDocumentDataExtractor? dataExtractor;

        switch (extractionTest.EndpointType)
        {
            case EndpointType.AzureOpenAI:
                var openAIClient = new OpenAIClient(new Uri(endpointSettings.Endpoint), _defaultCredential);
                var openAIOptions =
                    new ChatCompletionsOptions(
                        endpointSettings.DeploymentName,
                        new List<ChatRequestMessage>
                        {
                            new ChatRequestSystemMessage(extractionTest.ModelConfig.SystemPrompt),
                            new ChatRequestUserMessage(extractionTest.ModelConfig.ExtractionPrompt)
                        })
                    {
                        Temperature = extractionTest.ModelConfig.Temperature,
                        NucleusSamplingFactor = extractionTest.ModelConfig.TopP,
                        MaxTokens = 4096
                    };

                dataExtractor = extractionTest.AsMarkdown
                    ? new AzureOpenAIMarkdownDocumentDataExtractor(
                        openAIClient,
                        openAIOptions,
                        markdownConverter!)
                    : new AzureOpenAIVisionDocumentDataExtractor(
                        openAIClient,
                        openAIOptions);
                break;
            default:
                throw new InvalidOperationException("Invalid endpoint type.");
        }

        return dataExtractor;
    }

    public async Task SaveExtractionDataAsync(string name, DataExtractionResult result)
    {
        if (!Directory.Exists("Output"))
        {
            Directory.CreateDirectory("Output");
        }

        var fileName = $"Output/{name}-{DateTime.UtcNow.ToString("yy-MM-dd", CultureInfo.InvariantCulture)}.json";

        await File.WriteAllTextAsync(fileName, JsonSerializer.Serialize(result, _indentSerializerSettings));
    }

    public record ExtractionTestCase(
        string Name,
        EndpointType EndpointType,
        string EndpointSettingKey,
        ExtractionTestCaseModelConfig ModelConfig,
        byte[] FileBytes,
        bool AsMarkdown,
        T ExpectedData);

    public record ExtractionTestCaseModelConfig(
        string SystemPrompt,
        string ExtractionPrompt,
        float? Temperature,
        float? TopP);
}
