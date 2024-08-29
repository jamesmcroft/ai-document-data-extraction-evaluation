using System.Text.Json.Serialization;
using EvaluationTests.Shared.Serialization;

namespace EvaluationTests.Assets.Contracts;

public class VehicleInsuranceContractData
{
    public string? PolicyNumber { get; set; }

    public CostDetails? Cost { get; set; }

    public RenewalDetails? Renewal { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? EffectiveFrom { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? EffectiveTo { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? LastDateToCancel { get; set; }

    public CustomerDetails? Policyholder { get; set; }

    public VehicleDetails? Vehicle { get; set; }

    public ExcessDetails? AccidentExcess { get; set; }

    public ExcessDetails? FireAndTheftExcess { get; set; }

    public static VehicleInsuranceContractData Empty => new()
    {
        PolicyNumber = string.Empty,
        Cost = new CostDetails
        {
            AnnualTotal = 0.0,
            PayableByDate = DateTime.MinValue
        },
        Renewal = new RenewalDetails
        {
            RenewalNotificationDate = DateTime.MinValue,
            RenewalDueDate = DateTime.MinValue
        },
        EffectiveFrom = DateTime.MinValue,
        EffectiveTo = DateTime.MinValue,
        LastDateToCancel = DateTime.MinValue,
        Policyholder = new CustomerDetails
        {
            FirstName = string.Empty,
            LastName = string.Empty,
            DateOfBirth = DateTime.MinValue,
            Address = string.Empty,
            EmailAddress = string.Empty,
            TotalYearsOfResidenceInUK = 0,
            DrivingLicenseNumber = string.Empty
        },
        Vehicle = new VehicleDetails
        {
            RegistrationNumber = string.Empty,
            Make = string.Empty,
            Model = string.Empty,
            Year = 2024,
            Value = 0.0
        },
        AccidentExcess = new ExcessDetails
        {
            Compulsory = 0.0,
            Voluntary = 0.0,
            UnapprovedRepairPenalty = 0.0
        },
        FireAndTheftExcess = new ExcessDetails
        {
            Compulsory = 0.0,
            Voluntary = 0.0,
            UnapprovedRepairPenalty = 0.0
        }
    };

    public class CustomerDetails
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime? DateOfBirth { get; set; }

        public string? Address { get; set; }

        public string? EmailAddress { get; set; }

        public int? TotalYearsOfResidenceInUK { get; set; }

        public string? DrivingLicenseNumber { get; set; }
    }

    public class VehicleDetails
    {
        public string? RegistrationNumber { get; set; }

        public string? Make { get; set; }

        public string? Model { get; set; }

        public int? Year { get; set; }

        public double? Value { get; set; }
    }

    public class CostDetails
    {
        public double? AnnualTotal { get; set; }

        [JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime? PayableByDate { get; set; }
    }

    public class RenewalDetails
    {
        [JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime? RenewalNotificationDate { get; set; }

        [JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime? RenewalDueDate { get; set; }
    }

    public class ExcessDetails
    {
        public double? Compulsory { get; set; }

        public double? Voluntary { get; set; }

        public double? UnapprovedRepairPenalty { get; set; }
    }
}
