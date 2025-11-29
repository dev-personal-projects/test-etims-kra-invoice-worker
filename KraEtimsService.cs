using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using QRCoder;

namespace test_etims_kra_invoice_worker;

// Configuration model for KRA eTIMS
public class KraEtimsConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiUsername { get; set; } = string.Empty;
    public string ApiPassword { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public string? DeviceSerialNumber { get; set; }
    
    // API Endpoints (configured based on KRA documentation after sandbox registration)
    public string InvoiceSubmitEndpoint { get; set; } = "/api/oscu/invoice/submit"; // Common patterns: /api/oscu/invoice/submit, /api/v1/invoice, /oscu/api/invoice
    public string InvoiceStatusEndpoint { get; set; } = "/api/oscu/invoice/status"; // Common patterns: /api/oscu/invoice/status/{invoiceNumber}, /api/v1/invoice/status/{invoiceNumber}
    public string DeviceInitEndpoint { get; set; } = "/api/oscu/device/init"; // Device initialization endpoint
}

// Service result model
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
}

// KRA eTIMS Service
public class KraEtimsService
{
    private readonly HttpClient _httpClient;
    private readonly KraEtimsConfig _config;
    private readonly ILogger<KraEtimsService>? _logger;
    private readonly string _outputDirectory;

    public KraEtimsService(HttpClient httpClient, KraEtimsConfig config, ILogger<KraEtimsService>? logger = null)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        
        // Set base address
        // KRA eTIMS API Base URLs:
        // Sandbox: https://etims-api-sbx.kra.go.ke (new format)
        //          https://etims-sbx.kra.go.ke/api (old format - backward compatible)
        // Production: https://etims-api.kra.go.ke
        // Note: Actual endpoint paths are provided in KRA documentation after sandbox registration
        
