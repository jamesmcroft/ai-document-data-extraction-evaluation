using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using EvaluationTests.Assets.Invoices;
using EvaluationTests.Shared;

namespace EvaluationTests;

public class InvoiceDataExtractionTests : ExtractionTests<InvoiceData>
{
    [OneTimeSetUp]
    public override void Initialize()
    {
        base.Initialize();
    }

    [TestCaseSource(nameof(TestCases)), Timeout(180000)]
    public async Task Extract(ExtractionTestCase test)
    {
        // Arrange
        var dataExtractor = GetDocumentDataExtractor(test);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Act
        var result = (await dataExtractor.FromDocumentBytesAsync(test.FileBytes)).Deserialize<InvoiceData>();

        // Assert
        stopwatch.Stop();

        await SaveExtractionDataAsync(
            $"{test.Name}-{test.EndpointSettingKey}-{test.AsMarkdown}",
            new ExtractionTestCaseResult(result, stopwatch.Elapsed.ToString("g", CultureInfo.InvariantCulture)));

        await TestContext.Out.WriteLineAsync($"Prompt Tokens: {result.PromptTokens}");
        await TestContext.Out.WriteLineAsync($"Completion Tokens: {result.CompletionTokens}");
        await TestContext.Out.WriteLineAsync($"Time Elapsed: {stopwatch.Elapsed}");

        if (result.Content is null)
        {
            Assert.Fail("Extraction failed.");
        }

        var actualData = result.Data as InvoiceData;
        ValidateExtractedData(test.ExpectedData, actualData);
    }

