using System;
using HVO.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Common.Tests.Utilities;

[TestClass]
public class GuardTests
{
    [TestMethod]
    public void AgainstNull_ThrowsWhenNull()
    {
        string? value = null;
        Assert.ThrowsExactly<ArgumentNullException>(() => Guard.AgainstNull(value));
    }

    [TestMethod]
    public void AgainstNull_ReturnsValueWhenNotNull()
    {
        var value = "test";
        var result = Guard.AgainstNull(value);
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public void AgainstNullOrWhiteSpace_ThrowsWhenWhitespace()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace("   "));
    }

    [TestMethod]
    public void AgainstNullOrWhiteSpace_ReturnsValueWhenValid()
    {
        var result = Guard.AgainstNullOrWhiteSpace("test");
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public void AgainstNullOrEmpty_ThrowsWhenEmpty()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Guard.AgainstNullOrEmpty(Array.Empty<int>()));
    }

    [TestMethod]
    public void AgainstNullOrEmpty_ThrowsWhenNullCollection()
    {
        IEnumerable<int>? value = null;
        Assert.ThrowsExactly<ArgumentNullException>(() => Guard.AgainstNullOrEmpty(value));
    }

    [TestMethod]
    public void AgainstNullOrEmptyString_ThrowsWhenNull()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Guard.AgainstNullOrEmpty((string?)null));
    }

    [TestMethod]
    public void AgainstOutOfRange_ReturnsValueWhenInRange()
    {
        var result = Guard.AgainstOutOfRange(5, 1, 10);
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    public void AgainstOutOfRange_ThrowsWhenOutOfRange()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(15, 1, 10));
    }

    [TestMethod]
    public void AgainstLessThanOrEqual_ThrowsWhenZero()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Guard.AgainstLessThanOrEqual(0, 0));
    }

    [TestMethod]
    public void AgainstLessThanOrEqual_ReturnsValueWhenPositive()
    {
        var result = Guard.AgainstLessThanOrEqual(5, 0);
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    public void Against_ThrowsWhenConditionIsTrue()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Guard.Against(true, "Condition was true"));
    }

    [TestMethod]
    public void Against_DoesNotThrowWhenConditionIsFalse()
    {
        Guard.Against(false, "Condition was false"); // Should not throw
    }

    private enum TestEnum { Value1, Value2 }

    [TestMethod]
    public void AgainstInvalidEnum_ThrowsWhenInvalid()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Guard.AgainstInvalidEnum((TestEnum)99));
    }

    [TestMethod]
    public void AgainstInvalidEnum_ReturnsValueWhenValid()
    {
        var result = Guard.AgainstInvalidEnum(TestEnum.Value1);
        Assert.AreEqual(TestEnum.Value1, result);
    }
}

[TestClass]
public class EnsureTests
{
    [TestMethod]
    public void That_ThrowsWhenConditionIsFalse()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Ensure.That(false, "Condition was false"));
    }

    [TestMethod]
    public void That_DoesNotThrowWhenConditionIsTrue()
    {
        Ensure.That(true, "Condition is true"); // Should not throw
    }

    [TestMethod]
    public void NotNull_ThrowsWhenNull()
    {
        string? value = null;
        Assert.ThrowsExactly<InvalidOperationException>(() => Ensure.NotNull(value));
    }

    [TestMethod]
    public void NotNull_ReturnsValueWhenNotNull()
    {
        var value = "test";
        var result = Ensure.NotNull(value);
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public void NotNullOrWhiteSpace_ThrowsWhenWhitespace()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Ensure.NotNullOrWhiteSpace("   "));
    }

    [TestMethod]
    public void InRange_ReturnsValueWhenInRange()
    {
        var result = Ensure.InRange(5, 1, 10, "value");
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    public void InRange_ThrowsWhenOutOfRange()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Ensure.InRange(15, 1, 10, "value"));
    }

    [TestMethod]
    public void Unreachable_AlwaysThrows()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Ensure.Unreachable("This should never be reached"));
    }
}
