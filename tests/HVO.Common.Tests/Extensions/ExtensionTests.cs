using System;
using System.Linq;
using HVO.Core.Extensions;
using HvoCollectionExtensions = HVO.Core.Extensions.CollectionExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Common.Tests.Extensions;

[TestClass]
public class StringExtensionsTests
{
    private enum TestParseEnum
    {
        First,
        Second
    }

    [TestMethod]
    public void IsNullOrWhiteSpace_ReturnsTrueForNull()
    {
        string? value = null;
        Assert.IsTrue(value.IsNullOrWhiteSpace());
    }

    [TestMethod]
    public void IsNullOrWhiteSpace_ReturnsTrueForWhitespace()
    {
        Assert.IsTrue("   ".IsNullOrWhiteSpace());
    }

    [TestMethod]
    public void IsNullOrWhiteSpace_ReturnsFalseForNonEmpty()
    {
        Assert.IsFalse("test".IsNullOrWhiteSpace());
    }

    [TestMethod]
    public void Truncate_TruncatesLongString()
    {
        var result = "HelloWorld".Truncate(5);
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void Truncate_PreservesShortString()
    {
        var result = "Hi".Truncate(5);
        Assert.AreEqual("Hi", result);
    }

    [TestMethod]
    public void TruncateWithSuffix_AddsSuffix()
    {
        var result = "HelloWorld".TruncateWithSuffix(8, "...");
        Assert.AreEqual("Hello...", result);
    }

    [TestMethod]
    public void ToEnum_ParsesEnumValue()
    {
        var result = "Second".ToEnum<TestParseEnum>();
        Assert.AreEqual(TestParseEnum.Second, result);
    }

    [TestMethod]
    public void TryToEnum_ReturnsFalseForInvalid()
    {
        var success = "Invalid".TryToEnum<TestParseEnum>(out var value);

        Assert.IsFalse(success);
        Assert.AreEqual(default, value);
    }

    [TestMethod]
    public void TryToEnum_IgnoresCaseWhenRequested()
    {
        var success = "second".TryToEnum<TestParseEnum>(out var value, ignoreCase: true);

        Assert.IsTrue(success);
        Assert.AreEqual(TestParseEnum.Second, value);
    }

    [TestMethod]
    public void ToEnum_ThrowsWhenInvalid()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = "Invalid".ToEnum<TestParseEnum>());
    }

    [TestMethod]
    public void EqualsAny_ReturnsTrueWhenMatchFound()
    {
        Assert.IsTrue("Test".EqualsAny(StringComparison.OrdinalIgnoreCase, "foo", "test"));
    }

    [TestMethod]
    public void Reverse_ReturnsReversedString()
    {
        var result = "abc".Reverse();
        Assert.AreEqual("cba", result);
    }

    [TestMethod]
    public void ToTitleCase_ConvertsToTitleCase()
    {
        var result = "hello world".ToTitleCase();
        Assert.AreEqual("Hello World", result);
    }

    [TestMethod]
    public void RemoveWhitespace_RemovesAllWhitespace()
    {
        var result = "Hello World Test".RemoveWhitespace();
        Assert.AreEqual("HelloWorldTest", result);
    }

    [TestMethod]
    public void ContainsAny_ReturnsTrueWhenFound()
    {
        Assert.IsTrue("Hello World".ContainsAny("World", "Test"));
    }

    [TestMethod]
    public void ContainsAny_ReturnsFalseWhenNotFound()
    {
        Assert.IsFalse("Hello World".ContainsAny("Test", "Foo"));
    }
}

[TestClass]
public class CollectionExtensionsTests
{
    [TestMethod]
    public void IsNullOrEmpty_ReturnsTrueForNull()
    {
        int[]? collection = null;
        Assert.IsTrue(collection.IsNullOrEmpty());
    }

