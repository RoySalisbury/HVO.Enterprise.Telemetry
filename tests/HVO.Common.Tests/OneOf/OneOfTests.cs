using System;
using HVO.Core.OneOf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Common.Tests.OneOf;

[TestClass]
public class OneOfTests
{
    [TestMethod]
    public void OneOf2_ImplicitConversion_FromT1()
    {
        // Arrange & Act
        OneOf<int, string> oneOf = 42;

        // Assert
        Assert.IsTrue(oneOf.IsT1);
        Assert.IsFalse(oneOf.IsT2);
        Assert.AreEqual(42, oneOf.AsT1);
    }

    [TestMethod]
    public void OneOf2_ImplicitConversion_FromT2()
    {
        // Arrange & Act
        OneOf<int, string> oneOf = "test";

        // Assert
        Assert.IsFalse(oneOf.IsT1);
        Assert.IsTrue(oneOf.IsT2);
        Assert.AreEqual("test", oneOf.AsT2);
    }

    [TestMethod]
    public void OneOf2_Match_CallsCorrectHandler()
    {
        // Arrange
        OneOf<int, string> intValue = 42;
        OneOf<int, string> stringValue = "test";

        // Act
        var intResult = intValue.Match(
            i => $"Int: {i}",
            s => $"String: {s}");
        var stringResult = stringValue.Match(
            i => $"Int: {i}",
            s => $"String: {s}");

        // Assert
        Assert.AreEqual("Int: 42", intResult);
        Assert.AreEqual("String: test", stringResult);
    }

    [TestMethod]
    public void OneOf2_Switch_InvokesCorrectBranch()
    {
        OneOf<int, string> value = "value";
        var called = string.Empty;

        value.Switch(
            _ => called = "int",
            _ => called = "string");

        Assert.AreEqual("string", called);
    }

    [TestMethod]
    public void OneOf2_ValueAndValueType_ReturnExpected()
    {
        OneOf<int, string> intValue = 10;
        OneOf<int, string> stringValue = "value";

        Assert.AreEqual(10, intValue.Value);
        Assert.AreEqual(typeof(int), intValue.ValueType);
        Assert.AreEqual("value", stringValue.Value);
        Assert.AreEqual(typeof(string), stringValue.ValueType);
    }

    [TestMethod]
    public void OneOf2_AsT2_ThrowsWhenT1()
    {
        // Arrange
        OneOf<int, string> oneOf = 42;

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => { var value = oneOf.AsT2; });
    }

    [TestMethod]
    public void OneOf3_SupportsThreeTypes()
    {
        // Arrange & Act
        OneOf<int, string, bool> intValue = 42;
        OneOf<int, string, bool> stringValue = "test";
        OneOf<int, string, bool> boolValue = true;

        // Assert
        Assert.IsTrue(intValue.IsT1);
        Assert.IsTrue(stringValue.IsT2);
        Assert.IsTrue(boolValue.IsT3);
    }

    [TestMethod]
    public void OneOf3_Switch_InvokesCorrectBranch()
    {
        OneOf<int, string, bool> value = true;
        var called = string.Empty;

        value.Switch(
            _ => called = "int",
            _ => called = "string",
            _ => called = "bool");

        Assert.AreEqual("bool", called);
    }

    [TestMethod]
    public void OneOf3_AsT2_ThrowsWhenWrongType()
    {
        OneOf<int, string, bool> value = 99;

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var result = value.AsT2;
        });
    }

    [TestMethod]
    public void OneOf4_SupportsFourTypes()
    {
        // Arrange & Act
        OneOf<int, string, bool, double> intValue = 42;
        OneOf<int, string, bool, double> doubleValue = 3.14;

        // Assert
        Assert.IsTrue(intValue.IsT1);
        Assert.IsTrue(doubleValue.IsT4);
    }

    [TestMethod]
    public void OneOf4_Match_ReturnsExpected()
    {
        OneOf<int, string, bool, double> value = 3.14;

        var result = value.Match(
            _ => "int",
            _ => "string",
            _ => "bool",
            _ => "double");

        Assert.AreEqual("double", result);
    }

    [TestMethod]
    public void OneOf4_AsT3_ThrowsWhenWrongType()
    {
        OneOf<int, string, bool, double> value = "value";

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var result = value.AsT3;
        });
    }

    [TestMethod]
    public void OneOfExtensions_Is_ThrowsWhenNull()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            IOneOf? value = null;
            _ = OneOfExtensions.Is<int>(value!);
        });
    }

    [TestMethod]
    public void OneOfExtensions_Is_DetectsType()
    {
        // Arrange
        OneOf<int, string> oneOf = 42;

        // Act & Assert
        Assert.IsTrue(oneOf.Is<int>());
        Assert.IsFalse(oneOf.Is<string>());
    }

    [TestMethod]
    public void OneOfExtensions_TryGet_GetsValueWhenCorrectType()
    {
        // Arrange
        OneOf<int, string> oneOf = 42;

        // Act
        var success = oneOf.TryGet<int>(out var value);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(42, value);
    }

    [TestMethod]
    public void OneOfExtensions_TryGet_ReturnsFalseWhenWrongType()
    {
        OneOf<int, string> oneOf = 42;

        var success = oneOf.TryGet<string>(out var value);

        Assert.IsFalse(success);
        Assert.IsNull(value);
    }

    [TestMethod]
    public void OneOfExtensions_As_ThrowsWhenWrongType()
    {
        // Arrange
        OneOf<int, string> oneOf = 42;

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => oneOf.As<string>());
    }
}
