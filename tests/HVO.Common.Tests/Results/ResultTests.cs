using System;
using HVO.Core.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Common.Tests.Results;

[TestClass]
public class ResultTests
{
    [TestMethod]
    public void Result_Success_HasValue()
    {
        // Arrange & Act
        var result = Result<int>.Success(42);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.IsFalse(result.IsFailure);
        Assert.AreEqual(42, result.Value);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public void Result_Failure_HasError()
    {
        // Arrange
        var error = new Exception("Test error");

        // Act
        var result = Result<int>.Failure(error);

        // Assert
        Assert.IsFalse(result.IsSuccessful);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(error, result.Error);
    }

    [TestMethod]
    public void Result_ImplicitConversion_FromValue()
    {
        // Arrange & Act
        Result<int> result = 42;

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void Result_ImplicitConversion_FromException()
    {
        // Arrange
        var error = new Exception("Test error");

        // Act
        Result<int> result = error;

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(error, result.Error);
    }

    [TestMethod]
    public void Result_Match_CallsSuccessFunction()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var output = result.Match(
            success: x => $"Value: {x}",
            failure: ex => $"Error: {ex?.Message}");

        // Assert
        Assert.AreEqual("Value: 42", output);
    }

    [TestMethod]
    public void Result_Match_CallsFailureFunction()
    {
        // Arrange
        var error = new Exception("Test error");
        var result = Result<int>.Failure(error);

        // Act
        var output = result.Match(
            success: x => $"Value: {x}",
            failure: ex => $"Error: {ex?.Message}");

        // Assert
        Assert.AreEqual("Error: Test error", output);
    }

    [TestMethod]
    public void Result_Value_ThrowsOnFailure()
    {
        // Arrange
        var result = Result<int>.Failure(new Exception("Test error"));

        // Act & Assert
        Assert.ThrowsExactly<Exception>(() => { var value = result.Value; });
    }
}

[TestClass]
public class ResultEnumTests
{
    private enum ErrorCode
    {
        NotFound,
        Invalid,
        Unauthorized
    }

    [TestMethod]
    public void ResultEnum_Success_HasValue()
    {
        // Arrange & Act
        var result = Result<int, ErrorCode>.Success(42);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.IsFalse(result.IsFailure);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void ResultEnum_Failure_HasErrorCode()
    {
        // Arrange & Act
        var result = Result<int, ErrorCode>.Failure(ErrorCode.NotFound, "Item not found");

        // Assert
        Assert.IsFalse(result.IsSuccessful);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ErrorCode.NotFound, result.Error.Code);
        Assert.AreEqual("Item not found", result.Error.Message);
    }

    [TestMethod]
    public void ResultEnum_ImplicitConversion_FromValue()
    {
        // Arrange & Act
        Result<int, ErrorCode> result = 42;

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void ResultEnum_ImplicitConversion_FromErrorCode()
    {
        // Arrange & Act
        Result<int, ErrorCode> result = ErrorCode.NotFound;

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ErrorCode.NotFound, result.Error.Code);
    }

    [TestMethod]
    public void ResultEnum_Match_CallsCorrectBranch()
    {
        // Arrange
        var success = Result<int, ErrorCode>.Success(42);
        var failure = Result<int, ErrorCode>.Failure(ErrorCode.Invalid);

        // Act
        var successOutput = success.Match(
            success: x => $"Value: {x}",
            failure: err => $"Error: {err.Code}");
        var failureOutput = failure.Match(
            success: x => $"Value: {x}",
            failure: err => $"Error: {err.Code}");

        // Assert
        Assert.AreEqual("Value: 42", successOutput);
        Assert.AreEqual("Error: Invalid", failureOutput);
    }
}

[TestClass]
public class ResultExtensionsTests
{
    [TestMethod]
    public void Map_TransformsSuccessfulValue()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        Assert.IsTrue(mapped.IsSuccessful);
        Assert.AreEqual("42", mapped.Value);
    }

    [TestMethod]
    public void Map_PreservesError()
    {
        // Arrange
        var error = new Exception("Test error");
        var result = Result<int>.Failure(error);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        Assert.IsTrue(mapped.IsFailure);
        Assert.AreEqual(error, mapped.Error);
    }

    [TestMethod]
    public void Bind_FlatMapsSuccessfulValue()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var bound = result.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        Assert.IsTrue(bound.IsSuccessful);
        Assert.AreEqual("42", bound.Value);
    }

    [TestMethod]
    public void Bind_PreservesError()
    {
        // Arrange
        var error = new Exception("Test error");
        var result = Result<int>.Failure(error);

        // Act
        var bound = result.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        Assert.IsTrue(bound.IsFailure);
        Assert.AreEqual(error, bound.Error);
    }

    [TestMethod]
    public void OnSuccess_InvokesActionOnlyOnSuccess()
    {
        var result = Result<int>.Success(10);
        var called = false;

        result.OnSuccess(value =>
        {
            called = true;
            Assert.AreEqual(10, value);
        });

        Assert.IsTrue(called);
    }

    [TestMethod]
    public void OnFailure_InvokesActionOnlyOnFailure()
    {
        var error = new InvalidOperationException("boom");
        var result = Result<int>.Failure(error);
        Exception? observed = null;

        result.OnFailure(ex => observed = ex);

        Assert.AreEqual(error, observed);
    }

    [TestMethod]
    public void WhereSuccess_FiltersSuccessfulResults()
    {
        var results = new[]
        {
            Result<int>.Success(1),
            Result<int>.Failure(new Exception("fail")),
            Result<int>.Success(2)
        };

        var values = results.WhereSuccess();

        CollectionAssert.AreEqual(new[] { 1, 2 }, new System.Collections.Generic.List<int>(values));
    }

    [TestMethod]
    public void WhereFailure_FiltersFailedResults()
    {
        var firstError = new Exception("fail-1");
        var secondError = new Exception("fail-2");
        var results = new[]
        {
            Result<int>.Failure(firstError),
            Result<int>.Success(2),
            Result<int>.Failure(secondError)
        };

        var errors = results.WhereFailure();

        CollectionAssert.AreEqual(new[] { firstError, secondError }, new System.Collections.Generic.List<Exception>(errors));
    }

    [TestMethod]
    public void GetValueOrDefault_ReturnsValueOnSuccess()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var value = result.GetValueOrDefault(0);

        // Assert
        Assert.AreEqual(42, value);
    }

    [TestMethod]
    public void GetValueOrDefault_ReturnsDefaultOnFailure()
    {
        // Arrange
        var result = Result<int>.Failure(new Exception());

        // Act
        var value = result.GetValueOrDefault(0);

        // Assert
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void GetValueOrDefault_Factory_UsesDefaultOnFailure()
    {
        var result = Result<int>.Failure(new Exception());
        var called = false;

        var value = result.GetValueOrDefault(() =>
        {
            called = true;
            return 7;
        });

        Assert.IsTrue(called);
        Assert.AreEqual(7, value);
    }

    [TestMethod]
    public void GetValueOrDefault_Factory_SkipsFactoryOnSuccess()
    {
        var result = Result<int>.Success(42);
        var called = false;

        var value = result.GetValueOrDefault(() =>
        {
            called = true;
            return 7;
        });

        Assert.IsFalse(called);
        Assert.AreEqual(42, value);
    }
}
