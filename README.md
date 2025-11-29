# KRA eTIMS Invoice Worker

A simple, clean .NET 9 Web API for integrating with Kenya Revenue Authority (KRA) eTIMS (Electronic Tax Invoice Management System). This application provides a minimal implementation for generating tax-compliant invoices with KRA eTIMS integration, QR code generation, and automatic file management.

## Features

- **OSCU Integration** - Online Sales Control Unit for real-time invoice submission to KRA
- **QR Code Generation** - Automatically generates QR codes from KRA responses and saves as PNG files
- **File Management** - Automatically saves invoice requests, responses, and QR codes to `generated-invoices/` folder
- **Tax-Compliant Invoices** - Generate invoices with KRA-assigned invoice numbers, QR codes, and digital signatures
- **Minimal API** - Clean, simple structure with minimal files
- **Dev Container Support** - Ready-to-use development environment with .NET 9 SDK

## Project Structure

```
test-etims-kra-invoice-worker/
├── Program.cs              # Main application entry point and service registration
├── InvoiceEndpoints.cs     # API endpoint definitions and routing
├── KraEtimsService.cs      # KRA eTIMS API integration service
├── Invoice.cs              # Invoice data models
├── appsettings.json        # Application configuration
├── appsettings.Development.json
└── README.md
```

## Getting Started

### Prerequisites

