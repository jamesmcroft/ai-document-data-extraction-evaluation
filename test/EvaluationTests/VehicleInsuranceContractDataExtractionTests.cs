using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using EvaluationTests.Assets.Contracts;
using EvaluationTests.Shared;
using EvaluationTests.Shared.Conversion;
using EvaluationTests.Shared.Extraction;

namespace EvaluationTests;

public class VehicleInsuranceContractDataExtractionTests
    : ExtractionTests<VehicleInsuranceContractData>
{
    [OneTimeSetUp]
    public override void Initialize()
    {
        base.Initialize();
    }

    [TestCaseSource(nameof(TestCases)), CancelAfter(180000)]
    public async Task Extract(ExtractionTestCase test)
    {
        // Arrange
        var dataExtractor = GetDocumentDataExtractor(test, true);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Act
        var result = (await dataExtractor.FromDocumentBytesAsync(test.FileBytes))
            .Deserialize<VehicleInsuranceContractData>();

        // Assert
        stopwatch.Stop();

        var actualData = result.Data as VehicleInsuranceContractData;
        var accuracy = ValidateExtractedData(test.ExpectedData, actualData);

        await TestContext.Out.WriteLineAsync($"Prompt Tokens: {result.PromptTokens}");
        await TestContext.Out.WriteLineAsync($"Completion Tokens: {result.CompletionTokens}");
        await TestContext.Out.WriteLineAsync($"Time Elapsed: {stopwatch.Elapsed}");
        await TestContext.Out.WriteLineAsync($"Accuracy: {accuracy.Overall:P}");

        await SaveResultAsync(new VehicleInsuranceContractExtractionTestCaseResult(result, accuracy, stopwatch.Elapsed.ToString("g", CultureInfo.InvariantCulture)));
    }

    private static VehicleInsuranceContractDataAccuracy ValidateExtractedData(VehicleInsuranceContractData expectedData,
        VehicleInsuranceContractData? actualData)
    {
        var accuracy = new VehicleInsuranceContractDataAccuracy();

        if (actualData is null)
        {
            return accuracy;
        }

        accuracy.PolicyNumber = string.Equals(actualData.PolicyNumber, expectedData.PolicyNumber,
            StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;

        if (actualData.Cost is null)
        {
            accuracy.Cost = expectedData.Cost is null
                ? new VehicleInsuranceContractDataAccuracy.CostDetailsAccuracy
                {
                    AnnualTotal = 1,
                    PayableByDate = 1
                }
                : new VehicleInsuranceContractDataAccuracy.CostDetailsAccuracy();
        }
        else
        {
            if (expectedData.Cost is null)
            {
                accuracy.Cost = new VehicleInsuranceContractDataAccuracy.CostDetailsAccuracy();
            }
            else
            {
                accuracy.Cost = new VehicleInsuranceContractDataAccuracy.CostDetailsAccuracy
                {
                    AnnualTotal = actualData.Cost.AnnualTotal == expectedData.Cost.AnnualTotal ? 1 : 0,
                    PayableByDate = actualData.Cost.PayableByDate == expectedData.Cost.PayableByDate ? 1 : 0
                };
            }
        }

        if (actualData.Renewal is null)
        {
            accuracy.Renewal = expectedData.Renewal is null
                ? new VehicleInsuranceContractDataAccuracy.RenewalDetailsAccuracy
                {
                    RenewalNotificationDate = 1,
                    RenewalDueDate = 1
                }
                : new VehicleInsuranceContractDataAccuracy.RenewalDetailsAccuracy();
        }
        else
        {
            if (expectedData.Renewal is null)
            {
                accuracy.Renewal = new VehicleInsuranceContractDataAccuracy.RenewalDetailsAccuracy();
            }
            else
            {
                accuracy.Renewal = new VehicleInsuranceContractDataAccuracy.RenewalDetailsAccuracy
                {
                    RenewalNotificationDate =
                        actualData.Renewal.RenewalNotificationDate == expectedData.Renewal.RenewalNotificationDate
                            ? 1
                            : 0,
                    RenewalDueDate = actualData.Renewal.RenewalDueDate == expectedData.Renewal.RenewalDueDate ? 1 : 0
                };
            }
        }

        accuracy.EffectiveFrom = actualData.EffectiveFrom == expectedData.EffectiveFrom ? 1 : 0;
        accuracy.EffectiveTo = actualData.EffectiveTo == expectedData.EffectiveTo ? 1 : 0;
        accuracy.LastDateToCancel = actualData.LastDateToCancel == expectedData.LastDateToCancel ? 1 : 0;

        if (actualData.Policyholder is null)
        {
            accuracy.Policyholder = expectedData.Policyholder is null
                ? new VehicleInsuranceContractDataAccuracy.CustomerDetailsAccuracy
                {
                    FirstName = 1,
                    DateOfBirth = 1,
                    Address = 1,
                    EmailAddress = 1,
                    YearsOfResidenceInUK = 1,
                    DrivingLicenseNumber = 1
                }
                : new VehicleInsuranceContractDataAccuracy.CustomerDetailsAccuracy();
        }
        else
        {
            if (expectedData.Policyholder is null)
            {
                accuracy.Policyholder = new VehicleInsuranceContractDataAccuracy.CustomerDetailsAccuracy();
            }
            else
            {
                accuracy.Policyholder = new VehicleInsuranceContractDataAccuracy.CustomerDetailsAccuracy
                {
                    FirstName = string.Equals(actualData.Policyholder.FirstName, expectedData.Policyholder.FirstName,
                        StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 0,
                    LastName = string.Equals(actualData.Policyholder.LastName, expectedData.Policyholder.LastName,
                        StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 0,
                    DateOfBirth = actualData.Policyholder.DateOfBirth == expectedData.Policyholder.DateOfBirth ? 1 : 0,
                    Address = string.Equals(actualData.Policyholder.Address, expectedData.Policyholder.Address,
                        StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 0,
                    EmailAddress = string.Equals(actualData.Policyholder.EmailAddress,
                        expectedData.Policyholder.EmailAddress,
                        StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 0,
                    YearsOfResidenceInUK =
                        actualData.Policyholder.TotalYearsOfResidenceInUK ==
                        expectedData.Policyholder.TotalYearsOfResidenceInUK
                            ? 1
                            : 0,
                    DrivingLicenseNumber = string.Equals(actualData.Policyholder.DrivingLicenseNumber,
                        expectedData.Policyholder.DrivingLicenseNumber,
                        StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 0
                };
            }
        }

        if (actualData.Vehicle is null)
        {
            accuracy.Vehicle = expectedData.Vehicle is null
                ? new VehicleInsuranceContractDataAccuracy.VehicleDetailsAccuracy
                {
                    RegistrationNumber = 1,
                    Make = 1,
                    Model = 1,
                    Year = 1,
                    Value = 1
                }
                : new VehicleInsuranceContractDataAccuracy.VehicleDetailsAccuracy();
        }
        else
        {
            if (expectedData.Vehicle is null)
            {
                accuracy.Vehicle = new VehicleInsuranceContractDataAccuracy.VehicleDetailsAccuracy();
            }
            else
            {
                accuracy.Vehicle = new VehicleInsuranceContractDataAccuracy.VehicleDetailsAccuracy
                {
                    RegistrationNumber = string.Equals(actualData.Vehicle.RegistrationNumber,
                        expectedData.Vehicle.RegistrationNumber,
                        StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 0,
                    Make = string.Equals(actualData.Vehicle.Make, expectedData.Vehicle.Make,
                        StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 0,
                    Model = string.Equals(actualData.Vehicle.Model, expectedData.Vehicle.Model,
                        StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 0,
                    Year = actualData.Vehicle.Year == expectedData.Vehicle.Year ? 1 : 0,
                    Value = actualData.Vehicle.Value == expectedData.Vehicle.Value ? 1 : 0
                };
            }
        }

        if (actualData.AccidentExcess is null)
        {
            accuracy.AccidentExcess = expectedData.AccidentExcess is null
                ? new VehicleInsuranceContractDataAccuracy.ExcessDetailsAccuracy
                {
                    Compulsory = 1,
                    Voluntary = 1,
                    UnapprovedRepairerPenalty = 1
                }
                : new VehicleInsuranceContractDataAccuracy.ExcessDetailsAccuracy();
        }
        else
        {
            if (expectedData.AccidentExcess is null)
            {
                accuracy.AccidentExcess = new VehicleInsuranceContractDataAccuracy.ExcessDetailsAccuracy();
            }
            else
            {
                accuracy.AccidentExcess = new VehicleInsuranceContractDataAccuracy.ExcessDetailsAccuracy
                {
                    Compulsory = actualData.AccidentExcess.Compulsory == expectedData.AccidentExcess.Compulsory
                        ? 1
                        : 0,
                    Voluntary = actualData.AccidentExcess.Voluntary == expectedData.AccidentExcess.Voluntary
                        ? 1
                        : 0,
                    UnapprovedRepairerPenalty =
                        actualData.AccidentExcess.UnapprovedRepairPenalty ==
                        expectedData.AccidentExcess.UnapprovedRepairPenalty
                            ? 1
                            : 0
                };
            }
        }

        if (actualData.FireAndTheftExcess is null)
        {
            accuracy.FireAndTheftExcess = expectedData.FireAndTheftExcess is null
                ? new VehicleInsuranceContractDataAccuracy.ExcessDetailsAccuracy
                {
                    Compulsory = 1,
                    Voluntary = 1,
                    UnapprovedRepairerPenalty = 1
                }
                : new VehicleInsuranceContractDataAccuracy.ExcessDetailsAccuracy();
        }
        else
        {
            if (expectedData.FireAndTheftExcess is null)
            {
                accuracy.FireAndTheftExcess = new VehicleInsuranceContractDataAccuracy.ExcessDetailsAccuracy();
            }
            else
            {
                accuracy.FireAndTheftExcess = new VehicleInsuranceContractDataAccuracy.ExcessDetailsAccuracy
                {
                    Compulsory = actualData.FireAndTheftExcess.Compulsory == expectedData.FireAndTheftExcess.Compulsory
                        ? 1
                        : 0,
                    Voluntary = actualData.FireAndTheftExcess.Voluntary == expectedData.FireAndTheftExcess.Voluntary
                        ? 1
                        : 0,
                    UnapprovedRepairerPenalty =
                        actualData.FireAndTheftExcess.UnapprovedRepairPenalty ==
                        expectedData.FireAndTheftExcess.UnapprovedRepairPenalty
                            ? 1
                            : 0
                };
            }
        }

        return accuracy;
    }


    public static ExtractionTestCase[] TestCases()
    {
        return InsurancePolicyInference();
    }

    private static ExtractionTestCase[] InsurancePolicyInference()
    {
        const string testName = nameof(InsurancePolicyInference);

        const string systemPrompt =
            "You are an AI assistant that extracts data from documents and returns them as structured JSON objects. Do not return as a code block.";
        var extractPrompt =
            $"Extract the data from this contract using the provided JSON structure only. Only provide values for the fields in the structure. If a value is not present, provide null. Values in the structure may be inferred based on other values and rules defined in the text. Use the following structure: {JsonSerializer.Serialize(VehicleInsuranceContractData.Empty)}";
        const float temperature = 0.1f;
        const float topP = 0.1f;

        var fileBytes = File.ReadAllBytes(Path.Combine("Assets", "Contracts", "Simple.pdf"));
        var expectedOutput = new VehicleInsuranceContractData
        {
            PolicyNumber = "GB20246717948",
            Cost =
                new VehicleInsuranceContractData.CostDetails
                {
                    AnnualTotal = 532.19,
                    PayableByDate = new DateTime(2024, 6, 3, 0, 0, 0, DateTimeKind.Utc).AddDays(10)
                },
            Renewal = new VehicleInsuranceContractData.RenewalDetails
            {
                RenewalNotificationDate = new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc).AddDays(-21),
                RenewalDueDate = new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc).AddDays(-7)
            },
            EffectiveFrom = new DateTime(2024, 6, 3, 10, 41, 0, DateTimeKind.Utc),
            EffectiveTo = new DateTime(2025, 6, 2, 23, 59, 0, DateTimeKind.Utc),
            LastDateToCancel = new DateTime(2024, 6, 3, 0, 0, 0, DateTimeKind.Utc).AddDays(14),
            Policyholder =
                new VehicleInsuranceContractData.CustomerDetails
                {
                    FirstName = "Joe",
                    LastName = "Bloggs",
                    DateOfBirth = new DateTime(1990, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                    Address = "73 Regal Way, LEEDS, West Yorkshire, LS1 5AB",
                    EmailAddress = "Joe.Bloggs@me.com",
                    TotalYearsOfResidenceInUK =
                        new DateTime(1990, 1, 5, 0, 0, 0, DateTimeKind.Utc).ToCurrentAge(),
                    DrivingLicenseNumber = "BLOGGS901050JJ1AB"
                },
            Vehicle =
                new VehicleInsuranceContractData.VehicleDetails
                {
                    RegistrationNumber = "VS24DMC",
                    Make = "Hyundai",
                    Model = "IONIQ 5 Premium 73 kWh RWD",
                    Year = 2024,
                    Value = 40000
                },
            AccidentExcess =
                new VehicleInsuranceContractData.ExcessDetails
                {
                    Compulsory = 250,
                    Voluntary = 250,
                    UnapprovedRepairPenalty = 250
                },
            FireAndTheftExcess =
                new VehicleInsuranceContractData.ExcessDetails
                {
                    Compulsory = 250,
                    Voluntary = 250,
                    UnapprovedRepairPenalty = 250
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
                    temperature,
                    topP),
                fileBytes,
                AsMarkdown: true,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4Omni",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    temperature,
                    topP),
                fileBytes,
                AsMarkdown: true,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4OmniMini",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    temperature,
                    topP),
                fileBytes,
                AsMarkdown: true,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4Omni",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    temperature,
                    topP),
                fileBytes,
                AsMarkdown: false,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureOpenAI,
                "GPT4OmniMini",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    temperature,
                    topP),
                fileBytes,
                AsMarkdown: false,
                expectedOutput),
            new ExtractionTestCase(
                testName,
                EndpointType.AzureMLServerless,
                "Phi35MiniInstruct",
                new ExtractionTestCaseModelConfig(
                    systemPrompt,
                    extractPrompt,
                    temperature,
                    topP),
                fileBytes,
                AsMarkdown: true,
                expectedOutput)
        ];
    }

    public record VehicleInsuranceContractExtractionTestCaseResult(
        DataExtractionResult Result,
        VehicleInsuranceContractDataAccuracy Accuracy,
        string ExecutionTime) : ExtractionTestCaseResult(Result, ExecutionTime);

    public record VehicleInsuranceContractDataAccuracy
    {
        public double Overall => new List<double>
        {
            PolicyNumber,
            Cost.Overall,
            Renewal.Overall,
            EffectiveFrom,
            EffectiveTo,
            LastDateToCancel,
            Policyholder.Overall,
            Vehicle.Overall,
            AccidentExcess.Overall,
            FireAndTheftExcess.Overall
        }.Average();

        public double PolicyNumber { get; set; }

        public CostDetailsAccuracy Cost { get; set; } = new();

        public RenewalDetailsAccuracy Renewal { get; set; } = new();

        public double EffectiveFrom { get; set; }

        public double EffectiveTo { get; set; }

        public double LastDateToCancel { get; set; }

        public CustomerDetailsAccuracy Policyholder { get; set; } = new();

        public VehicleDetailsAccuracy Vehicle { get; set; } = new();

        public ExcessDetailsAccuracy AccidentExcess { get; set; } = new();

        public ExcessDetailsAccuracy FireAndTheftExcess { get; set; } = new();

        public record CustomerDetailsAccuracy
        {
            public double FirstName { get; set; }

            public double LastName { get; set; }

            public double DateOfBirth { get; set; }

            public double Address { get; set; }

            public double EmailAddress { get; set; }

            public double YearsOfResidenceInUK { get; set; }

            public double DrivingLicenseNumber { get; set; }

            public double Overall => new List<double>
            {
                FirstName,
                LastName,
                DateOfBirth,
                Address,
                EmailAddress,
                YearsOfResidenceInUK,
                DrivingLicenseNumber
            }.Average();
        }

        public record VehicleDetailsAccuracy
        {
            public double RegistrationNumber { get; set; }

            public double Make { get; set; }

            public double Model { get; set; }

            public double Year { get; set; }

            public double Value { get; set; }

            public double Overall => new List<double>
            {
                RegistrationNumber,
                Make,
                Model,
                Year,
                Value
            }.Average();
        }

        public class CostDetailsAccuracy
        {
            public double AnnualTotal { get; set; }

            public double PayableByDate { get; set; }

            public double Overall => new List<double> { AnnualTotal, PayableByDate }.Average();
        }

        public class RenewalDetailsAccuracy
        {
            public double RenewalNotificationDate { get; set; }

            public double RenewalDueDate { get; set; }

            public double Overall => new List<double> { RenewalNotificationDate, RenewalDueDate }.Average();
        }

        public record ExcessDetailsAccuracy
        {
            public double Compulsory { get; set; }

            public double Voluntary { get; set; }

            public double UnapprovedRepairerPenalty { get; set; }

            public double Overall => new List<double> { Compulsory, Voluntary, UnapprovedRepairerPenalty }.Average();
        }
    }
}
