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

    
  }
}
