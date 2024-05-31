using System.Text.Json.Serialization;
using EvaluationTests.Shared.Serialization;

namespace EvaluationTests.Assets.Invoices;

public class InvoiceData
{
    public string? InvoiceNumber { get; set; }

    public string? PurchaseOrderNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerAddress { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? DeliveryDate { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? PayableBy { get; set; }

    public IEnumerable<InvoiceDataProduct>? Products { get; set; }

    public IEnumerable<InvoiceDataProduct>? Returns { get; set; }

    public double? TotalProductQuantity { get; set; }

    public double? TotalProductPrice { get; set; }

    public IEnumerable<InvoiceDataSignature>? ProductsSignatures { get; set; }

    public IEnumerable<InvoiceDataSignature>? ReturnsSignatures { get; set; }

    public static InvoiceData Empty => new()
    {
        InvoiceNumber = string.Empty,
        PurchaseOrderNumber = string.Empty,
        CustomerName = string.Empty,
        CustomerAddress = string.Empty,
        DeliveryDate = DateTime.MinValue,
        PayableBy = DateTime.MinValue,
        Products =
            new List<InvoiceDataProduct>
            {
                new()
                {
                    Id = string.Empty,
                    Description = string.Empty,
                    UnitPrice = 0.0,
                    Quantity = 0.0,
                    Total = 0.0
                }
            },
        Returns =
            new List<InvoiceDataProduct> { new() { Id = string.Empty, Quantity = 0.0, Reason = string.Empty } },
        TotalProductQuantity = 0,
        TotalProductPrice = 0,
        ProductsSignatures =
            new List<InvoiceDataSignature>
            {
                new() { Type = "Customer", Name = string.Empty, IsSigned = false },
                new() { Type = "Driver", Name = string.Empty, IsSigned = false }
            },
        ReturnsSignatures = new List<InvoiceDataSignature>
        {
            new() { Type = string.Empty, Name = string.Empty, IsSigned = false }
        }
    };

    public class InvoiceDataProduct
    {
        public string? Id { get; set; }

        public string? Description { get; set; }

        public double? UnitPrice { get; set; }

        public double Quantity { get; set; }

        public double? Total { get; set; }

        public string? Reason { get; set; }
    }

    public class InvoiceDataSignature
    {
        public string? Type { get; set; }

        public string? Name { get; set; }

        public bool? IsSigned { get; set; }
    }
}
