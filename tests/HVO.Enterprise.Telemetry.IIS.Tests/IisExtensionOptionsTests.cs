using System;
using HVO.Enterprise.Telemetry.IIS.Configuration;

namespace HVO.Enterprise.Telemetry.IIS.Tests
{
    /// <summary>
    /// Tests for <see cref="IisExtensionOptions"/> configuration and validation.
    /// </summary>
    [TestClass]
    public sealed class IisExtensionOptionsTests
    {
        [TestMethod]
        public void DefaultValues_AreCorrect()
        {
            var options = new IisExtensionOptions();

            Assert.AreEqual(TimeSpan.FromSeconds(25), options.ShutdownTimeout);
            Assert.IsTrue(options.AutoInitialize);
            Assert.IsTrue(options.RegisterWithHostingEnvironment);
            Assert.IsNull(options.OnPreShutdown);
            Assert.IsNull(options.OnPostShutdown);
        }

        [TestMethod]
        public void ShutdownTimeout_CanBeCustomized()
        {
            var options = new IisExtensionOptions
            {
                ShutdownTimeout = TimeSpan.FromSeconds(10)
            };

            Assert.AreEqual(TimeSpan.FromSeconds(10), options.ShutdownTimeout);
        }

        [TestMethod]
        public void AutoInitialize_CanBeDisabled()
        {
            var options = new IisExtensionOptions
            {
                AutoInitialize = false
            };

            Assert.IsFalse(options.AutoInitialize);
        }

        [TestMethod]
        public void RegisterWithHostingEnvironment_CanBeDisabled()
        {
            var options = new IisExtensionOptions
            {
                RegisterWithHostingEnvironment = false
            };

            Assert.IsFalse(options.RegisterWithHostingEnvironment);
        }

        [TestMethod]
        public void Validate_Succeeds_WithDefaultValues()
        {
            var options = new IisExtensionOptions();
            // Should not throw
            options.Validate();
        }

        [TestMethod]
        public void Validate_Succeeds_WithZeroTimeout()
        {
            var options = new IisExtensionOptions
            {
                ShutdownTimeout = TimeSpan.Zero
            };

            // Zero timeout is valid (immediate shutdown)
            options.Validate();
        }

        [TestMethod]
        public void Validate_Succeeds_WithMaxTimeout()
        {
            var options = new IisExtensionOptions
            {
                ShutdownTimeout = TimeSpan.FromSeconds(120)
            };

            // 120 seconds is the maximum allowed
            options.Validate();
        }

        [TestMethod]
        public void Validate_ThrowsArgumentOutOfRange_ForNegativeTimeout()
        {
            var options = new IisExtensionOptions
            {
                ShutdownTimeout = TimeSpan.FromSeconds(-1)
            };

            var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.Validate());
            Assert.AreEqual("ShutdownTimeout", ex.ParamName);
        }

        [TestMethod]
        public void Validate_ThrowsArgumentOutOfRange_ForExcessiveTimeout()
        {
            var options = new IisExtensionOptions
            {
                ShutdownTimeout = TimeSpan.FromSeconds(121)
            };

            var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.Validate());
            Assert.AreEqual("ShutdownTimeout", ex.ParamName);
        }

        [TestMethod]
        public void OnPreShutdown_CanBeSet()
        {
            var called = false;
            var options = new IisExtensionOptions
            {
                OnPreShutdown = async (ct) => { called = true; await System.Threading.Tasks.Task.CompletedTask; }
            };

            Assert.IsNotNull(options.OnPreShutdown);

            // Invoke to verify the delegate is callable
            options.OnPreShutdown!(System.Threading.CancellationToken.None).GetAwaiter().GetResult();
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void OnPostShutdown_CanBeSet()
        {
            var called = false;
            var options = new IisExtensionOptions
            {
                OnPostShutdown = async (ct) => { called = true; await System.Threading.Tasks.Task.CompletedTask; }
            };

            Assert.IsNotNull(options.OnPostShutdown);

            options.OnPostShutdown!(System.Threading.CancellationToken.None).GetAwaiter().GetResult();
            Assert.IsTrue(called);
        }
    }
}
