using Hl7.Fhir.Model;
using SmartHealthCard.Test.Model;
using SmartHealthCard.Test.Serializers;
using SmartHealthCard.Test.Support;
using SmartHealthCard.Token;
using SmartHealthCard.Token.Exceptions;
using SmartHealthCard.Token.Model.Shc;
using SmartHealthCard.Token.Providers;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace SmartHealthCard.Test
{
  public class SmartHealthCardDecoderTest
  {
    [Fact]
    public async void Decode_Token_Verify_with_JWKS()
    {
      // Arrange
      //Get the ECC certificate from the Cert and Private key PEM files
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();

      //The base of the URL where a validator will retrieve the public keys from (e.g : [Issuer]/.well-known/jwks.json) 
      Uri issuer = new("https://sonichealthcare.com/something");
      string smartHealthCardJwsToken = await SmartHealthCardJwsSupport.GetJWSCovidDetectedExampleOneAsync(certificate, issuer);
      
      //This testing JwksSupport class provides us with a mocked IJwksProvider that will inject the JWKS file
      //rather than make the HTTP call to go get it from a public endpoint.
      IJwksProvider mockedJwksProvider = JwksSupport.GetMockedIJwksProvider(certificate, issuer);

      //Instantiate the SmartHealthCard Decoder
      SmartHealthCardDecoder decoder = new(mockedJwksProvider);

      // Act
      SmartHealthCardModel smartHealthCardModel = await decoder.DecodeAsync(smartHealthCardJwsToken, Verify: true);

      // Assert
      Assert.True(!string.IsNullOrWhiteSpace(smartHealthCardJwsToken));
      Assert.NotNull(smartHealthCardModel);
    }

    /// <summary>
    /// Test that an invalid IssuanceDate throws and exception with the correct message.
    /// That is an IssuanceDate that is in the future by more than 2 mins. This test case sets it to 3 min into the future.
    /// </summary>
    [Fact]
    public async void Decode_Token_Verify_IsTrue_IssuanceDate_Is_Invalid()
    {
      // Arrange
      //Get the ECC certificate from the Cert and Private key PEM files
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();

      //The base of the URL where a validator will retrieve the public keys from (e.g : [Issuer]/.well-known/jwks.json) 
      Uri issuer = new("https://sonichealthcare.com/something");

      //Gets a JWS token with an IssuanceDate that is 3 min into the future from now
      string smartHealthCardJwsToken = await SmartHealthCardJwsSupport.GetJWSCovidIssuanceDateInvalidExampleAsync(certificate, issuer);

      //This testing JwksSupport class provides us with a mocked IJwksProvider that will inject the JWKS file
      //rather than make the HTTP call to go get it from a public endpoint.
      IJwksProvider mockedJwksProvider = JwksSupport.GetMockedIJwksProvider(certificate, issuer);

      //Instantiate the SmartHealthCard Decoder
      SmartHealthCardDecoder decoder = new(mockedJwksProvider);
      
      System.Threading.Tasks.Task Act() => decoder.DecodeAsync(smartHealthCardJwsToken, Verify: true);

      // Act & Assert
      var exception = await Assert.ThrowsAsync<SmartHealthCardDecoderException>(Act);
      
      // Assert
      Assert.StartsWith("The token's Issuance Date (nbf) timestamp is earlier than the current date and time. The token is not valid untill:", exception.Message);
    }

    [Fact]
    public void Decode_Token_Verify_IsTrue_IssuanceDate_Is_Before_minimum_Allowed_Date()
    {
      // Arrange
      Uri issuer = new("https://sonichealthcare.com/something");

      //The Version of FHIR in use
      string fhirVersion = "4.0.1";

      //Get FHIR bundle
      Bundle fhirBundleResource = FhirDataSupport.GetCovid19NotDetectedFhirBundleExample();
      string fhirBundleJson = FhirSerializer.SerializeToJson(fhirBundleResource);

      //IssuanceDate can not be Before : 00:00:00 UTC on 1 January 1970
      DateTimeOffset issuanceDateTimeOffset = new(1970, 01, 01, 00, 00, 00, new TimeSpan(0, 0, 0));
      issuanceDateTimeOffset = issuanceDateTimeOffset.AddSeconds(-1);

      //The Uri for the type of VerifiableCredentials
      List<VerifiableCredentialType> verifiableCredentialTypeList = new()
      {
        VerifiableCredentialType.VerifiableCredential,
        VerifiableCredentialType.HealthCard,
        VerifiableCredentialType.Covid19
      };

      // Act & Assert
      //Create the SmartHealthCardModel     
      var ex = Assert.Throws<SmartHealthCardPayloadException>(() => new SmartHealthCardModel(issuer, issuanceDateTimeOffset, 
                                                                                             new VerifiableCredential(verifiableCredentialTypeList, 
                                                                                             new CredentialSubject(fhirVersion, fhirBundleJson))));
      
      // Assert
      Assert.Equal("The provided IssuanceDate is a date and time before the minimum allowed date and time of: 00:00:00 UTC on 1 January 1970", ex.Message);
    }

    [Fact]
    public async void Decode_Token_Verify_with_Certificate()
    {
      // Arrange
      //Get the ECC certificate from the Cert and Private key PEM files
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();

      //The base of the URL where a validator will retrieve the public keys from (e.g : [Issuer]/.well-known/jwks.json) 
      Uri issuer = new("https://sonichealthcare.com/something");
      string smartHealthCardJwsToken = await SmartHealthCardJwsSupport.GetJWSCovidDetectedExampleOneAsync(certificate, issuer);

      //This testing JwksSupport class provides us with a mocked IJwksProvider that will inject the JWKS file
      //rather than make the HTTP call to go get it from a public endpoint.
      IJwksProvider mockedJwksProvider = JwksSupport.GetMockedIJwksProvider(certificate, issuer);

      //Instantiate the SmartHealthCard Decoder
      SmartHealthCardDecoder decoder = new(mockedJwksProvider);

      // Act
      //Verify and Decode
      SmartHealthCardModel smartHealthCardModel = await decoder.DecodeAsync(smartHealthCardJwsToken, Verify: true);

      // Assert
      Assert.True(!string.IsNullOrWhiteSpace(smartHealthCardJwsToken));
      Assert.NotNull(smartHealthCardModel);
    }

    [Fact]
    public async void Decode_Token_Verify_InvalidTokenSignature_Certificate()
    {
      // Arrange
      //Get the ECC certificate from the Cert and Private key PEM files
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();

      //The base of the URL where a validator will retrieve the public keys from (e.g : [Issuer]/.well-known/jwks.json) 
      Uri issuer = new("https://sonichealthcare.com/something");

      string smartHealthCardJwsTokenCovidDetected = await SmartHealthCardJwsSupport.GetJWSCovidDetectedExampleOneAsync(certificate, issuer);
      string smartHealthCardJwsTokenCovidNotDetected = await SmartHealthCardJwsSupport.GetJWSCovidNotDetectedExampleOneAsync(certificate, issuer);

      //Here we have taken the Payload of a NotDetected Covid test result and substituted it into
      //a token build with a Detected Covid Test result, so the signature is now be invalid for it's payload.
      string[] jwsSplitCovidNotDetected = smartHealthCardJwsTokenCovidNotDetected.Split('.');
      string[] jwsSplitCovidDetected = smartHealthCardJwsTokenCovidDetected.Split('.');
      string fraudulentSmartHealthCardJwsToken = string.Join('.', jwsSplitCovidDetected[0], jwsSplitCovidNotDetected[1], jwsSplitCovidDetected[2]);

      //This testing JwksSupport class provides us with a mocked IJwksProvider that will inject the JWKS file
      //rather than make the HTTP call to go get it from a public endpoint.
      IJwksProvider mockedJwksProvider = JwksSupport.GetMockedIJwksProvider(certificate, issuer);

      //Instantiate the SmartHealthCard Decoder
      SmartHealthCardDecoder decoder = new(mockedJwksProvider);

      // Act & Assert
      //Verify and Decode
      SmartHealthCardSignatureInvalidException exception = await Assert.ThrowsAsync<SmartHealthCardSignatureInvalidException>(() => decoder.DecodeAsync(fraudulentSmartHealthCardJwsToken, Verify: true));

      // Assert
      Assert.Equal("The JWS signing signature is invalid.", exception.Message);
    }

    [Fact]
    public async void Decode_Token_Verify_JWKS_Is_Inaccessible()
    {
      // Arrange
      //Get the ECC certificate from the Cert and Private key PEM files
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();

      //The base of the URL where a validator will retrieve the public keys from (e.g : [Issuer]/.well-known/jwks.json) 
      Uri issuer = new("https://sonichealthcare.com/something");

      string smartHealthCardJwsTokenCovidDetected = await SmartHealthCardJwsSupport.GetJWSCovidDetectedExampleOneAsync(certificate, issuer);
      string smartHealthCardJwsTokenCovidNotDetected = await SmartHealthCardJwsSupport.GetJWSCovidNotDetectedExampleOneAsync(certificate, issuer);

      //Here we have taken the Payload of a NotDetected Covid test result and substituted it into
      //a token build with a Detected Covid Test result, so the signature is now be invalid for it's payload.
      string[] jwsSplitCovidNotDetected = smartHealthCardJwsTokenCovidNotDetected.Split('.');
      string[] jwsSplitCovidDetected = smartHealthCardJwsTokenCovidDetected.Split('.');
      string fraudulentSmartHealthCardJwsToken = string.Join('.', jwsSplitCovidDetected[0], jwsSplitCovidNotDetected[1], jwsSplitCovidDetected[2]);

      //This testing JwksSupport class provides us with a mocked IJwksProvider that will inject the JWKS file
      //rather than make the HTTP call to go get it from a public endpoint.
      IJwksProvider mockedJwksProvider = JwksSupport.GetMockedIJwksProvider(certificate, issuer);

      //Instantiate the SmartHealthCard Decoder
      SmartHealthCardDecoder decoder = new(mockedJwksProvider);

      // Act & Assert
      //Verify and Decode
      SmartHealthCardSignatureInvalidException exception = await Assert.ThrowsAsync<SmartHealthCardSignatureInvalidException>(() => decoder.DecodeAsync(fraudulentSmartHealthCardJwsToken, Verify: true));

      // Assert
      Assert.Equal("The JWS signing signature is invalid.", exception.Message);
    }

    [Fact]
    public async void DecodeToJsonAsync_Should_Return_Json_String()
    {
      // Arrange
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();
      Uri issuer = new("https://sonichealthcare.com/something");
      string smartHealthCardJwsToken = await SmartHealthCardJwsSupport.GetJWSCovidDetectedExampleOneAsync(certificate, issuer);
      IJwksProvider mockedJwksProvider = JwksSupport.GetMockedIJwksProvider(certificate, issuer);
      SmartHealthCardDecoder decoder = new(mockedJwksProvider);

      // Act
      string jsonResult = await decoder.DecodeToJsonAsync(smartHealthCardJwsToken, Verify: true);

      // Assert
      Assert.True(!string.IsNullOrWhiteSpace(jsonResult));
      Assert.Contains("iss", jsonResult); //Should contain issuer field
      Assert.Contains("vc", jsonResult);  //Should contain verifiable credential field
    }

    [Fact]
    public async void DecodeToJsonAsync_Without_Verification_Should_Return_Json()
    {
      // Arrange  
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();
      Uri issuer = new("https://sonichealthcare.com/something");
      string smartHealthCardJwsToken = await SmartHealthCardJwsSupport.GetJWSCovidDetectedExampleOneAsync(certificate, issuer);
      SmartHealthCardDecoder decoder = new(); //No JWKS provider needed for unverified decode

      // Act
      string jsonResult = await decoder.DecodeToJsonAsync(smartHealthCardJwsToken, Verify: false);

      // Assert
      Assert.True(!string.IsNullOrWhiteSpace(jsonResult));
      Assert.Contains("iss", jsonResult);
      Assert.Contains("vc", jsonResult);
    }

  }
}
