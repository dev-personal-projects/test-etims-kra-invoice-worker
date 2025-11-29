using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace test_etims_kra_invoice_worker;

// Customer Information Model
public class Customer
{
    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("customerPin")]
    public string? CustomerPin { get; set; }

    [JsonPropertyName("customerAddress")]
    public string? CustomerAddress { get; set; }

    [JsonPropertyName("customerPhone")]
    public string? CustomerPhone { get; set; }

    [JsonPropertyName("customerEmail")]
    public string? CustomerEmail { get; set; }
}

// Tax Information Model
public class TaxInfo
{
    [JsonPropertyName("taxCategory")]
    public string TaxCategory { get; set; } = "A"; // Standard VAT category

    [JsonPropertyName("taxRate")]
    [Range(0, 100)]
    public decimal TaxRate { get; set; } = 16.0m; // Standard VAT rate in Kenya

    [JsonPropertyName("taxAmount")]
    public decimal TaxAmount { get; set; }

    [JsonPropertyName("taxableAmount")]
    public decimal TaxableAmount { get; set; }
}

// Payment Information Model
public class PaymentInfo
{
    [JsonPropertyName("paymentMode")]
    public string PaymentMode { get; set; } = "CASH"; // CASH, CARD, MOBILE, etc.

    [JsonPropertyName("paymentAmount")]
    public decimal PaymentAmount { get; set; }

    [JsonPropertyName("paymentReference")]
    public string? PaymentReference { get; set; }
}

// Invoice Line Item Model
public class InvoiceLineItem
{
    [Required]
    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("itemCode")]
    public string? ItemCode { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    [JsonPropertyName("taxRate")]
    public decimal TaxRate { get; set; } = 16.0m;

    [JsonPropertyName("discount")]
    public decimal Discount { get; set; } = 0m;

    [JsonPropertyName("lineTotal")]
    public decimal LineTotal => (Quantity * UnitPrice) - Discount;

    [JsonPropertyName("taxAmount")]
    public decimal TaxAmount => LineTotal * (TaxRate / 100);
}

// Invoice Request Model for KRA eTIMS API
public class InvoiceRequest
{
    [Required]
    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("invoiceDate")]
    public DateTime InvoiceDate { get; set; } = DateTime.Now;

    [Required]
    [JsonPropertyName("customer")]
    public Customer Customer { get; set; } = new();

    [Required]
    [MinLength(1)]
    [JsonPropertyName("lineItems")]
    public List<InvoiceLineItem> LineItems { get; set; } = new();

    [Required]
    [JsonPropertyName("taxInfo")]
    public TaxInfo TaxInfo { get; set; } = new();

    [Required]
    [JsonPropertyName("paymentInfo")]
    public PaymentInfo PaymentInfo { get; set; } = new();

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "KES";

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    // Calculated properties
    [JsonPropertyName("subTotal")]
    public decimal SubTotal => LineItems.Sum(item => item.LineTotal);

    [JsonPropertyName("totalTax")]
    public decimal TotalTax => LineItems.Sum(item => item.TaxAmount);

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount => SubTotal + TotalTax;
}

// Invoice Response Model from KRA eTIMS API
public class InvoiceResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("kraInvoiceNumber")]
    public string? KraInvoiceNumber { get; set; }

    [JsonPropertyName("invoiceDate")]
    public DateTime? InvoiceDate { get; set; }

    [JsonPropertyName("qrCode")]
    public string? QrCode { get; set; }

    [JsonPropertyName("qrCodeData")]
    public string? QrCodeData { get; set; }

    [JsonPropertyName("digitalSignature")]
    public string? DigitalSignature { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }

    [JsonPropertyName("taxAmount")]
    public decimal? TaxAmount { get; set; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }
}