- .NET 9 SDK
- KRA eTIMS Sandbox credentials (register at https://etims-sbx.kra.go.ke)

### Configuration

Update `appsettings.json` with your KRA eTIMS credentials and API endpoints:

```json
{
  "KraEtims": {
    "BaseUrl": "https://etims-api-sbx.kra.go.ke",
    "ApiUsername": "your-username",
    "ApiPassword": "your-password",
    "Pin": "your-kra-pin",
    "DeviceSerialNumber": "",
    "InvoiceSubmitEndpoint": "/api/oscu/invoice/submit",
    "InvoiceStatusEndpoint": "/api/oscu/invoice/status",
    "DeviceInitEndpoint": "/api/oscu/device/init"
  }
}
```

**Important Notes:**
- **BaseUrl**: 
  - Sandbox: `https://etims-api-sbx.kra.go.ke`
  - Production: `https://etims-api.kra.go.ke`
- **API Endpoints**: The actual endpoint paths are provided in the KRA eTIMS API documentation after you register for sandbox access. Update the endpoint paths in `appsettings.json` based on the official KRA documentation.
- **Getting Endpoints**: After registering at https://etims-sbx.kra.go.ke, KRA will provide:
  - API documentation with exact endpoint paths
  - Authentication credentials
  - Device initialization requirements

### Running the Application

```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5042`
- HTTPS: `https://localhost:7029`

### API Endpoints

- `GET /` - Health check endpoint
- `POST /api/invoice/submit` - Submit invoice to KRA eTIMS
- `GET /api/invoice/status/{invoiceNumber}` - Get invoice status from KRA
- `GET /api/invoice/files` - List all generated invoice files
- `GET /openapi/v1.json` - OpenAPI specification (Development only)

## KRA eTIMS Integration

This application uses **OSCU (Online Sales Control Unit)** for integration, which means:
- Direct API calls to KRA's cloud service
- Real-time invoice validation and signing
- Requires internet connectivity for invoice submission

For offline capability, consider implementing **VSCU (Virtual Sales Control Unit)** which requires running KRA's Java-based control unit locally.

## Generated Files

When you submit an invoice, the system automatically generates and saves the following files in the `generated-invoices/` directory:

- **Invoice Request JSON** - `invoice-{number}-request.json` - The invoice data sent to KRA
- **Invoice Response JSON** - `invoice-{number}-response.json` - The response received from KRA
- **QR Code Image** - `invoice-{number}-qr.png` - Generated QR code for invoice verification

Example:
```
generated-invoices/
├── invoice-INV001-request.json
├── invoice-INV001-response.json
└── invoice-INV001-qr.png
```

## Usage Example

### Submit an Invoice

```http
POST /api/invoice/submit
Content-Type: application/json

{
  "invoiceNumber": "INV-001",
  "invoiceDate": "2024-11-28T10:00:00Z",
  "customer": {
    "customerName": "John Doe",
    "customerPin": "P123456789X"
  },
  "lineItems": [
    {
      "itemName": "Product A",
      "quantity": 2,
      "unitPrice": 1000.00,
      "taxRate": 16.0
    }
  ],
  "paymentInfo": {
    "paymentMode": "CASH",
    "paymentAmount": 2320.00
  }
}
```

### Response

```json
{
  "success": true,
  "data": {
    "success": true,
    "kraInvoiceNumber": "KRA-INV-12345",
    "qrCode": "base64-encoded-qr-data",
    "digitalSignature": "signature-hash",
    "totalAmount": 2320.00
  }
}
```

The QR code image and JSON files are automatically saved to the `generated-invoices/` folder.

## Testing & API Usage

This section provides complete examples for testing the API using cURL commands. These examples are perfect for new developers to understand the full flow of the application.

### Prerequisites for Testing

1. **Start the API:**
   ```bash
   dotnet run
   ```

2. **Optional - Install jq for pretty JSON output:**
   - Ubuntu/Debian: `sudo apt-get install jq`
   - macOS: `brew install jq`
   - Windows: Download from https://stedolan.github.io/jq/

### Base URL

```bash
BASE_URL="http://localhost:5042"
```

### 1. Health Check

Verify the API is running:

```bash
curl -X GET "http://localhost:5042/" \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
  "service": "KRA eTIMS Invoice Worker",
  "status": "running",
  "version": "1.0.0"
}
```

### 2. Submit Invoice to KRA eTIMS

Submit a complete invoice with customer details, line items, and payment information:

```bash
curl -X POST "http://localhost:5042/api/invoice/submit" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "invoiceNumber": "INV-001",
    "invoiceDate": "2024-11-28T10:00:00Z",
    "customer": {
      "customerName": "John Doe",
      "customerPin": "P123456789X",
      "customerAddress": "123 Main Street, Nairobi",
      "customerPhone": "+254712345678",
      "customerEmail": "john.doe@example.com"
    },
    "lineItems": [
      {
        "itemName": "Product A",
        "itemCode": "PROD-A-001",
        "quantity": 2,
        "unitPrice": 1000.00,
        "taxRate": 16.0,
        "discount": 0
      },
      {
        "itemName": "Product B",
        "itemCode": "PROD-B-002",
        "quantity": 1,
        "unitPrice": 500.00,
        "taxRate": 16.0,
        "discount": 50.00
      }
    ],
    "paymentInfo": {
      "paymentMode": "CASH",
      "paymentAmount": 2900.00,
      "paymentReference": "PAY-REF-001"
    },
    "currency": "KES",
    "remarks": "Test invoice submission"
  }'
```

**Expected Response (Success):**
```json
{
  "success": true,
  "message": "Invoice submitted successfully",
  "data": {
    "success": true,
    "kraInvoiceNumber": "KRA-INV-12345",
    "qrCode": "base64-encoded-data",
    "digitalSignature": "signature-hash",
    "totalAmount": 2900.00
  }
}
```

**What Happens:**
- Invoice request JSON is saved to `generated-invoices/invoice-INV-001-request.json`
- API call is made to KRA eTIMS
- Response JSON is saved to `generated-invoices/invoice-INV-001-response.json`
- QR code PNG is generated and saved to `generated-invoices/invoice-INV-001-qr.png` (on successful response)

### 3. Get Invoice Status

Check the status of a submitted invoice:

```bash
curl -X GET "http://localhost:5042/api/invoice/status/INV-001" \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "success": true,
    "invoiceNumber": "INV-001",
    "kraInvoiceNumber": "KRA-INV-12345",
    "status": "processed"
  }
}
```

### 4. List Generated Invoice Files

View all generated invoice files:

```bash
curl -X GET "http://localhost:5042/api/invoice/files" \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
  "outputDirectory": "/path/to/generated-invoices",
  "fileCount": 3,
  "files": [
    "invoice-INV-001-request.json",
    "invoice-INV-001-response.json",
    "invoice-INV-001-qr.png"
  ]
}
```

### Complete Testing Flow

Run all commands in sequence to test the complete flow:

```bash
# Set base URL and generate unique invoice number
BASE_URL="http://localhost:5042"
INVOICE_NUMBER="INV-$(date +%Y%m%d-%H%M%S)"

# 1. Health Check
echo "=== Health Check ==="
curl -s "$BASE_URL/" | jq '.'

# 2. Submit Invoice
echo -e "\n=== Submitting Invoice ==="
curl -s -X POST "$BASE_URL/api/invoice/submit" \
  -H "Content-Type: application/json" \
  -d "{
    \"invoiceNumber\": \"$INVOICE_NUMBER\",
    \"invoiceDate\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
    \"customer\": {
      \"customerName\": \"Test Customer\",
      \"customerPin\": \"P123456789X\"
    },
    \"lineItems\": [{
      \"itemName\": \"Test Product\",
      \"quantity\": 1,
      \"unitPrice\": 1000.00,
      \"taxRate\": 16.0
    }],
    \"paymentInfo\": {
      \"paymentMode\": \"CASH\",
      \"paymentAmount\": 1160.00
    }
  }" | jq '.'

# 3. Get Invoice Status
echo -e "\n=== Invoice Status ==="
curl -s "$BASE_URL/api/invoice/status/$INVOICE_NUMBER" | jq '.'

# 4. List Generated Files
echo -e "\n=== Generated Files ==="
curl -s "$BASE_URL/api/invoice/files" | jq '.'
```

### Testing Different Payment Modes

#### Cash Payment
```bash
curl -X POST "http://localhost:5042/api/invoice/submit" \
  -H "Content-Type: application/json" \
  -d '{
    "invoiceNumber": "INV-CASH-001",
    "invoiceDate": "2024-11-28T10:00:00Z",
    "customer": {"customerName": "Cash Customer"},
    "lineItems": [{
      "itemName": "Item 1",
      "quantity": 1,
      "unitPrice": 1000.00,
      "taxRate": 16.0
    }],
    "paymentInfo": {
      "paymentMode": "CASH",
      "paymentAmount": 1160.00
    }
  }'
```

#### Card Payment
```bash
curl -X POST "http://localhost:5042/api/invoice/submit" \
  -H "Content-Type: application/json" \
  -d '{
    "invoiceNumber": "INV-CARD-001",
    "invoiceDate": "2024-11-28T10:00:00Z",
    "customer": {"customerName": "Card Customer"},
    "lineItems": [{
      "itemName": "Item 1",
      "quantity": 1,
      "unitPrice": 1000.00,
      "taxRate": 16.0
    }],
    "paymentInfo": {
      "paymentMode": "CARD",
      "paymentAmount": 1160.00,
      "paymentReference": "CARD-REF-12345"
    }
  }'
```

### Troubleshooting

#### Check if API is running
```bash
curl -v "http://localhost:5042/"
```

#### View HTTP headers
```bash
curl -i "http://localhost:5042/"
```

#### Save response to file
```bash
curl -s "http://localhost:5042/api/invoice/files" > response.json
```

#### Pretty Print JSON (with jq)
```bash
curl -s "http://localhost:5042/" | jq '.'
```

## Development

### Project Architecture

The project follows a clean, minimal structure:

- **Program.cs** - Application bootstrap, service registration, and dependency injection setup
- **InvoiceEndpoints.cs** - All API endpoint definitions using minimal API pattern
- **KraEtimsService.cs** - Business logic for KRA eTIMS integration, HTTP client, QR code generation, and file management
- **Invoice.cs** - Data models (InvoiceRequest, InvoiceResponse, Customer, LineItems, etc.) and helper methods

### Key Concepts for New Developers

1. **File Generation**: All invoice data is automatically saved to `generated-invoices/` folder:
   - Request JSON (what we send to KRA)
   - Response JSON (what KRA returns)
   - QR Code PNG (generated from KRA response)

2. **Error Handling**: Files are saved even on API errors for debugging purposes. Check the response JSON files to see what KRA returned.

3. **Configuration**: All KRA API endpoints are configurable via `appsettings.json`. Update these after receiving official KRA documentation.

4. **Service Pattern**: `KraEtimsService` handles all KRA API communication. It's registered as a scoped service and injected into endpoints.

### Dev Container

The project includes a Dev Container configuration for consistent development environments. Open in VS Code with the Remote Containers extension to get started immediately.

### Building and Running

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run

# Run with hot reload (watch mode)
dotnet watch run
```

## License

MIT