    private static void ValidateExtractedData(InvoiceData expectedData, InvoiceData? actualData)
    {
        Assert.That(actualData, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(actualData!.InvoiceNumber, Is.EqualTo(expectedData.InvoiceNumber));
            Assert.That(actualData.PurchaseOrderNumber, Is.EqualTo(expectedData.PurchaseOrderNumber));
            Assert.That(actualData.CustomerName, Is.EqualTo(expectedData.CustomerName));
            Assert.That(actualData.CustomerAddress, Is.EqualTo(expectedData.CustomerAddress));
            Assert.That(actualData.DeliveryDate, Is.EqualTo(expectedData.DeliveryDate));
            Assert.That(actualData.PayableBy, Is.EqualTo(expectedData.PayableBy));
            Assert.That(actualData.TotalProductQuantity, Is.EqualTo(expectedData.TotalProductQuantity));
            Assert.That(actualData.TotalProductPrice, Is.EqualTo(expectedData.TotalProductPrice));

            if (expectedData.Products is null)
            {
                Assert.That(actualData.Products, Is.Null);
            }
            else
            {
                Assert.That(actualData.Products.Count, Is.EqualTo(expectedData.Products.Count()));

                foreach (var extractedDataProduct in actualData.Products)
                {
                    var expectedProduct =
                        expectedData.Products.FirstOrDefault(p => p.Id == extractedDataProduct.Id);

                    Assert.That(expectedProduct, Is.Not.Null);
                    Assert.That(extractedDataProduct.Description, Is.EqualTo(expectedProduct!.Description));
                    Assert.That(extractedDataProduct.UnitPrice, Is.EqualTo(expectedProduct!.UnitPrice));
                    Assert.That(extractedDataProduct.Quantity, Is.EqualTo(expectedProduct.Quantity));
                    Assert.That(extractedDataProduct.Total, Is.EqualTo(expectedProduct.Total));
                }
            }

            if (expectedData.Returns is null)
            {
                Assert.That(actualData.Returns, Is.Null);
            }
            else
            {
                Assert.That(actualData.Returns.Count, Is.EqualTo(expectedData.Returns.Count()));

                foreach (var extractedDataReturn in actualData.Returns)
                {
                    var expectedReturn =
                        expectedData.Returns.FirstOrDefault(p => p.Id == extractedDataReturn.Id);

                    Assert.That(expectedReturn, Is.Not.Null);
                    Assert.That(extractedDataReturn.Description, Is.EqualTo(expectedReturn!.Description));
                    Assert.That(extractedDataReturn.Quantity, Is.EqualTo(expectedReturn.Quantity));
                    Assert.That(extractedDataReturn.Reason, Contains.Substring(expectedReturn.Reason));
                }
            }

            if (expectedData.ProductsSignatures is null)
            {
                Assert.That(actualData.ProductsSignatures, Is.Null);
            }
            else
            {
                Assert.That(actualData.ProductsSignatures.Count,
                    Is.EqualTo(expectedData.ProductsSignatures.Count()));

                foreach (var extractedDataSignature in actualData.ProductsSignatures)
                {
                    var expectedSignature =
                        expectedData.ProductsSignatures.FirstOrDefault(
                            p => p.Type == extractedDataSignature.Type);

                    Assert.That(expectedSignature, Is.Not.Null);
                    Assert.That(extractedDataSignature.Name, Contains.Substring(expectedSignature!.Name));
                    Assert.That(extractedDataSignature.IsSigned, Is.EqualTo(expectedSignature.IsSigned));
                }
            }

            if (expectedData.ReturnsSignatures is null)
            {
                Assert.That(actualData.ReturnsSignatures, Is.Null);
            }
            else
            {
                Assert.That(actualData.ReturnsSignatures.Count,
                    Is.EqualTo(expectedData.ReturnsSignatures.Count()));

                foreach (var extractedDataSignature in actualData.ReturnsSignatures)
                {
                    var expectedSignature =
                        expectedData.ReturnsSignatures.FirstOrDefault(p =>
                            p.Type == extractedDataSignature.Type);

                    Assert.That(expectedSignature, Is.Not.Null);
                    Assert.That(extractedDataSignature.Name, Contains.Substring(expectedSignature!.Name));
                    Assert.That(extractedDataSignature.IsSigned, Is.EqualTo(expectedSignature.IsSigned));
                }
            }
        });
    }


    public static ExtractionTestCase[] TestCases()
    {
        return SimpleTestCases().Concat(ComplexWithHandwritingTestCases()).ToArray();
    }

    private static ExtractionTestCase[] SimpleTestCases()
    {
        const string testName = nameof(SimpleTestCases);

        const string systemPrompt =
            "You are an AI assistant that extracts data from documents and returns them as structured JSON objects. Do not return as a code block.";
        var extractPrompt =
            $"Extract the data from this invoice. If a value is not present, provide null. Use the following structure: {JsonSerializer.Serialize(InvoiceData.Empty)}";

        var fileBytes = File.ReadAllBytes(Path.Combine("Assets", "Invoices", "Simple.pdf"));
        var expectedOutput = new InvoiceData
        {
            InvoiceNumber = "3847193",
            PurchaseOrderNumber = "15931",
            CustomerName = "Sharp Consulting",
            CustomerAddress = "73 Regal Way, Leeds, LS1 5AB, UK",
            DeliveryDate = new DateTime(2024, 5, 16),
            PayableBy = DateTime.MinValue,
            Products =
                new List<InvoiceData.InvoiceDataProduct>
                {
                    new()
                    {
                        Id = "MA197",
                        UnitPrice = 16.62,
                        Quantity = 5,
                        Total = 83.10
                    },
                    new()
                    {
                        Id = "ST4086",
                        UnitPrice = 2.49,
                        Quantity = 10,
                        Total = 24.90
                    },
                    new()
                    {
                        Id = "JF9912413BF",
                        UnitPrice = 15.46,
                        Quantity = 12,
                        Total = 185.52
                    }
                },
            Returns = new List<InvoiceData.InvoiceDataProduct>
            {
                new()
                {
                    Id = "MA145",
                    Quantity = 1,
                    Reason = "This item was provided in previous order as a replacement"
                },
                new() { Id = "JF7902", Quantity = 1, Reason = "Not required" }
            },
            TotalProductQuantity = 27,
            TotalProductPrice = 293.52,
            ProductsSignatures = new List<InvoiceData.InvoiceDataSignature>
            {
                new() { Type = "Customer", Name = "", IsSigned = false },
                new() { Type = "Driver", Name = "James T", IsSigned = true }
            },
            ReturnsSignatures = new List<InvoiceData.InvoiceDataSignature>
            {
                new() { Type = "Customer", Name = "", IsSigned = false },
                new() { Type = "Driver", Name = "", IsSigned = false }
            }
        };

        return
        [
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT35Turbo",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                true,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4Turbo",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                true,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4Omni",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                true,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4Turbo",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                false,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4Omni",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                false,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureMLServerless,
                "Phi3Mini128kInstruct",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                true,
                expectedOutput)
        ];
    }

    private static ExtractionTestCase[] ComplexWithHandwritingTestCases()
    {
        const string testName = nameof(ComplexWithHandwritingTestCases);

        const string systemPrompt =
            "You are an AI assistant that extracts data from documents and returns them as structured JSON objects. Do not return as a code block.";
        var extractPrompt =
            $"Extract the data from this invoice. If a value is not present, provide null. Use the following structure: {JsonSerializer.Serialize(InvoiceData.Empty)}";

        var fileBytes = File.ReadAllBytes(Path.Combine("Assets", "Invoices", "ComplexWithHandwriting.pdf"));
        var expectedOutput = new InvoiceData
        {
            InvoiceNumber = "3847193",
            PurchaseOrderNumber = "15931",
            CustomerName = "Sharp Consulting",
            CustomerAddress = "73 Regal Way, Leeds, LS1 5AB, UK",
            DeliveryDate = new DateTime(2024, 5, 16),
            PayableBy = new DateTime(2024, 5, 24),
            Products =
                new List<InvoiceData.InvoiceDataProduct>
                {
                    new()
                    {
                        Id = "MA197",
                        Description = "STRETCHWRAP ROLL",
                        UnitPrice = 16.62,
                        Quantity = 5,
                        Total = 83.10
                    },
                    new()
                    {
                        Id = "ST4086",
                        Description = "BALLPOINT PEN MED.",
                        UnitPrice = 2.49,
                        Quantity = 10,
                        Total = 24.90
                    },
                    new()
                    {
                        Id = "JF9912413BF",
                        Description = "BUBBLE FILM ROLL CL.",
                        UnitPrice = 15.46,
                        Quantity = 12,
                        Total = 185.52
                    }
                },
            Returns = new List<InvoiceData.InvoiceDataProduct>
            {
                new()
                {
                    Id = "MA145",
                    Description = "POSTAL TUBE BROWN",
                    Quantity = 1,
                    Reason = "This item was provided in previous order as a replacement"
                },
                new() { Id = "JF7902", Description = "MAILBOX 25PK", Quantity = 1, Reason = "Not required" }
            },
            TotalProductQuantity = 27,
            TotalProductPrice = 293.52,
            ProductsSignatures = new List<InvoiceData.InvoiceDataSignature>
            {
                new() { Type = "Customer", Name = "Sarah H", IsSigned = true },
                new() { Type = "Driver", Name = "James T", IsSigned = true }
            },
            ReturnsSignatures = new List<InvoiceData.InvoiceDataSignature>
            {
                new() { Type = "Customer", Name = "Sarah H", IsSigned = true },
                new() { Type = "Driver", Name = "James T", IsSigned = true }
            }
        };

        return
        [
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT35Turbo",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                true,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4Turbo",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                true,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4Omni",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                true,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4Turbo",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                false,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4Omni",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                false,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureMLServerless,
                "Phi3Mini128kInstruct",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    0.1f,
                    0.1f),
                fileBytes,
                true,
                expectedOutput)
        ];
    }
}
