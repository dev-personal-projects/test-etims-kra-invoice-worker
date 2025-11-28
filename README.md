# KRA eTIMS Invoice Worker

A simple, clean .NET 9 Web API for integrating with Kenya Revenue Authority (KRA) eTIMS (Electronic Tax Invoice Management System). This application provides a minimal implementation for generating tax-compliant invoices with KRA eTIMS integration.

## Features

- **OSCU Integration** - Online Sales Control Unit for real-time invoice submission to KRA
- **Minimal API** - Clean, simple structure with minimal files
- **Tax-Compliant Invoices** - Generate invoices with KRA-assigned invoice numbers, QR codes, and digital signatures
- **Dev Container Support** - Ready-to-use development environment with .NET 9 SDK

## Project Structure

```
test-etims-kra-invoice-worker/
├── Program.cs              # Main application entry point and API endpoints
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

Update `appsettings.json` with your KRA eTIMS credentials:

```json
{
  "KraEtims": {
    "BaseUrl": "https://etims-sbx.kra.go.ke/api",
    "ApiUsername": "your-username",
    "ApiPassword": "your-password",
    "Pin": "your-kra-pin"
  }
}
```

### Running the Application

```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5042`
- HTTPS: `https://localhost:7029`

### API Endpoints

- `GET /` - Health check endpoint
- `GET /openapi/v1.json` - OpenAPI specification (Development only)

## KRA eTIMS Integration

This application uses **OSCU (Online Sales Control Unit)** for integration, which means:
- Direct API calls to KRA's cloud service
- Real-time invoice validation and signing
- Requires internet connectivity for invoice submission

For offline capability, consider implementing **VSCU (Virtual Sales Control Unit)** which requires running KRA's Java-based control unit locally.

## Development

The project includes a Dev Container configuration for consistent development environments. Open in VS Code with the Remote Containers extension to get started immediately.

## License

MIT