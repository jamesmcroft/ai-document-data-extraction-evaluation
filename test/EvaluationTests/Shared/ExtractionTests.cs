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

    protected TestOutputStorage? OutputStorage { get; set; }

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
        OutputStorage = new TestOutputStorage(
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
                        outputDebug ? OutputStorage : null)
                    : new AzureOpenAIVisionDocumentDataExtractor(
                        openAIClient,
                        openAIOptions,
                        outputDebug ? OutputStorage : null);
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
                        outputDebug ? OutputStorage : null)
                    : throw new NotImplementedException(
                        "Vision-based AzureMLServerless data extractor is not implemented.");
                break;
            default:
                throw new InvalidOperationException("Invalid endpoint type.");
        }

        if (OutputStorage != null && outputDebug)
        {
            SaveTestAsync(extractionTest).ConfigureAwait(false);
        }

        return dataExtractor;
    }

    public async Task SaveTestAsync(ExtractionTestCase testCase)
    {
        await OutputStorage.SaveJsonAsync(testCase,
            $"{DateTime.UtcNow.ToString("yy-MM-dd", CultureInfo.InvariantCulture)}.Test.json");
    }

    public async Task SaveResultAsync<TResult>(TResult result)
        where TResult : ExtractionTestCaseResult
    {
        await OutputStorage.SaveJsonAsync(result,
            $"{DateTime.UtcNow.ToString("yy-MM-dd", CultureInfo.InvariantCulture)}.Result.json");
    }

    /// <summary>
    /// Defines a test case for data extraction.
    /// </summary>
    /// <param name="Name">The name of the test case, for reference.</param>
    /// <param name="EndpointType">The type of endpoint being used (Azure OpenAI, or Azure AI Studio Serverless).</param>
    /// <param name="EndpointSettingKey">The key associated with the appsettings.json configuration to use for the test.</param>
    /// <param name="ModelConfig">The configuration for the request to the endpoint for data extraction.</param>
    /// <param name="FileBytes">The bytes of the file being processed.</param>
    /// <param name="AsMarkdown">A value indicating whether to use Azure AI Document Intelligence prebuilt-layout to Markdown feature, or to use the vision capabilities of the supplied model (e.g., GPT-4o, GPT-4-Turbo).</param>
    /// <param name="ExpectedData">The object containing the expected data output to compare for evaluation purposes.</param>
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
