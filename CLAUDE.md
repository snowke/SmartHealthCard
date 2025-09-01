# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 9 C# library for creating, validating, and processing SMART Health Card JWS tokens and their QR codes. The solution implements the SMART Health Cards Framework specifications for health data verification using FHIR resources, digital signatures, and QR codes.

## Build and Test Commands

```bash
# Build the entire solution
dotnet build SmartHealthCard.sln

# Build specific projects
dotnet build SmartHealthCard.Token/SmartHealthCard.Token.csproj
dotnet build SmartHealthCard.QRCode/SmartHealthCard.QRCode.csproj

# Run tests
dotnet test SmartHealthCard.Test/SmartHealthCard.Test.csproj

# Build and run demo applications
dotnet run --project SmartHealthCard.EncoderDemo/SmartHealthCard.EncoderDemo.csproj
dotnet run --project SmartHealthCard.DecoderDemo/SmartHealthCard.DecoderDemo.csproj

# Run the JWKS API
dotnet run --project SmartHealthCard.JwksApi/SmartHealthCard.JwksApi.csproj

# Create NuGet packages
dotnet pack SmartHealthCard.Token/SmartHealthCard.Token.csproj
dotnet pack SmartHealthCard.QRCode/SmartHealthCard.QRCode.csproj
```

## Project Architecture

### Core Projects

- **SmartHealthCard.Token**: Core library for JWS token encoding, decoding, and validation
- **SmartHealthCard.QRCode**: QR code generation and parsing from/to JWS tokens  
- **SmartHealthCard.Test**: xUnit test suite using Moq and HL7 FHIR libraries
- **SmartHealthCard.JwksApi**: ASP.NET Core API for serving JWKS endpoints
- **SmartHealthCard.EncoderDemo** / **SmartHealthCard.DecoderDemo**: Console demo applications

### Key Components

#### SmartHealthCard.Token Architecture
- **Encoders/Decoders**: `SmartHealthCardEncoder` and `SmartHealthCardDecoder` are the main entry points
- **JWS Token Processing**: Located in `JwsToken/` folder, handles JWT signing and validation
- **Certificate Support**: `X509CertificateSupport` for loading certificates from Windows Certificate Store
- **JWKS Provider**: Retrieves public keys from issuer endpoints or custom implementations via `IJwksProvider`
- **Model Objects**: 
  - `SmartHealthCardModel` - Core data model
  - `VerifiableCredential` - Credential wrapper with types (HealthCard, Covid19, etc.)
  - `CredentialSubject` - Contains FHIR version and bundle JSON
- **Algorithms**: ES256 (ECDSA with P-256 curve) implementation
- **Validation**: Header, payload, and signature validators with comprehensive exception types

#### SmartHealthCard.QRCode Architecture
- Uses SkiaSharp for cross-platform image generation (replacing System.Drawing.Common from .NET 6+)
- Handles multi-QR code scenarios when payloads exceed single QR code capacity
- Returns `SKBitmap` objects that can be converted to various image formats

### Certificate Requirements
- Uses Elliptic Curve Cryptography (ECC) with P-256 curve
- Certificates loaded from Windows Certificate Store by thumbprint
- Private keys required for encoding, public keys for validation
- JWKS (JSON Web Key Set) format for public key distribution

### FHIR Integration
- Expects FHIR R4 bundles as JSON strings
- Does not validate FHIR compliance - recommend using Firely .NET SDK
- Supports health card types: HealthCard, Covid19, Immunization, Laboratory

### Development Notes
- Target framework: .NET 9
- Uses Newtonsoft.Json for serialization
- Comprehensive exception handling with specific exception types
- Support for custom JWKS providers for development without public endpoints
- Azure Pipelines CI/CD configured for Windows builds and testing