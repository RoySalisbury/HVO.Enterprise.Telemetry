using System;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class OperationConfigurationTests
    {
        [TestMethod]
        public void OperationConfiguration_MergeWith_InheritsAndOverridesTags()
        {
            var parent = new OperationConfiguration
            {
                SamplingRate = 0.5,
                Enabled = true
            };
            parent.Tags["parent"] = "value";

            var child = new OperationConfiguration
            {
                ParameterCapture = ParameterCaptureMode.Full,
                TimeoutThresholdMs = 500
            };
            child.Tags["parent"] = "override";
            child.Tags["child"] = 123;

            var merged = child.MergeWith(parent);

            Assert.AreEqual(0.5, merged.SamplingRate);
            Assert.AreEqual(true, merged.Enabled);
            Assert.AreEqual(ParameterCaptureMode.Full, merged.ParameterCapture);
            Assert.AreEqual(500, merged.TimeoutThresholdMs);
            Assert.AreEqual(2, merged.Tags.Count);
            Assert.AreEqual("override", merged.Tags["parent"]);
            Assert.AreEqual(123, merged.Tags["child"]);
        }

        [TestMethod]
        public void OperationConfiguration_Validate_RejectsInvalidSamplingRate()
        {
            var config = new OperationConfiguration
            {
                SamplingRate = 1.5
            };

            Assert.ThrowsExactly<InvalidOperationException>(() => config.Validate());
        }
    }
}
