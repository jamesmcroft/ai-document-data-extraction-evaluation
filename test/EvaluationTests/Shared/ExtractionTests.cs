using System.Globalization;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Identity;
using EvaluationTests.Shared.Extraction;
using EvaluationTests.Shared.Extraction.AzureML;
using EvaluationTests.Shared.Extraction.AzureOpenAI;
using EvaluationTests.Shared.Markdown;
using EvaluationTests.Shared.Storage;
using Microsoft.Extensions.Configuration;

namespace EvaluationTests.Shared;

public abstract class ExtractionTests<TData>
{
    private IConfigurationRoot _configuration;
    private EndpointSettings _documentIntelligenceSettings;
    private DefaultAzureCredential _defaultCredential;
    private TestOutputStorage _outputStorage;

    public virtual void Initialize()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        _documentIntelligenceSettings =
            EndpointSettings.FromConfiguration(_configuration.GetRequiredSection("DocumentIntelligence"));

        _defaultCredential = new DefaultAzureCredential();
    }

    public IDocumentDataExtractor GetDocumentDataExtractor(ExtractionTestCase extractionTest, bool outputDebug = false)
    {
        _outputStorage = new TestOutputStorage(
            extractionTest.Name,
            extractionTest.EndpointSettingKey,
            extractionTest.AsMarkdown);

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
                        markdownConverter!,
                        outputDebug ? _outputStorage : null)
                    : new AzureOpenAIVisionDocumentDataExtractor(
                        openAIClient,
                        openAIOptions,
                        outputDebug ? _outputStorage : null);
                break;
            case EndpointType.AzureMLServerless:
                var azureMLClient =
                    new AzureMLServerlessClient(new Uri(endpointSettings.Endpoint), endpointSettings.ApiKey!);
                var azureMLOptions = new AzureMLServerlessChatCompletionOptions
                {
                    Messages =
                    [
                        new("user", extractionTest.ModelConfig.SystemPrompt),
                        new("user", extractionTest.ModelConfig.ExtractionPrompt)
                    ],
                    Temperature = extractionTest.ModelConfig.Temperature,
                    NucleusSamplingFactor = extractionTest.ModelConfig.TopP,
                    MaxTokens = 6144
                };

                dataExtractor = extractionTest.AsMarkdown
                    ? new AzureMLServerlessMarkdownDocumentDataExtractor(
                        azureMLClient,
                        azureMLOptions,
                        markdownConverter!,
                        outputDebug ? _outputStorage : null)
                    : throw new NotImplementedException(
                        "Vision-based AzureMLServerless data extractor is not implemented.");
                break;
            default:
                throw new InvalidOperationException("Invalid endpoint type.");
        }

        return dataExtractor;
    }

    public async Task SaveResultAsync<TResult>(TResult result)
        where TResult : ExtractionTestCaseResult
    {
        await _outputStorage.SaveJsonAsync(result,
            $"{DateTime.UtcNow.ToString("yy-MM-dd", CultureInfo.InvariantCulture)}.Result.json");
    }

    public record ExtractionTestCase(
        string Name,
        EndpointType EndpointType,
        string EndpointSettingKey,
        ExtractionTestCaseModelConfig ModelConfig,
        byte[] FileBytes,
        bool AsMarkdown,
        TData ExpectedData);

    public record ExtractionTestCaseResult(
        DataExtractionResult Result,
        string ExecutionTime);

    public record ExtractionTestCaseModelConfig(
        string SystemPrompt,
        string ExtractionPrompt,
        float? Temperature,
        float? TopP);
}
