namespace test_etims_kra_invoice_worker;

// Invoice API Endpoints
public static class InvoiceEndpoints
{
    // Map all invoice-related endpoints
    public static void MapInvoiceEndpoints(this WebApplication app)
    {
        // Health check endpoint
        app.MapGet("/", () => Results.Ok(new
        {
            service = "KRA eTIMS Invoice Worker",
            status = "running",
            version = "1.0.0"
        }))
        .WithName("HealthCheck")
        .WithTags("Health");

        // Submit invoice to KRA eTIMS
        app.MapPost("/api/invoice/submit", async (InvoiceRequest request, KraEtimsService service) =>
        {
            var result = await service.SubmitInvoiceAsync(request);

            if (result.Success && result.Data != null)
            {
                return Results.Ok(new
                {
                    success = true,
                    message = "Invoice submitted successfully",
                    data = result.Data
                });
            }

            return Results.BadRequest(new
            {
                success = false,
                message = result.ErrorMessage ?? "Failed to submit invoice",
                errors = result.Exception?.Message
            });
        })
        .WithName("SubmitInvoice")
        .WithTags("Invoice")
        .Accepts<InvoiceRequest>("application/json")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest);

        // Get invoice status
        app.MapGet("/api/invoice/status/{invoiceNumber}", async (string invoiceNumber, KraEtimsService service) =>
        {
            var result = await service.GetInvoiceStatusAsync(invoiceNumber);

            if (result.Success && result.Data != null)
            {
                return Results.Ok(new
                {
                    success = true,
                    data = result.Data
                });
            }

            return Results.NotFound(new
            {
                success = false,
                message = result.ErrorMessage ?? "Invoice not found"
            });
        })
        .WithName("GetInvoiceStatus")
        .WithTags("Invoice")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status404NotFound);

        // List generated invoice files
        app.MapGet("/api/invoice/files", (KraEtimsService service) =>
        {
            var files = service.GetGeneratedFiles();
            var outputDir = service.GetOutputDirectory();

            return Results.Ok(new
            {
                outputDirectory = outputDir,
                fileCount = files.Count,
                files = files
            });
        })
        .WithName("GetGeneratedFiles")
        .WithTags("Invoice")
        .Produces<object>(StatusCodes.Status200OK);
    }
}