        // Handle backward compatibility for old BaseUrl format
        var baseUrl = config.BaseUrl;
        if (!string.IsNullOrEmpty(baseUrl))
        {
            // If old format is used (contains /api), remove it as endpoints are now separate
            if (baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.Length - 4);
                _logger?.LogWarning("BaseUrl contains '/api' suffix. This is deprecated. Use base URL without '/api' and configure endpoints separately.");
            }
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        // Set default headers
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Create output directory for generated files
        _outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "generated-invoices");
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    // Submit invoice to KRA eTIMS
    public async Task<ServiceResult<InvoiceResponse>> SubmitInvoiceAsync(InvoiceRequest invoice)
    {
        try
        {
            _logger?.LogInformation("Submitting invoice {InvoiceNumber} to KRA eTIMS", invoice.InvoiceNumber);

            // Validate invoice before submission
            var validation = InvoiceHelper.ValidateInvoice(invoice);
            if (!validation.IsValid)
            {
                return new ServiceResult<InvoiceResponse>
                {
                    Success = false,
                    ErrorMessage = $"Invoice validation failed: {string.Join(", ", validation.Errors)}"
                };
            }

            // Serialize invoice request
            var jsonContent = InvoiceHelper.SerializeInvoiceRequest(invoice);
            
            // Save request JSON to file
            await SaveInvoiceRequestJsonAsync(invoice, jsonContent);

            // Prepare authentication (adjust based on actual KRA API requirements)
            SetAuthenticationHeaders();

            // Create HTTP content
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Submit to KRA API using configured endpoint
            // Actual endpoints are provided in KRA eTIMS documentation after sandbox registration
            // Common patterns: /api/oscu/invoice/submit, /api/v1/invoice, /oscu/api/invoice
            var endpoint = string.IsNullOrWhiteSpace(_config.InvoiceSubmitEndpoint) 
                ? "/api/oscu/invoice/submit" 
                : _config.InvoiceSubmitEndpoint;
            
            _logger?.LogInformation("Submitting to KRA endpoint: {Endpoint}", endpoint);
            var response = await _httpClient.PostAsync(endpoint, content);

            // Read response
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Save response to file immediately (even on errors for debugging)
            await SaveInvoiceResponseJsonAsync(invoice.InvoiceNumber, responseContent, response.IsSuccessStatusCode);

            if (response.IsSuccessStatusCode)
            {
                // Deserialize response
                var invoiceResponse = InvoiceHelper.DeserializeInvoiceResponse(responseContent);
                
                if (invoiceResponse != null && invoiceResponse.Success)
                {
                    // Generate and save QR code
                    await GenerateAndSaveQrCodeAsync(invoiceResponse, invoice.InvoiceNumber);

                    _logger?.LogInformation("Invoice {InvoiceNumber} submitted successfully. KRA Invoice: {KraInvoiceNumber}", 
                        invoice.InvoiceNumber, invoiceResponse.KraInvoiceNumber);

                    return new ServiceResult<InvoiceResponse>
                    {
                        Success = true,
                        Data = invoiceResponse
                    };
                }
                else
                {
                    var errorMsg = invoiceResponse?.Message ?? "Unknown error from KRA API";
                    return new ServiceResult<InvoiceResponse>
                    {
                        Success = false,
                        ErrorMessage = errorMsg,
                        Data = invoiceResponse
                    };
                }
            }
            else
            {
                _logger?.LogError("KRA API returned error: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);

                return new ServiceResult<InvoiceResponse>
                {
                    Success = false,
                    ErrorMessage = $"KRA API error: {response.StatusCode} - {responseContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error while submitting invoice {InvoiceNumber}", invoice.InvoiceNumber);
            return new ServiceResult<InvoiceResponse>
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}",
                Exception = ex
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error submitting invoice {InvoiceNumber}", invoice.InvoiceNumber);
            return new ServiceResult<InvoiceResponse>
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                Exception = ex
            };
        }
    }

    // Generate QR code from KRA response and save as image file
    private async Task GenerateAndSaveQrCodeAsync(InvoiceResponse invoiceResponse, string invoiceNumber)
    {
        try
        {
            // Use QR code data from KRA response, or generate from invoice data
            string qrData = invoiceResponse.QrCodeData ?? 
                           invoiceResponse.QrCode ?? 
                           GenerateQrDataFromInvoice(invoiceResponse);

            if (string.IsNullOrEmpty(qrData))
            {
                _logger?.LogWarning("No QR code data available for invoice {InvoiceNumber}", invoiceNumber);
                return;
            }

            // Generate QR code image
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            // Save QR code as PNG file
            var qrCodeFileName = $"invoice-{invoiceNumber}-qr.png";
            var qrCodePath = Path.Combine(_outputDirectory, qrCodeFileName);
            await File.WriteAllBytesAsync(qrCodePath, qrCodeBytes);

            _logger?.LogInformation("QR code saved to {FilePath}", qrCodePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating QR code for invoice {InvoiceNumber}", invoiceNumber);
        }
    }

    // Generate QR code data string from invoice response
    private string GenerateQrDataFromInvoice(InvoiceResponse invoiceResponse)
    {
        // Format: KRA Invoice Number, Date, Amount, Signature
        // Adjust format based on KRA's actual QR code data structure
        var qrData = new StringBuilder();
        qrData.AppendLine($"Invoice: {invoiceResponse.KraInvoiceNumber ?? invoiceResponse.InvoiceNumber}");
        qrData.AppendLine($"Date: {invoiceResponse.InvoiceDate:yyyy-MM-dd HH:mm:ss}");
        qrData.AppendLine($"Amount: {invoiceResponse.TotalAmount:C}");
        if (!string.IsNullOrEmpty(invoiceResponse.DigitalSignature))
        {
            qrData.AppendLine($"Signature: {invoiceResponse.DigitalSignature}");
        }
        return qrData.ToString();
    }

    // Save invoice request JSON to file
    private async Task SaveInvoiceRequestJsonAsync(InvoiceRequest invoice, string jsonContent)
    {
        try
        {
            var fileName = $"invoice-{invoice.InvoiceNumber}-request.json";
            var filePath = Path.Combine(_outputDirectory, fileName);
            await File.WriteAllTextAsync(filePath, jsonContent, Encoding.UTF8);
            _logger?.LogInformation("Invoice request JSON saved to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving invoice request JSON for {InvoiceNumber}", invoice.InvoiceNumber);
        }
    }

    // Save invoice response JSON to file (always saved, even on errors for debugging)
    private async Task SaveInvoiceResponseJsonAsync(string invoiceNumber, string jsonContent, bool isSuccess = true)
    {
        try
        {
            var fileName = $"invoice-{invoiceNumber}-response.json";
            var filePath = Path.Combine(_outputDirectory, fileName);
            
            // Format error responses better for readability
            string contentToSave = jsonContent;
            if (!isSuccess && jsonContent.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
            {
                // If it's an HTML error page, wrap it in a JSON structure for better readability
                contentToSave = $"{{\n  \"error\": true,\n  \"statusCode\": \"Error\",\n  \"message\": \"KRA API returned an error response\",\n  \"rawResponse\": {JsonSerializer.Serialize(jsonContent)}\n}}";
            }
            
            await File.WriteAllTextAsync(filePath, contentToSave, Encoding.UTF8);
            _logger?.LogInformation("Invoice response JSON saved to {FilePath} (Success: {IsSuccess})", filePath, isSuccess);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving invoice response JSON for {InvoiceNumber}", invoiceNumber);
        }
    }

    // Set authentication headers (adjust based on actual KRA API authentication method)
    private void SetAuthenticationHeaders()
    {
        // Option 1: Basic Authentication
        if (!string.IsNullOrEmpty(_config.ApiUsername) && !string.IsNullOrEmpty(_config.ApiPassword))
        {
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ApiUsername}:{_config.ApiPassword}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }

        // Option 2: Bearer Token (if KRA uses token-based auth)
        // _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Option 3: Custom headers (if KRA requires specific headers)
        // _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
        // _httpClient.DefaultRequestHeaders.Add("X-PIN", _config.Pin);
    }

    // Get invoice status (if KRA API supports this)
    public async Task<ServiceResult<InvoiceResponse>> GetInvoiceStatusAsync(string invoiceNumber)
    {
        try
        {
            // Use configured endpoint pattern, replacing {invoiceNumber} placeholder if present
            var baseEndpoint = string.IsNullOrWhiteSpace(_config.InvoiceStatusEndpoint) 
                ? "/api/oscu/invoice/status" 
                : _config.InvoiceStatusEndpoint;
            
            var endpoint = baseEndpoint.Replace("{invoiceNumber}", invoiceNumber);
            if (!endpoint.Contains(invoiceNumber))
            {
                endpoint = $"{endpoint}/{invoiceNumber}";
            }
            _logger?.LogInformation("Getting invoice status from KRA endpoint: {Endpoint}", endpoint);
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var invoiceResponse = InvoiceHelper.DeserializeInvoiceResponse(responseContent);

                return new ServiceResult<InvoiceResponse>
                {
                    Success = true,
                    Data = invoiceResponse
                };
            }
            else
            {
                return new ServiceResult<InvoiceResponse>
                {
                    Success = false,
                    ErrorMessage = $"Failed to get invoice status: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            return new ServiceResult<InvoiceResponse>
            {
                Success = false,
                ErrorMessage = $"Error getting invoice status: {ex.Message}",
                Exception = ex
            };
        }
    }

    // Get list of generated invoice files
    public List<string> GetGeneratedFiles()
    {
        try
        {
            if (Directory.Exists(_outputDirectory))
            {
                return Directory.GetFiles(_outputDirectory)
                    .Select(f => Path.GetFileName(f))
                    .OrderByDescending(f => f)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting generated files list");
        }
        return new List<string>();
    }

    // Get output directory path
    public string GetOutputDirectory() => _outputDirectory;
}

