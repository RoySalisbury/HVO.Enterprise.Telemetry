using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Initialization
{
    [TestClass]
    public class CorrelationIdProviderTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            CorrelationContext.Clear();
        }

        [TestInitialize]
        public void Initialize()
        {
            CorrelationContext.Clear();
        }

        [TestMethod]
        public void GenerateCorrelationId_ReturnsNonNullId()
        {
            var provider = new CorrelationIdProvider();
            var id = provider.GenerateCorrelationId();

            Assert.IsNotNull(id);
            Assert.AreEqual(32, id.Length); // Guid.ToString("N") is 32 hex chars
        }

        [TestMethod]
        public void GenerateCorrelationId_SetsCorrelationContext()
        {
            var provider = new CorrelationIdProvider();
            var id = provider.GenerateCorrelationId();

            Assert.AreEqual(id, CorrelationContext.Current);
        }

        [TestMethod]
        public void GenerateCorrelationId_ReturnsDifferentIdsEachTime()
        {
            var provider = new CorrelationIdProvider();
            var first = provider.GenerateCorrelationId();
            var second = provider.GenerateCorrelationId();

            Assert.AreNotEqual(first, second);
        }

        [TestMethod]
        public void TryGetCorrelationId_ReturnsFalseWhenNotSet()
        {
            var provider = new CorrelationIdProvider();

            var result = provider.TryGetCorrelationId(out var id);

            Assert.IsFalse(result);
            Assert.IsNull(id);
        }

        [TestMethod]
        public void TryGetCorrelationId_ReturnsTrueAfterGenerate()
        {
            var provider = new CorrelationIdProvider();
            var generated = provider.GenerateCorrelationId();

            var result = provider.TryGetCorrelationId(out var id);

            Assert.IsTrue(result);
            Assert.AreEqual(generated, id);
        }

        [TestMethod]
        public void TryGetCorrelationId_ReturnsTrueWhenSetManually()
        {
            CorrelationContext.Current = "manual-id-123";

            var provider = new CorrelationIdProvider();
            var result = provider.TryGetCorrelationId(out var id);

            Assert.IsTrue(result);
            Assert.AreEqual("manual-id-123", id);
        }
    }
}
