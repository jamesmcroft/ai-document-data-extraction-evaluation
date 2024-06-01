using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using EvaluationTests.Assets.Invoices;
using EvaluationTests.Shared;
using EvaluationTests.Shared.Extraction;

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


        await TestContext.Out.WriteLineAsync($"Prompt Tokens: {result.PromptTokens}");
        await TestContext.Out.WriteLineAsync($"Completion Tokens: {result.CompletionTokens}");
        await TestContext.Out.WriteLineAsync($"Time Elapsed: {stopwatch.Elapsed}");

        var actualData = result.Data as InvoiceData;
        var accuracy = ValidateExtractedData(test.ExpectedData, actualData);

        await SaveResultAsync(
            $"{test.Name}-{test.EndpointSettingKey}-{test.AsMarkdown}",
            new InvoiceExtractionTestCaseResult(result, accuracy, stopwatch.Elapsed.ToString("g", CultureInfo.InvariantCulture)));
    }

    private static InvoiceDataAccuracy ValidateExtractedData(InvoiceData expectedData, InvoiceData? actualData)
    {
        var accuracy = new InvoiceDataAccuracy();

        if (actualData is null)
        {
            return accuracy;
        }

        accuracy.InvoiceNumber = string.Equals(actualData.InvoiceNumber, expectedData.InvoiceNumber, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        accuracy.PurchaseOrderNumber = string.Equals(actualData.PurchaseOrderNumber, expectedData.PurchaseOrderNumber, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        accuracy.CustomerName = string.Equals(actualData.CustomerName, expectedData.CustomerName, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        accuracy.CustomerAddress = string.Equals(actualData.CustomerAddress, expectedData.CustomerAddress, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        accuracy.DeliveryDate = actualData.DeliveryDate == expectedData.DeliveryDate ? 1 : 0;
        accuracy.PayableBy = actualData.PayableBy == expectedData.PayableBy ? 1 : 0;
        accuracy.TotalProductQuantity = actualData.TotalProductQuantity == expectedData.TotalProductQuantity ? 1 : 0;
        accuracy.TotalProductPrice = actualData.TotalProductPrice == expectedData.TotalProductPrice ? 1 : 0;

        if (actualData.Products is null)
        {
            accuracy.ProductsOverall = expectedData.Products is null ? 1 : 0;
        }
        else
        {
            if (expectedData.Products is null)
            {
                accuracy.ProductsOverall = 0;
            }
            else
            {
                accuracy.Products = actualData.Products.Select(p =>
                {
                    var expectedProduct = expectedData.Products.FirstOrDefault(ep => ep.Id == p.Id);

                    return new InvoiceDataAccuracy.InvoiceDataProductAccuracy
                    {
                        Id = string.Equals(p.Id, expectedProduct?.Id, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                        Description =
                            string.Equals(p.Description, expectedProduct?.Description,
                                StringComparison.OrdinalIgnoreCase)
                                ? 1
                                : 0,
                        UnitPrice = p.UnitPrice == expectedProduct?.UnitPrice ? 1 : 0,
                        Quantity = p.Quantity == expectedProduct?.Quantity ? 1 : 0,
                        Total = p.Total == expectedProduct?.Total ? 1 : 0,
                        Reason = string.Equals(p.Reason, expectedProduct?.Reason, StringComparison.OrdinalIgnoreCase)
                            ? 1
                            : 0
                    };
                });

                accuracy.ProductsOverall = accuracy.Products.Average(p => new List<double>
                {
                    p.Id,
                    p.Description,
                    p.UnitPrice,
                    p.Quantity,
                    p.Total,
                    p.Reason
                }.Average());
            }
        }

        if (actualData.Returns is null)
        {
            accuracy.ReturnsOverall = expectedData.Returns is null ? 1 : 0;
        }
        else
        {
            if (expectedData.Returns is null)
            {
                accuracy.ReturnsOverall = 0;
            }
            else
            {
                accuracy.Returns = actualData.Returns.Select(p =>
                {
                    var expectedReturn = expectedData.Returns.FirstOrDefault(ep => ep.Id == p.Id);

                    return new InvoiceDataAccuracy.InvoiceDataProductAccuracy
                    {
                        Id = string.Equals(p.Id, expectedReturn?.Id, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                        Description =
                            string.Equals(p.Description, expectedReturn?.Description,
                                                               StringComparison.OrdinalIgnoreCase)
                                ? 1
                                : 0,
                        UnitPrice = p.UnitPrice == expectedReturn?.UnitPrice ? 1 : 0,
                        Quantity = p.Quantity == expectedReturn?.Quantity ? 1 : 0,
                        Total = p.Total == expectedReturn?.Total ? 1 : 0,
                        Reason = string.Equals(p.Reason, expectedReturn?.Reason, StringComparison.OrdinalIgnoreCase)
                            ? 1
                            : 0
                    };
                });

                accuracy.ReturnsOverall = accuracy.Returns.Average(p => new List<double>
                {
                    p.Id,
                    p.Description,
                    p.UnitPrice,
                    p.Quantity,
                    p.Total,
                    p.Reason
                }.Average());
            }
        }

        if (actualData.ProductsSignatures is null)
        {
            accuracy.ProductsSignaturesOverall = expectedData.ProductsSignatures is null ? 1 : 0;
        }
        else
        {
            if (expectedData.ProductsSignatures is null)
            {
                accuracy.ProductsSignaturesOverall = 0;
            }
            else
            {
                accuracy.ProductsSignatures = actualData.ProductsSignatures.Select(p =>
                {
                    var expectedSignature = expectedData.ProductsSignatures.FirstOrDefault(ep => ep.Type == p.Type);

                    return new InvoiceDataAccuracy.InvoiceDataSignatureAccuracy
                    {
                        Type = string.Equals(p.Type, expectedSignature?.Type, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                        Name = string.Equals(p.Name, expectedSignature?.Name, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                        IsSigned = p.IsSigned == expectedSignature?.IsSigned ? 1 : 0
                    };
                });

                accuracy.ProductsSignaturesOverall = accuracy.ProductsSignatures.Average(p => new List<double>
                {
                    p.Type,
                    p.Name,
                    p.IsSigned
                }.Average());
            }
        }

        if (actualData.ReturnsSignatures is null)
        {
            accuracy.ReturnsSignaturesOverall = expectedData.ReturnsSignatures is null ? 1 : 0;
        }
        else
        {
            if (expectedData.ReturnsSignatures is null)
            {
                accuracy.ReturnsSignaturesOverall = 0;
            }
            else
            {
                accuracy.ReturnsSignatures = actualData.ReturnsSignatures.Select(p =>
                {
                    var expectedSignature = expectedData.ReturnsSignatures.FirstOrDefault(ep => ep.Type == p.Type);

                    return new InvoiceDataAccuracy.InvoiceDataSignatureAccuracy
                    {
                        Type = string.Equals(p.Type, expectedSignature?.Type, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                        Name = string.Equals(p.Name, expectedSignature?.Name, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                        IsSigned = p.IsSigned == expectedSignature?.IsSigned ? 1 : 0
                    };
                });

                accuracy.ReturnsSignaturesOverall = accuracy.ReturnsSignatures.Average(p => new List<double>
                {
                    p.Type,
                    p.Name,
                    p.IsSigned
                }.Average());
            }
        }

        return accuracy;
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
            InvoiceNumber = "3847192",
            PurchaseOrderNumber = "15931",
            CustomerName = "Sharp Consulting",
            CustomerAddress = "73 Regal Way, Leeds, LS1 5AB, UK",
            DeliveryDate = new DateTime(2024, 5, 16),
            PayableBy = DateTime.MinValue,
            Products =
                new List<InvoiceData.InvoiceDataProduct>
                {
                    new() { Id = "MA197", UnitPrice = 16.62, Quantity = 5, Total = 83.10 },
                    new() { Id = "ST4086", UnitPrice = 2.49, Quantity = 10, Total = 24.90 },
                    new() { Id = "JF9912413BF", UnitPrice = 15.46, Quantity = 12, Total = 185.52 }
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

    public record InvoiceExtractionTestCaseResult(DataExtractionResult Result, InvoiceDataAccuracy Accuracy, string ExecutionTime) : ExtractionTestCaseResult(Result, ExecutionTime);

    public record InvoiceDataAccuracy
    {
        public double Overall => new List<double>
        {
            InvoiceNumber,
            PurchaseOrderNumber,
            CustomerName,
            CustomerAddress,
            DeliveryDate,
            PayableBy,
            ProductsOverall,
            ReturnsOverall,
            TotalProductQuantity,
            TotalProductPrice,
            ProductsSignaturesOverall,
            ReturnsSignaturesOverall
        }.Average();

        public double InvoiceNumber { get; set; }

        public double PurchaseOrderNumber { get; set; }

        public double CustomerName { get; set; }

        public double CustomerAddress { get; set; }

        public double DeliveryDate { get; set; }

        public double PayableBy { get; set; }

        public IEnumerable<InvoiceDataProductAccuracy>? Products { get; set; }

        public double ProductsOverall { get; set; }

        public IEnumerable<InvoiceDataProductAccuracy>? Returns { get; set; }

        public double ReturnsOverall { get; set; }

        public double TotalProductQuantity { get; set; }

        public double TotalProductPrice { get; set; }

        public IEnumerable<InvoiceDataSignatureAccuracy>? ProductsSignatures { get; set; }

        public double ProductsSignaturesOverall { get; set; }

        public IEnumerable<InvoiceDataSignatureAccuracy>? ReturnsSignatures { get; set; }

        public double ReturnsSignaturesOverall { get; set; }

        public record InvoiceDataProductAccuracy
        {
            public double Id { get; set; }

            public double Description { get; set; }

            public double UnitPrice { get; set; }

            public double Quantity { get; set; }

            public double Total { get; set; }

            public double Reason { get; set; }
        }

        public record InvoiceDataSignatureAccuracy
        {
            public double Type { get; set; }

            public double Name { get; set; }

            public double IsSigned { get; set; }
        }
    }
}
