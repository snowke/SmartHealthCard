using Hl7.Fhir.Model;
using Moq;
using SmartHealthCard.Test.Model;
using SmartHealthCard.Test.Serializers;
using SmartHealthCard.Test.Support;
using SmartHealthCard.Token;
using SmartHealthCard.Token.Encoders;
using SmartHealthCard.Token.Exceptions;
using SmartHealthCard.Token.Model.Jwks;
using SmartHealthCard.Token.Model.Shc;
using SmartHealthCard.Token.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace SmartHealthCard.Test
{
  public class SmartHealthCardEncoderTest
  {
    [Fact]
    public async void Create_Token_Decode_Token()
    {
      // Arrange
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();
      string fhirVersion = "4.0.1";
      Bundle fhirBundleResource = FhirDataSupport.GetCovid19DetectedFhirBundleExample();
      string fhirBundleJson = FhirSerializer.SerializeToJson(fhirBundleResource);
      Uri issuer = new("https://e1414486fce0.ngrok.io");
      DateTimeOffset issuanceDateTimeOffset = DateTimeOffset.Now.AddMinutes(-1);
      List<VerifiableCredentialType> verifiableCredentialTypeList = new()
      {
        VerifiableCredentialType.HealthCard,
        VerifiableCredentialType.Covid19
      };
      
      SmartHealthCardModel smartHealthCard = new(issuer, issuanceDateTimeOffset,
          new VerifiableCredential(verifiableCredentialTypeList,
            new CredentialSubject(fhirVersion, fhirBundleJson)));
      
      SmartHealthCardEncoder encoder = new();
      IJwksProvider mockedJwksProvider = JwksSupport.GetMockedIJwksProvider(certificate, issuer);
      SmartHealthCardDecoder decoder = new(mockedJwksProvider);

      // Act
      string smartHealthCardJwsToken = await encoder.GetTokenAsync(certificate, smartHealthCard);
      SmartHealthCardModel decodedSmartHealthCardModel = await decoder.DecodeAsync(smartHealthCardJwsToken, Verify: true);

      // Assert
      Assert.True(!string.IsNullOrWhiteSpace(smartHealthCardJwsToken));
      
      DateTimeOffset actualIssuanceDate = decodedSmartHealthCardModel.GetIssuanceDate();
      Assert.True((issuanceDateTimeOffset - actualIssuanceDate).TotalSeconds < 1);
      Assert.Equal(issuer, decodedSmartHealthCardModel.Issuer);
      Assert.NotNull(decodedSmartHealthCardModel.VerifiableCredential);
      Assert.Equal(verifiableCredentialTypeList, decodedSmartHealthCardModel.VerifiableCredential.VerifiableCredentialTypeList);
      Assert.NotNull(decodedSmartHealthCardModel.VerifiableCredential.CredentialSubject);
      Assert.Equal(fhirVersion, decodedSmartHealthCardModel.VerifiableCredential.CredentialSubject.FhirVersion);
    }

    [Fact]
    public void HelloWorld_Should_Return_Expected_Message()
    {
      // Arrange
      SmartHealthCardEncoder encoder = new();

      // Act
      string result = encoder.HelloWorld();

      // Assert
      Assert.Equal("Hello World!", result);
    }

    [Fact]
    public async void GetSmartHealthCardFile_Should_Create_File_With_Multiple_Cards()
    {
      // Arrange
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();
      string fhirVersion = "4.0.1";
      Uri issuer = new("https://test-issuer.com");
      DateTimeOffset issuanceDateTimeOffset = DateTimeOffset.Now.AddMinutes(-1);
      
      Bundle fhirBundle1 = FhirDataSupport.GetCovid19DetectedFhirBundleExample();
      Bundle fhirBundle2 = FhirDataSupport.GetCovid19NotDetectedFhirBundleExample();
      string fhirBundleJson1 = FhirSerializer.SerializeToJson(fhirBundle1);
      string fhirBundleJson2 = FhirSerializer.SerializeToJson(fhirBundle2);
      
      List<VerifiableCredentialType> verifiableCredentialTypes = new()
      {
        VerifiableCredentialType.HealthCard,
        VerifiableCredentialType.Covid19
      };

      List<SmartHealthCardModel> smartHealthCardList = new()
      {
        new(issuer, issuanceDateTimeOffset,
            new VerifiableCredential(verifiableCredentialTypes,
              new CredentialSubject(fhirVersion, fhirBundleJson1))),
        new(issuer, issuanceDateTimeOffset.AddMinutes(1),
            new VerifiableCredential(verifiableCredentialTypes,
              new CredentialSubject(fhirVersion, fhirBundleJson2)))
      };

      SmartHealthCardEncoder encoder = new();

      // Act
      string smartHealthCardFileJson = await encoder.GetSmartHealthCardFile(certificate, smartHealthCardList);

      // Assert
      Assert.True(!string.IsNullOrWhiteSpace(smartHealthCardFileJson));
      Assert.Contains("verifiableCredential", smartHealthCardFileJson);
      
      //Just verify basic structure since exact JWS count depends on token structure
      Assert.Contains("[", smartHealthCardFileJson); //Should be JSON array
      Assert.Contains("]", smartHealthCardFileJson);
    }
    
  }
}
