using Hl7.Fhir.Model;
using SmartHealthCard.Test.Model;
using SmartHealthCard.Test.Serializers;
using SmartHealthCard.Test.Support;
using SmartHealthCard.Token;
using SmartHealthCard.Token.Model.Shc;
using SmartHealthCard.QRCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SmartHealthCard.Token.Providers;
using System.Runtime.Versioning;
using SkiaSharp;
using Net.Codecrete.QrCodeGenerator;

namespace SmartHealthCard.Test
{
  public class SmartHealthCardQRCodeEncoderTest
  {    
    [Fact]
    public async void Encode_QRCode()
    {
      // Arrange
      //Get the ECC certificate from the Cert and Private key PEM files
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();

      //The Version of FHIR in use
      string fhirVersion = "4.0.1";

      //Get FHIR bundle
      Bundle fhirBundleResource = FhirDataSupport.GetCovid19NotDetectedFhirBundleExample();
      string fhirBundleJson = FhirSerializer.SerializeToJson(fhirBundleResource);

      //The base of the URL where a validator will retrieve the public keys from (e.g : [Issuer]/.well-known/jwks.json) 
      Uri issuer = new("https://localhost:44306/Smart-health-card");

      //When the Smart Health Card became valid, the from date.
      DateTimeOffset issuanceDateTimeOffset = DateTimeOffset.Now.AddMinutes(-1);

      //The Uri for the type of VerifiableCredentials
      List<VerifiableCredentialType> verifiableCredentialTypeList = new()
      {
        VerifiableCredentialType.HealthCard,
        VerifiableCredentialType.Covid19,
        VerifiableCredentialType.Laboratory
      };

      //Create the SmartHealthCardModel
      SmartHealthCardModel smartHealthCardToEncode = new(issuer, issuanceDateTimeOffset,
          new VerifiableCredential(verifiableCredentialTypeList,
            new CredentialSubject(fhirVersion, fhirBundleJson)));

      //Instantiate the SmartHealthCard Encoder
      SmartHealthCardEncoder smartHealthCardEncoder = new();
      SmartHealthCardQRCodeEncoder smartHealthCardQRCodeFactory = new();

      // Act
      //Get the Smart Health Card JWS Token 
      string smartHealthCardJwsToken = await smartHealthCardEncoder.GetTokenAsync(certificate, smartHealthCardToEncode);

      //Create list of QR Codes
      List<SKBitmap> qrCodeImageList = smartHealthCardQRCodeFactory.GetQRCodeList(smartHealthCardJwsToken);

      // Assert
      Assert.True(!string.IsNullOrWhiteSpace(smartHealthCardJwsToken));
      Assert.NotNull(qrCodeImageList);
      Assert.Single(qrCodeImageList);
    }

    
    [Fact]
    public async void Decode_QRCodeRawData()
    {
      // Arrange
      //Get the ECC certificate from the Cert and Private key PEM files
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();

      //The Version of FHIR in use
      string fhirVersion = "4.0.1";

      //Get FHIR bundle
      Bundle fhirBundleResource = FhirDataSupport.GetCovid19NotDetectedFhirBundleExample();
      string fhirBundleJson = FhirSerializer.SerializeToJson(fhirBundleResource);

      //The base of the URL where a validator will retrieve the public keys from (e.g : [Issuer]/.well-known/jwks.json) 
      Uri issuer = new("https://e1414486fce0.ngrok.io");

      //When the Smart Health Card became valid, the from date.
      DateTimeOffset issuanceDateTimeOffset = DateTimeOffset.Now.AddMinutes(-1);

      //The Uri for the type of VerifiableCredentials
      List<VerifiableCredentialType> verifiableCredentialTypeList = new() 
      { 
        VerifiableCredentialType.HealthCard, 
        VerifiableCredentialType.Covid19 
      };

      //Create the SmartHealthCardModel
      SmartHealthCardModel smartHealthCardModel = new(issuer, issuanceDateTimeOffset,
          new VerifiableCredential(verifiableCredentialTypeList,
            new CredentialSubject(fhirVersion, fhirBundleJson)));

      //Instantiate components
      SmartHealthCardEncoder smartHealthCardEncoder = new();
      SmartHealthCardQRCodeEncoder smartHealthCardQRCodeEncoder = new();
      SmartHealthCardQRCodeDecoder smartHealthCardQRCodeDecoder = new();
      IJwksProvider mockedJwksProvider = JwksSupport.GetMockedIJwksProvider(certificate, issuer);
      SmartHealthCardDecoder smartHealthCardDecoder = new(mockedJwksProvider);

      // Act
      //Get the Smart Health Card JWS Token 
      string smartHealthCardJwsToken = await smartHealthCardEncoder.GetTokenAsync(certificate, smartHealthCardModel);

      //Create list of QR Code raw data
      List<string> qrCodeRawDataList = smartHealthCardQRCodeEncoder.GetQRCodeRawDataList(smartHealthCardJwsToken);

      //Decode QR code back to JWS token
      string jws = smartHealthCardQRCodeDecoder.GetToken(qrCodeRawDataList);

      //Verify the decoded token
      SmartHealthCardModel decodedSmartHealthCardModel = await smartHealthCardDecoder.DecodeAsync(jws, true);

      // Assert
      Assert.True(!string.IsNullOrWhiteSpace(jws));
      Assert.Equal(3, jws.Split('.').Length);
      Assert.NotNull(decodedSmartHealthCardModel);
    }

    [Fact]
    public void QRCodeExtensions_ToBitmap_With_Custom_Colors_Should_Generate_Bitmap()
    {
      // Arrange
      var qrCode = Net.Codecrete.QrCodeGenerator.QrCode.EncodeText("shc:/567629095326546034602", Net.Codecrete.QrCodeGenerator.QrCode.Ecc.Medium);
      int pixelPerModule = 4;
      int border = 2;
      SkiaSharp.SKColor foregroundColor = SkiaSharp.SKColors.Black;
      SkiaSharp.SKColor backgroundColor = SkiaSharp.SKColors.White;

      // Act
      SkiaSharp.SKBitmap bitmap = qrCode.ToBitmap(pixelPerModule, border, foregroundColor, backgroundColor);

      // Assert
      Assert.NotNull(bitmap);
      Assert.True(bitmap.Width > 0);
      Assert.True(bitmap.Height > 0);
      bitmap.Dispose();
    }

    [Fact]
    public void QRCodeExtensions_ToBitmap_With_Default_Colors_Should_Generate_Bitmap()
    {
      // Arrange
      var qrCode = Net.Codecrete.QrCodeGenerator.QrCode.EncodeText("shc:/567629095326546034602", Net.Codecrete.QrCodeGenerator.QrCode.Ecc.Medium);
      int pixelPerModule = 2;
      int border = 1;

      // Act
      SkiaSharp.SKBitmap bitmap = qrCode.ToBitmap(pixelPerModule, border);

      // Assert
      Assert.NotNull(bitmap);
      Assert.True(bitmap.Width > 0);
      Assert.True(bitmap.Height > 0);
      bitmap.Dispose();
    }

  }
}
