#!/bin/bash

# KRA eTIMS Invoice Worker - cURL Examples
# Make sure the API is running: dotnet run

BASE_URL="http://localhost:5042"

echo "=========================================="
echo "KRA eTIMS Invoice Worker - API Testing"
echo "=========================================="
echo ""

# 1. Health Check
echo "1. Health Check"
echo "----------------------------------------"
curl -X GET "$BASE_URL/" \
  -H "Accept: application/json" \
  -w "\nHTTP Status: %{http_code}\n" \
  | jq '.' 2>/dev/null || cat
echo ""
echo ""

# 2. Submit Invoice
echo "2. Submit Invoice to KRA eTIMS"
echo "----------------------------------------"
INVOICE_NUMBER="INV-$(date +%Y%m%d-%H%M%S)"
curl -X POST "$BASE_URL/api/invoice/submit" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d "{
    \"invoiceNumber\": \"$INVOICE_NUMBER\",
    \"invoiceDate\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
    \"customer\": {
      \"customerName\": \"John Doe\",
      \"customerPin\": \"P123456789X\",
      \"customerAddress\": \"123 Main Street, Nairobi\",
      \"customerPhone\": \"+254712345678\",
      \"customerEmail\": \"john.doe@example.com\"
    },
    \"lineItems\": [
      {
        \"itemName\": \"Product A\",
        \"itemCode\": \"PROD-A-001\",
        \"quantity\": 2,
        \"unitPrice\": 1000.00,
        \"taxRate\": 16.0,
        \"discount\": 0
      },
      {
        \"itemName\": \"Product B\",
        \"itemCode\": \"PROD-B-002\",
        \"quantity\": 1,
        \"unitPrice\": 500.00,
        \"taxRate\": 16.0,
        \"discount\": 50.00
      }
    ],
    \"paymentInfo\": {
      \"paymentMode\": \"CASH\",
      \"paymentAmount\": 2900.00,
      \"paymentReference\": \"PAY-REF-001\"
    },
    \"currency\": \"KES\",
    \"remarks\": \"Test invoice submission\"
  }" \
  -w "\nHTTP Status: %{http_code}\n" \
  | jq '.' 2>/dev/null || cat
echo ""
echo ""

# 3. Get Invoice Status (using the invoice number from above)
echo "3. Get Invoice Status"
echo "----------------------------------------"
curl -X GET "$BASE_URL/api/invoice/status/$INVOICE_NUMBER" \
  -H "Accept: application/json" \
  -w "\nHTTP Status: %{http_code}\n" \
  | jq '.' 2>/dev/null || cat
echo ""
echo ""

# 4. List Generated Files
echo "4. List Generated Invoice Files"
echo "----------------------------------------"
curl -X GET "$BASE_URL/api/invoice/files" \
  -H "Accept: application/json" \
  -w "\nHTTP Status: %{http_code}\n" \
  | jq '.' 2>/dev/null || cat
echo ""
echo ""

echo "=========================================="
echo "Testing Complete!"
echo "=========================================="
echo ""
echo "Check the 'generated-invoices/' folder for:"
echo "  - Invoice request JSON files"
echo "  - Invoice response JSON files"
echo "  - QR code PNG images"
echo ""