// Helper class for invoice operations
public static class InvoiceHelper
{
    // Validate invoice request
    public static ValidationResult ValidateInvoice(InvoiceRequest invoice)
    {
        var errors = new List<string>();

        if (invoice == null)
        {
            return new ValidationResult { IsValid = false, Errors = new List<string> { "Invoice request cannot be null" } };
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            errors.Add("Invoice number is required");
        }

        if (invoice.Customer == null || string.IsNullOrWhiteSpace(invoice.Customer.CustomerName))
        {
            errors.Add("Customer name is required");
        }

        if (invoice.LineItems == null || invoice.LineItems.Count == 0)
        {
            errors.Add("At least one line item is required");
        }
        else
        {
            // Validate line items
            for (int i = 0; i < invoice.LineItems.Count; i++)
            {
                var item = invoice.LineItems[i];
                if (string.IsNullOrWhiteSpace(item.ItemName))
                {
                    errors.Add($"Line item {i + 1}: Item name is required");
                }
                if (item.Quantity <= 0)
                {
                    errors.Add($"Line item {i + 1}: Quantity must be greater than zero");
                }
                if (item.UnitPrice < 0)
                {
                    errors.Add($"Line item {i + 1}: Unit price cannot be negative");
                }
            }
        }

        // Validate totals match
        var calculatedSubTotal = invoice.LineItems?.Sum(item => item.LineTotal) ?? 0m;
        var calculatedTax = invoice.LineItems?.Sum(item => item.TaxAmount) ?? 0m;
        var calculatedTotal = calculatedSubTotal + calculatedTax;

        if (Math.Abs(invoice.SubTotal - calculatedSubTotal) > 0.01m)
        {
            errors.Add("Subtotal calculation mismatch");
        }

        if (Math.Abs(invoice.TotalTax - calculatedTax) > 0.01m)
        {
            errors.Add("Tax calculation mismatch");
        }

        if (Math.Abs(invoice.TotalAmount - calculatedTotal) > 0.01m)
        {
            errors.Add("Total amount calculation mismatch");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    // Calculate tax for invoice
    public static TaxInfo CalculateTax(List<InvoiceLineItem> lineItems, decimal taxRate = 16.0m)
    {
        var taxableAmount = lineItems.Sum(item => item.LineTotal);
        var taxAmount = taxableAmount * (taxRate / 100);

        return new TaxInfo
        {
            TaxCategory = "A",
            TaxRate = taxRate,
            TaxableAmount = taxableAmount,
            TaxAmount = taxAmount
        };
    }

    // Serialize invoice request to JSON
    public static string SerializeInvoiceRequest(InvoiceRequest invoice)
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(invoice, options);
    }

    // Deserialize invoice response from JSON
    public static InvoiceResponse? DeserializeInvoiceResponse(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<InvoiceResponse>(json, options);
    }

    // Create invoice request from simple data
    public static InvoiceRequest CreateInvoiceRequest(
        string invoiceNumber,
        string customerName,
        List<(string itemName, decimal quantity, decimal unitPrice, decimal taxRate)> items,
        string paymentMode = "CASH",
        string? customerPin = null)
    {
        var lineItems = items.Select(item => new InvoiceLineItem
        {
            ItemName = item.itemName,
            Quantity = item.quantity,
            UnitPrice = item.unitPrice,
            TaxRate = item.taxRate
        }).ToList();

        var defaultTaxRate = lineItems.Count > 0 ? lineItems[0].TaxRate : 16.0m;
        var taxInfo = CalculateTax(lineItems, defaultTaxRate);

        var invoice = new InvoiceRequest
        {
            InvoiceNumber = invoiceNumber,
            InvoiceDate = DateTime.Now,
            Customer = new Customer
            {
                CustomerName = customerName,
                CustomerPin = customerPin
            },
            LineItems = lineItems,
            TaxInfo = taxInfo,
            PaymentInfo = new PaymentInfo
            {
                PaymentMode = paymentMode,
                PaymentAmount = lineItems.Sum(item => item.LineTotal + item.TaxAmount)
            }
        };

        return invoice;
    }

    // Format invoice for display
    public static string FormatInvoiceSummary(InvoiceRequest invoice)
    {
        return $"""
            Invoice Number: {invoice.InvoiceNumber}
            Date: {invoice.InvoiceDate:yyyy-MM-dd HH:mm:ss}
            Customer: {invoice.Customer.CustomerName}
            Items: {invoice.LineItems.Count}
            Subtotal: {invoice.SubTotal:C}
            Tax: {invoice.TotalTax:C}
            Total: {invoice.TotalAmount:C}
            """;
    }
}

// Validation result model
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