    [TestMethod]
    public void IsNullOrEmpty_ReturnsTrueForEmpty()
    {
        var collection = Array.Empty<int>();
        Assert.IsTrue(collection.IsNullOrEmpty());
    }

    [TestMethod]
    public void IsNullOrEmpty_ReturnsFalseForNonEmpty()
    {
        var collection = new[] { 1, 2, 3 };
        Assert.IsFalse(collection.IsNullOrEmpty());
    }

    [TestMethod]
    public void OrEmpty_ReturnsEmptyForNull()
    {
        int[]? collection = null;
        var result = collection.OrEmpty();
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Any());
    }

    [TestMethod]
    public void ForEach_ExecutesActionForEachElement()
    {
        var collection = new[] { 1, 2, 3 };
        var sum = 0;
        collection.ForEach(x => sum += x);
        Assert.AreEqual(6, sum);
    }

    [TestMethod]
    public void ForEach_WithIndex_UsesIndexValues()
    {
        var collection = new[] { "a", "b", "c" };
        var concatenated = string.Empty;

        collection.ForEach((value, index) =>
        {
            concatenated += index + value;
        });

        Assert.AreEqual("0a1b2c", concatenated);
    }

    [TestMethod]
    public void Chunk_PartitionsCollection()
    {
        var collection = new[] { 1, 2, 3, 4, 5 };
        var chunks = HvoCollectionExtensions.Chunk(collection, 2).ToList();

        Assert.AreEqual(3, chunks.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, chunks[0].ToArray());
        CollectionAssert.AreEqual(new[] { 3, 4 }, chunks[1].ToArray());
        CollectionAssert.AreEqual(new[] { 5 }, chunks[2].ToArray());
    }

    [TestMethod]
    public void Chunk_WithInvalidSize_ThrowsException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => HvoCollectionExtensions.Chunk(new[] { 1, 2, 3 }, 0).ToList());
    }

    [TestMethod]
    public void DistinctBy_RemovesDuplicatesByKey()
    {
        var items = new[]
        {
            new { Id = 1, Name = "A" },
            new { Id = 2, Name = "B" },
            new { Id = 1, Name = "C" }
        };

        var distinct = HvoCollectionExtensions.DistinctBy(items, x => x.Id).ToList();

        Assert.AreEqual(2, distinct.Count);
        Assert.AreEqual(1, distinct[0].Id);
        Assert.AreEqual(2, distinct[1].Id);
    }

    [TestMethod]
    public void Shuffle_ReturnsSameElements()
    {
        var items = new[] { 1, 2, 3, 4, 5 };
        var shuffled = items.Shuffle().ToList();

        CollectionAssert.AreEquivalent(items, shuffled);
    }

    [TestMethod]
    public void IndexOf_FindsFirstMatch()
    {
        var collection = new[] { 1, 2, 3, 4, 5 };
        var index = collection.IndexOf(x => x > 2);
        Assert.AreEqual(2, index); // Index of 3
    }

    [TestMethod]
    public void IndexOf_ReturnsNegativeWhenNotFound()
    {
        var collection = new[] { 1, 2, 3 };
        var index = collection.IndexOf(x => x > 10);
        Assert.AreEqual(-1, index);
    }
}

[TestClass]
public class EnumExtensionsTests
{
    private enum TestEnum
    {
        [System.ComponentModel.Description("First Value")]
        First,
        Second
    }

    [TestMethod]
    public void GetDescription_ReturnsDescriptionAttribute()
    {
        var description = TestEnum.First.GetDescription();
        Assert.AreEqual("First Value", description);
    }

    [TestMethod]
    public void GetDescription_ReturnsNameWhenNoAttribute()
    {
        var description = TestEnum.Second.GetDescription();
        Assert.AreEqual("Second", description);
    }

    [TestMethod]
    public void GetDescription_ReturnsEmptyForNull()
    {
        Enum? value = null;

        var description = EnumExtensions.GetDescription(value!);

        Assert.AreEqual(string.Empty, description);
    }
}
