using FluentAssertions;
using ProjectManager.Application.Common;
using Xunit;

namespace ProjectManager.Application.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_HasSuccessStatus()
    {
        var r = Result.Success();
        r.IsSuccess.Should().BeTrue();
        r.Status.Should().Be(ResultStatus.Success);
    }

    [Fact]
    public void NotFound_CarriesMessageAndIsNotSuccess()
    {
        var r = Result.NotFound("missing");
        r.IsSuccess.Should().BeFalse();
        r.Status.Should().Be(ResultStatus.NotFound);
        r.Message.Should().Be("missing");
    }

    [Fact]
    public void Invalid_CarriesValidationErrors()
    {
        var errors = new Dictionary<string, string[]> { ["Name"] = new[] { "Required" } };
        var r = Result.Invalid(errors);
        r.Status.Should().Be(ResultStatus.Invalid);
        r.ValidationErrors.Should().ContainKey("Name");
    }

    [Fact]
    public void GenericSuccess_CarriesValue()
    {
        var r = Result<int>.Success(42);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(42);
    }

    [Fact]
    public void GenericNotFound_HasNoValue()
    {
        var r = Result<int>.NotFound("x");
        r.IsSuccess.Should().BeFalse();
        r.Status.Should().Be(ResultStatus.NotFound);
    }
}
