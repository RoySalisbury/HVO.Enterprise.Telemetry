using System;
using HVO.Enterprise.Telemetry.Metrics;

namespace HVO.Enterprise.Telemetry.Tests.Metrics
{
    /// <summary>
    /// Tests for <see cref="MetricNameValidator"/> which had no dedicated tests.
    /// </summary>
    [TestClass]
    public class MetricNameValidatorTests
    {
        [TestMethod]
        public void ValidateName_NullName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => MetricNameValidator.ValidateName(null!, "testParam"));
        }

        [TestMethod]
        public void ValidateName_EmptyName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => MetricNameValidator.ValidateName(string.Empty, "testParam"));
        }

        [TestMethod]
        public void ValidateName_WhitespaceName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => MetricNameValidator.ValidateName("   ", "testParam"));
        }

        [TestMethod]
        public void ValidateName_ValidName_DoesNotThrow()
        {
            MetricNameValidator.ValidateName("valid.metric.name", "testParam");
        }

        [TestMethod]
        public void ValidateName_MinimalName_DoesNotThrow()
        {
            MetricNameValidator.ValidateName("a", "testParam");
        }

        [TestMethod]
        public void ValidateName_ExceptionContainsParameterName()
        {
            var ex = Assert.ThrowsExactly<ArgumentException>(
                () => MetricNameValidator.ValidateName(null!, "myParam"));
            Assert.IsTrue(ex.Message.Contains("non-empty") && ex.ParamName == "myParam",
                "Exception should contain 'non-empty' message and reference the parameter name");
        }
    }
}
