using System;
using HVO.Core.OneOf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Common.Tests.OneOf;

[TestClass]
public class NamedOneOfAttributeTests
{
    [TestMethod]
    public void Constructor_ValidPairs_PopulatesCases()
    {
        var attribute = new NamedOneOfAttribute(
            "Ok", typeof(string),
            "Error", typeof(Exception));

        Assert.AreEqual(2, attribute.Cases.Length);
        Assert.AreEqual("Ok", attribute.Cases[0].Name);
        Assert.AreEqual(typeof(string), attribute.Cases[0].Type);
        Assert.AreEqual("Error", attribute.Cases[1].Name);
        Assert.AreEqual(typeof(Exception), attribute.Cases[1].Type);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_NullArgs_Throws()
    {
        _ = new NamedOneOfAttribute(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_OddArgumentCount_Throws()
    {
        _ = new NamedOneOfAttribute("Ok", typeof(string), "Extra");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_NonStringName_Throws()
    {
        _ = new NamedOneOfAttribute(42, typeof(string));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_NonTypeValue_Throws()
    {
        _ = new NamedOneOfAttribute("Ok", 42);
    }
}
