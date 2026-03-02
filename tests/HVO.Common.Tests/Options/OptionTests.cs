using System;
using HVO.Core.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Common.Tests.Options;

[TestClass]
public class OptionTests
{
    [TestMethod]
    public void Option_WithValue_HasValue()
    {
        // Arrange & Act
        var option = new Option<int>(42);

        // Assert
        Assert.IsTrue(option.HasValue);
        Assert.AreEqual(42, option.Value);
    }

    [TestMethod]
    public void Option_None_HasNoValue()
    {
        // Arrange & Act
        var option = Option<int>.None();

        // Assert
        Assert.IsFalse(option.HasValue);
        Assert.AreEqual(default(int), option.Value);
    }

    [TestMethod]
    public void Option_ToString_ReturnsValue()
    {
        // Arrange
        var option = new Option<string>("test");

        // Act
        var result = option.ToString();

        // Assert
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public void Option_ToString_ReturnsNoneWhenEmpty()
    {
        // Arrange
        var option = Option<string>.None();

        // Act
        var result = option.ToString();

        // Assert
        Assert.AreEqual("<None>", result);
    }
}

[TestClass]
public class OptionExtensionsTests
{
    [TestMethod]
    public void Map_TransformsValueWhenPresent()
    {
        // Arrange
        var option = new Option<int>(42);

        // Act
        var mapped = option.Map(x => x.ToString());

        // Assert
        Assert.IsTrue(mapped.HasValue);
        Assert.AreEqual("42", mapped.Value);
    }

    [TestMethod]
    public void Map_ReturnsNoneWhenNoValue()
    {
        // Arrange
        var option = Option<int>.None();

        // Act
        var mapped = option.Map(x => x.ToString());

        // Assert
        Assert.IsFalse(mapped.HasValue);
    }

    [TestMethod]
    public void Bind_FlatMapsWhenValuePresent()
    {
        // Arrange
        var option = new Option<int>(42);

        // Act
        var bound = option.Bind(x => new Option<string>(x.ToString()));

        // Assert
        Assert.IsTrue(bound.HasValue);
        Assert.AreEqual("42", bound.Value);
    }

    [TestMethod]
    public void Bind_ReturnsNoneWhenNoValue()
    {
        // Arrange
        var option = Option<int>.None();

        // Act
        var bound = option.Bind(x => new Option<string>(x.ToString()));

        // Assert
        Assert.IsFalse(bound.HasValue);
    }

    [TestMethod]
    public void OnSome_InvokesActionWhenValuePresent()
    {
        var option = new Option<int>(42);
        var called = false;

        option.OnSome(value =>
        {
            called = true;
            Assert.AreEqual(42, value);
        });

        Assert.IsTrue(called);
    }

    [TestMethod]
    public void OnNone_InvokesActionWhenValueAbsent()
    {
        var option = Option<int>.None();
        var called = false;

        option.OnNone(() => called = true);

        Assert.IsTrue(called);
    }

    [TestMethod]
    public void WhereSome_FiltersValues()
    {
        var options = new[]
        {
            new Option<int>(1),
            Option<int>.None(),
            new Option<int>(2)
        };

        var values = options.WhereSome();

        CollectionAssert.AreEqual(new[] { 1, 2 }, new System.Collections.Generic.List<int>(values));
    }

    [TestMethod]
    public void GetValueOrDefault_ReturnsValueWhenPresent()
    {
        // Arrange
        var option = new Option<int>(42);

        // Act
        var value = option.GetValueOrDefault(0);

        // Assert
        Assert.AreEqual(42, value);
    }

    [TestMethod]
    public void GetValueOrDefault_ReturnsDefaultWhenAbsent()
    {
        // Arrange
        var option = Option<int>.None();

        // Act
        var value = option.GetValueOrDefault(0);

        // Assert
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void GetValueOrDefault_Factory_UsesDefaultWhenAbsent()
    {
        var option = Option<int>.None();
        var called = false;

        var value = option.GetValueOrDefault(() =>
        {
            called = true;
            return 5;
        });

        Assert.IsTrue(called);
        Assert.AreEqual(5, value);
    }

    [TestMethod]
    public void GetValueOrDefault_Factory_SkipsFactoryWhenPresent()
    {
        var option = new Option<int>(12);
        var called = false;

        var value = option.GetValueOrDefault(() =>
        {
            called = true;
            return 5;
        });

        Assert.IsFalse(called);
        Assert.AreEqual(12, value);
    }

    [TestMethod]
    public void ToNullable_ReturnsValueWhenPresent()
    {
        // Arrange
        var option = new Option<string>("test");

        // Act
        var value = option.ToNullable();

        // Assert
        Assert.AreEqual("test", value);
    }

    [TestMethod]
    public void ToNullable_ReturnsNullWhenAbsent()
    {
        // Arrange
        var option = Option<string>.None();

        // Act
        var value = option.ToNullable();

        // Assert
        Assert.IsNull(value);
    }

    [TestMethod]
    public void ToOption_CreatesOptionFromNonNullValue()
    {
        // Arrange
        string value = "test";

        // Act
        var option = value.ToOption();

        // Assert
        Assert.IsTrue(option.HasValue);
        Assert.AreEqual("test", option.Value);
    }

    [TestMethod]
    public void ToOption_CreatesNoneFromNullValue()
    {
        // Arrange
        string? value = null;

        // Act
        var option = value.ToOption();

        // Assert
        Assert.IsFalse(option.HasValue);
    }
}
