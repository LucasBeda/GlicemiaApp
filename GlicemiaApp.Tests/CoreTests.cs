using GlicemiaApp.Core;
using Xunit;

public class InputValidatorTests
{
    [Theory]
    [InlineData("98")]
    [InlineData("20")]
    [InlineData("600")]
    [InlineData(" 120 ")]
    public void Accepts_valid_values(string text) =>
        Assert.NotNull(InputValidator.Validate(text, InputValidator.Contexts[0], "").Input);

    [Theory]
    [InlineData("19")]
    [InlineData("601")]
    [InlineData("-90")]
    [InlineData("+90")]
    [InlineData("9.5")]
    [InlineData("0x40")]
    [InlineData("1e2")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("99999999999999999999")]
    public void Rejects_invalid_values(string? text) =>
        Assert.NotNull(InputValidator.Validate(text, InputValidator.Contexts[0], "").Error);

    [Fact]
    public void Rejects_unknown_context() =>
        Assert.NotNull(InputValidator.Validate("100", "'; DROP TABLE x;--", "").Error);

    [Fact]
    public void Note_strips_control_chars_and_limits_length()
    {
        Assert.Equal("ok", InputValidator.Validate("100", InputValidator.Contexts[0], "o\u0000k\n").Input!.Note);
        Assert.NotNull(InputValidator.Validate("100", InputValidator.Contexts[0], new string('a', 141)).Error);
    }
}

public class PinHasherTests
{
    [Fact]
    public void Hash_verifies_only_the_right_pin()
    {
        var stored = PinHasher.Hash("482913");
        Assert.True(PinHasher.Verify("482913", stored));
        Assert.False(PinHasher.Verify("482914", stored));
    }

    [Fact]
    public void Hash_is_salted() => Assert.NotEqual(PinHasher.Hash("482913"), PinHasher.Hash("482913"));

    [Fact]
    public void Verify_survives_garbage_in_storage()
    {
        Assert.False(PinHasher.Verify("482913", "lixo"));
        Assert.False(PinHasher.Verify("482913", "!!!:???"));
    }

    [Theory]
    [InlineData("12345", "12345")]
    [InlineData("1234567", "1234567")]
    [InlineData("12a456", "12a456")]
    [InlineData("111111", "111111")]
    [InlineData("123456", "123456")]
    [InlineData("654321", "654321")]
    [InlineData("482913", "482910")]
    public void Rejects_bad_new_pins(string pin, string confirm) =>
        Assert.NotNull(PinHasher.ValidateNew(pin, confirm));

    [Fact]
    public void Accepts_good_new_pin() => Assert.Null(PinHasher.ValidateNew("482913", "482913"));
}
