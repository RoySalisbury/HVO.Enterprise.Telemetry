using System;
using System.Collections.Generic;
using System.Linq;
using HVO.Enterprise.Telemetry.Http;

namespace HVO.Enterprise.Telemetry.Tests.Http
{
    [TestClass]
    public class HttpInstrumentationOptionsTests
    {
        // -------------------------------------------------------------------
        // Default values
        // -------------------------------------------------------------------

        [TestMethod]
        public void Default_ReturnsNewInstance_EachAccess()
        {
            var first = HttpInstrumentationOptions.Default;
            var second = HttpInstrumentationOptions.Default;

            Assert.AreNotSame(first, second,
                "Default property must return a new instance each time.");
        }

        [TestMethod]
        public void Default_RedactQueryStrings_IsTrue()
        {
            var options = HttpInstrumentationOptions.Default;
            Assert.IsTrue(options.RedactQueryStrings);
        }

        [TestMethod]
        public void Default_CaptureRequestHeaders_IsFalse()
        {
            var options = HttpInstrumentationOptions.Default;
            Assert.IsFalse(options.CaptureRequestHeaders);
        }

        [TestMethod]
        public void Default_CaptureResponseHeaders_IsFalse()
        {
            var options = HttpInstrumentationOptions.Default;
            Assert.IsFalse(options.CaptureResponseHeaders);
        }

        [TestMethod]
        public void Default_CaptureRequestBody_IsFalse()
        {
            var options = HttpInstrumentationOptions.Default;
            Assert.IsFalse(options.CaptureRequestBody);
        }

        [TestMethod]
        public void Default_CaptureResponseBody_IsFalse()
        {
            var options = HttpInstrumentationOptions.Default;
            Assert.IsFalse(options.CaptureResponseBody);
        }

        [TestMethod]
        public void Default_MaxBodyCaptureSize_Is4096()
        {
            var options = HttpInstrumentationOptions.Default;
            Assert.AreEqual(4096, options.MaxBodyCaptureSize);
        }

        [TestMethod]
        public void Default_SensitiveHeaders_ContainsExpectedDefaults()
        {
            var options = HttpInstrumentationOptions.Default;
            var expected = new[]
            {
                "Authorization",
                "Cookie",
                "Set-Cookie",
                "X-API-Key",
                "X-Auth-Token",
                "Proxy-Authorization"
            };

            foreach (var header in expected)
            {
                Assert.IsTrue(options.SensitiveHeaders.Contains(header),
                    $"Expected sensitive header '{header}' to be present.");
            }
        }

        // -------------------------------------------------------------------
        // IsSensitiveHeader
        // -------------------------------------------------------------------

        [TestMethod]
        public void IsSensitiveHeader_KnownSensitive_ReturnsTrue()
        {
            var options = HttpInstrumentationOptions.Default;

            Assert.IsTrue(options.IsSensitiveHeader("Authorization"));
            Assert.IsTrue(options.IsSensitiveHeader("Cookie"));
            Assert.IsTrue(options.IsSensitiveHeader("X-API-Key"));
        }

        [TestMethod]
        public void IsSensitiveHeader_CaseInsensitive()
        {
            var options = HttpInstrumentationOptions.Default;

            Assert.IsTrue(options.IsSensitiveHeader("authorization"));
            Assert.IsTrue(options.IsSensitiveHeader("AUTHORIZATION"));
            Assert.IsTrue(options.IsSensitiveHeader("x-api-key"));
        }

        [TestMethod]
        public void IsSensitiveHeader_SafeHeader_ReturnsFalse()
        {
            var options = HttpInstrumentationOptions.Default;

            Assert.IsFalse(options.IsSensitiveHeader("Content-Type"));
            Assert.IsFalse(options.IsSensitiveHeader("Accept"));
            Assert.IsFalse(options.IsSensitiveHeader("X-Request-Id"));
        }

        [TestMethod]
        public void IsSensitiveHeader_NullHeader_ReturnsFalse()
        {
            var options = HttpInstrumentationOptions.Default;

            Assert.IsFalse(options.IsSensitiveHeader(null!));
        }

        [TestMethod]
        public void IsSensitiveHeader_CustomHeader_Configurable()
        {
            var options = new HttpInstrumentationOptions();
            options.AddSensitiveHeader("X-Custom-Secret");

            Assert.IsTrue(options.IsSensitiveHeader("X-Custom-Secret"));
            Assert.IsTrue(options.IsSensitiveHeader("x-custom-secret"));
        }

        // -------------------------------------------------------------------
        // Validate
        // -------------------------------------------------------------------

        [TestMethod]
        public void Validate_DefaultOptions_DoesNotThrow()
        {
            var options = HttpInstrumentationOptions.Default;

            options.Validate(); // Should not throw
        }

        [TestMethod]
        public void Validate_ZeroMaxBodyCaptureSize_Throws()
        {
            var options = new HttpInstrumentationOptions { MaxBodyCaptureSize = 0 };

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_NegativeMaxBodyCaptureSize_Throws()
        {
            var options = new HttpInstrumentationOptions { MaxBodyCaptureSize = -1 };

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.Validate());
        }

        // -------------------------------------------------------------------
        // Clone
        // -------------------------------------------------------------------

        [TestMethod]
        public void Clone_CreatesSeparateInstance()
        {
            var original = new HttpInstrumentationOptions
            {
                RedactQueryStrings = false,
                CaptureRequestHeaders = true,
                CaptureResponseHeaders = true,
                MaxBodyCaptureSize = 8192
            };

            var clone = original.Clone();

            Assert.AreNotSame(original, clone);
            Assert.AreEqual(false, clone.RedactQueryStrings);
            Assert.AreEqual(true, clone.CaptureRequestHeaders);
            Assert.AreEqual(true, clone.CaptureResponseHeaders);
            Assert.AreEqual(8192, clone.MaxBodyCaptureSize);
        }

        [TestMethod]
        public void Clone_SensitiveHeaders_IsIndependentCopy()
        {
            var original = new HttpInstrumentationOptions();
            var clone = original.Clone();

            clone.AddSensitiveHeader("X-New-Header");

            Assert.IsFalse(original.SensitiveHeaders.Contains("X-New-Header"),
                "Modifying clone should not affect original.");
        }

        [TestMethod]
        public void Clone_MutatingOriginal_DoesNotAffectClone()
        {
            var original = new HttpInstrumentationOptions { RedactQueryStrings = true };
            var clone = original.Clone();

            original.RedactQueryStrings = false;

            Assert.IsTrue(clone.RedactQueryStrings,
                "Mutating original should not affect clone.");
        }
    }
}
