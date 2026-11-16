using FlowCart.BuildingBlocks.Application;
using FluentAssertions;
using Xunit;

namespace FlowCart.BuildingBlocks.UnitTests;

public class ResultTests
{
    [Fact]
    public void SuccessResult_ShouldHaveIsSuccessTrue_AndNoError()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void FailureResult_ShouldHaveIsFailureTrue_AndContainError()
    {
        // Arrange
        var error = Error.NotFound("Item.NotFound", "Item not found");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ResultWithValue_WhenSuccess_ShouldReturnExpectedValue()
    {
        // Act
        Result<int> result = Result.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ResultWithValue_WhenFailure_AccessingValueShouldThrowInvalidOperationException()
    {
        // Arrange
        var error = Error.Validation("InvalidInput", "The input is invalid");
        Result<string> result = Result.Failure<string>(error);

        // Act
        var act = () => result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Match_ShouldExecuteAppropriateBranch()
    {
        // Arrange
        Result<int> successResult = 100;
        Result<int> failureResult = Error.NotFound("NotFound", "Item not found");

        // Act
        var successOutcome = successResult.Match(v => $"Value: {v}", e => $"Error: {e.Code}");
        var failureOutcome = failureResult.Match(v => $"Value: {v}", e => $"Error: {e.Code}");

        // Assert
        successOutcome.Should().Be("Value: 100");
        failureOutcome.Should().Be("Error: NotFound");
    }
}
