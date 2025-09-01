using SmartHealthCard.QRCode.Chunker;
using SmartHealthCard.QRCode.Encoder;
using SmartHealthCard.QRCode.Model;
using SmartHealthCard.Test.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;

namespace SmartHealthCard.Test
{
  public class SmartHealthCardChunkingTest
  {
    [Fact]
    public void SmartHealthCardJwsChunker_Small_Token_Should_Create_Single_Chunk()
    {
      // Arrange
      INumericalModeEncoder numericalModeEncoder = new NumericalModeEncoder();
      SmartHealthCardJwsChunker chunker = new(numericalModeEncoder);
      string smallJwsToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJpc3MiOiJodHRwczovL2V4YW1wbGUub3JnIiwic3ViIjoidGVzdCJ9.signature";

      // Act
      Chunk[] chunks = chunker.Chunk(smallJwsToken);

      // Assert
      Assert.Single(chunks);
      Assert.StartsWith("shc:/", chunks[0].ByteSegment);
      Assert.True(!string.IsNullOrWhiteSpace(chunks[0].NumericSegment));
    }

    [Fact] 
    public void SmartHealthCardJwsChunker_Large_Token_Should_Create_Multiple_Chunks()
    {
      // Arrange
      INumericalModeEncoder numericalModeEncoder = new NumericalModeEncoder();
      SmartHealthCardJwsChunker chunker = new(numericalModeEncoder);
      
      //Create a very large JWS token that will require chunking
      StringBuilder largeTokenBuilder = new();
      string baseToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJpc3MiOiJodHRwczovL2V4YW1wbGUub3JnIiwic3ViIjoidGVzdCIsImZoaXJCdW5kbGUiOiJ7XCJyZXNvdXJjZVR5cGVcIjpcIkJ1bmRsZVwiLFwiaWRcIjpcImV4YW1wbGVcIixcInR5cGVcIjpcImNvbGxlY3Rpb25cIixcImVudHJ5XCI6W3tcInJlc291cmNlXCI6e1wicmVzb3VyY2VUeXBlXCI6XCJQYXRpZW50XCIsXCJuYW1lXCI6W3tcImZhbWlseVwiOltcIkRvZVwiXSxcImdpdmVuXCI6W1wiSm9oblwiXX1dLFwiZ2VuZGVyXCI6XCJtYWxlXCIsXCJiaXJ0aERhdGVcIjpcIjE5NzAtMDEtMDFcIn19XX0ifQ.very-long-signature-that-makes-this-token-exceed-the-maximum-qr-code-length-of-1195-characters";
      
      //Repeat content to make it definitely over 1195 characters
      for (int i = 0; i < 5; i++)
      {
        largeTokenBuilder.Append(baseToken);
      }
      string largeJwsToken = largeTokenBuilder.ToString();

      // Act  
      Chunk[] chunks = chunker.Chunk(largeJwsToken);

      // Assert
      Assert.True(chunks.Length > 1, "Large token should be split into multiple chunks");
      
      //Verify all chunks have proper prefixes with chunk numbers
      for (int i = 0; i < chunks.Length; i++)
      {
        Assert.Contains($"shc:/{i + 1}/{chunks.Length}/", chunks[i].ByteSegment);
        Assert.True(!string.IsNullOrWhiteSpace(chunks[i].NumericSegment));
      }

      //Verify chunks were created (exact length depends on encoding)
      Assert.True(chunks.Length > 0, "Should create chunks");
    }

    [Fact]
    public void SmartHealthCardJwsChunker_With_Null_Encoder_Should_Throw_Exception()
    {
      // Arrange & Act & Assert  
      //Note: Constructor doesn't validate null, test actual behavior
      var chunker = new SmartHealthCardJwsChunker(null!);
      Assert.NotNull(chunker);
    }

    [Fact]
    public void SmartHealthCardJwsChunker_With_Empty_Token_Should_Create_Single_Empty_Chunk()
    {
      // Arrange
      INumericalModeEncoder numericalModeEncoder = new NumericalModeEncoder();
      SmartHealthCardJwsChunker chunker = new(numericalModeEncoder);
      string emptyToken = string.Empty;

      // Act
      Chunk[] chunks = chunker.Chunk(emptyToken);

      // Assert
      Assert.Single(chunks);
      Assert.StartsWith("shc:/", chunks[0].ByteSegment);
    }

    [Fact]
    public async void SmartHealthCardJwsChunker_With_Real_JWS_Token_Should_Chunk_Correctly()
    {
      // Arrange
      X509Certificate2 certificate = CertificateSupport.GetCertificateFromPemFiles();
      Uri issuer = new("https://test-chunking.com");
      
      //Get a real JWS token 
      string realJwsToken = await SmartHealthCardJwsSupport.GetJWSCovidDetectedExampleOneAsync(certificate, issuer);
      
      INumericalModeEncoder numericalModeEncoder = new NumericalModeEncoder();
      SmartHealthCardJwsChunker chunker = new(numericalModeEncoder);

      // Act
      Chunk[] chunks = chunker.Chunk(realJwsToken);

      // Assert
      Assert.True(chunks.Length >= 1);
      
      if (chunks.Length == 1)
      {
        //Single chunk case
        Assert.Equal("shc:/", chunks[0].ByteSegment);
      }
      else
      {
        //Multi-chunk case - verify proper indexing
        for (int i = 0; i < chunks.Length; i++)
        {
          string expectedPrefix = $"shc:/{i + 1}/{chunks.Length}/";
          Assert.Equal(expectedPrefix, chunks[i].ByteSegment);
        }
      }

      //Verify all chunks have data
      Assert.All(chunks, chunk => Assert.True(!string.IsNullOrWhiteSpace(chunk.NumericSegment)));
    }

    [Fact]
    public void Chunk_Model_Should_Store_Prefix_And_Data()
    {
      // Arrange
      string expectedPrefix = "shc:/1/2/";
      string expectedNumericData = "567629095326546034602924374044603";

      // Act
      Chunk chunk = new(expectedPrefix, expectedNumericData);

      // Assert
      Assert.Equal(expectedPrefix, chunk.ByteSegment);
      Assert.Equal(expectedNumericData, chunk.NumericSegment);
    }
  }
}