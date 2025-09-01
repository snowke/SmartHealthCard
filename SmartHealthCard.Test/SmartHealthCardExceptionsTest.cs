using SmartHealthCard.QRCode.Exceptions;
using SmartHealthCard.Token.Exceptions;
using System;
using Xunit;

namespace SmartHealthCard.Test
{
  public class SmartHealthCardExceptionsTest
  {
    [Fact]
    public void SmartHealthCardEncoderException_Should_Set_Message()
    {
      // Arrange
      string expectedMessage = "Test encoder exception message";

      // Act
      SmartHealthCardEncoderException exception = new(expectedMessage);

      // Assert
      Assert.Equal(expectedMessage, exception.Message);
      Assert.IsAssignableFrom<SmartHealthCardException>(exception);
    }

    [Fact]
    public void SmartHealthCardDecoderException_Should_Set_Message()
    {
      // Arrange
      string expectedMessage = "Test decoder exception message";

      // Act
      SmartHealthCardDecoderException exception = new(expectedMessage);

      // Assert
      Assert.Equal(expectedMessage, exception.Message);
      Assert.IsAssignableFrom<SmartHealthCardException>(exception);
    }

    [Fact]
    public void SmartHealthCardJwksException_Should_Set_Message()
    {
      // Arrange
      string expectedMessage = "Test JWKS exception message";

      // Act
      SmartHealthCardJwksException exception = new(expectedMessage);

      // Assert
      Assert.Equal(expectedMessage, exception.Message);
      Assert.IsAssignableFrom<SmartHealthCardException>(exception);
    }

    [Fact]
    public void SmartHealthCardJwksRequestException_Should_Set_Message()
    {
      // Arrange
      string expectedMessage = "Test JWKS request exception message";

      // Act
      SmartHealthCardJwksRequestException exception = new(expectedMessage);

      // Assert
      Assert.Equal(expectedMessage, exception.Message);
      Assert.IsAssignableFrom<SmartHealthCardException>(exception);
    }

    [Fact]
    public void SmartHealthCardSignatureInvalidException_Should_Set_Message()
    {
      // Arrange
      string expectedMessage = "Test signature invalid exception message";

      // Act
      SmartHealthCardSignatureInvalidException exception = new(expectedMessage);

      // Assert
      Assert.Equal(expectedMessage, exception.Message);
      Assert.IsAssignableFrom<SmartHealthCardException>(exception);
    }

    [Fact]
    public void SmartHealthCardPayloadException_Should_Set_Message()
    {
      // Arrange
      string expectedMessage = "Test payload exception message";

      // Act
      SmartHealthCardPayloadException exception = new(expectedMessage);

      // Assert
      Assert.Equal(expectedMessage, exception.Message);
      Assert.IsAssignableFrom<SmartHealthCardException>(exception);
    }

    [Fact]
    public void QRCodeChunkFormatException_Should_Set_Message()
    {
      // Arrange
      string expectedMessage = "Test QR code chunk format exception message";

      // Act
      QRCodeChunkFormatException exception = new(expectedMessage);

      // Assert
      Assert.Equal(expectedMessage, exception.Message);
      Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void InvalidTokenPartsException_Should_Set_Message()
    {
      // Arrange
      string expectedMessage = "Test invalid token parts exception message";

      // Act
      InvalidTokenPartsException exception = new(expectedMessage);

      // Assert
      Assert.Equal(expectedMessage, exception.Message);
      Assert.IsAssignableFrom<Exception>(exception);
    }
  }
}