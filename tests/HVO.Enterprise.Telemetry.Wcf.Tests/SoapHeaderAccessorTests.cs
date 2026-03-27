using System;
using System.ServiceModel.Channels;
using HVO.Enterprise.Telemetry.Wcf.Propagation;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class SoapHeaderAccessorTests
    {
        [TestMethod]
        public void AddHeader_AndGetHeader_RoundTrips()
        {
            // Arrange
            var message = Message.CreateMessage(
                MessageVersion.Soap12WSAddressing10,
                "http://tempuri.org/Test",
                "test body");

            // Act
            SoapHeaderAccessor.AddHeader(
                message.Headers,
                TraceContextConstants.TraceParentHeaderName,
                "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");

            var value = SoapHeaderAccessor.GetHeader(
                message.Headers,
                TraceContextConstants.TraceParentHeaderName);

            // Assert
            Assert.AreEqual("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", value);
        }

        [TestMethod]
        public void GetHeader_NonExistent_ReturnsNull()
        {
            // Arrange
            var message = Message.CreateMessage(
                MessageVersion.Soap12WSAddressing10,
                "http://tempuri.org/Test",
                "test body");

            // Act
            var value = SoapHeaderAccessor.GetHeader(
                message.Headers,
                "nonexistent-header");

            // Assert
            Assert.IsNull(value);
        }

        [TestMethod]
        public void AddHeader_MultipleHeaders_AllRetrievable()
        {
            // Arrange
            var message = Message.CreateMessage(
                MessageVersion.Soap12WSAddressing10,
                "http://tempuri.org/Test",
                "test body");

            // Act
            SoapHeaderAccessor.AddHeader(
                message.Headers,
                TraceContextConstants.TraceParentHeaderName,
                "traceparent-value");

            SoapHeaderAccessor.AddHeader(
                message.Headers,
                TraceContextConstants.TraceStateHeaderName,
                "tracestate-value");

            // Assert
            Assert.AreEqual("traceparent-value",
                SoapHeaderAccessor.GetHeader(message.Headers, TraceContextConstants.TraceParentHeaderName));
            Assert.AreEqual("tracestate-value",
                SoapHeaderAccessor.GetHeader(message.Headers, TraceContextConstants.TraceStateHeaderName));
        }

        [TestMethod]
        public void RemoveHeader_ExistingHeader_ReturnsTrue()
        {
            // Arrange
            var message = Message.CreateMessage(
                MessageVersion.Soap12WSAddressing10,
                "http://tempuri.org/Test",
                "test body");

            SoapHeaderAccessor.AddHeader(
                message.Headers,
                TraceContextConstants.TraceParentHeaderName,
                "test-value");

            // Act
            var removed = SoapHeaderAccessor.RemoveHeader(
                message.Headers,
                TraceContextConstants.TraceParentHeaderName);

            // Assert
            Assert.IsTrue(removed);
            Assert.IsNull(SoapHeaderAccessor.GetHeader(
                message.Headers,
                TraceContextConstants.TraceParentHeaderName));
        }

        [TestMethod]
        public void RemoveHeader_NonExistent_ReturnsFalse()
        {
            // Arrange
            var message = Message.CreateMessage(
                MessageVersion.Soap12WSAddressing10,
                "http://tempuri.org/Test",
                "test body");

            // Act
            var removed = SoapHeaderAccessor.RemoveHeader(
                message.Headers,
                "nonexistent");

            // Assert
            Assert.IsFalse(removed);
        }

        [TestMethod]
        public void SetHeader_ReplacesExistingHeader()
        {
            // Arrange
            var message = Message.CreateMessage(
                MessageVersion.Soap12WSAddressing10,
                "http://tempuri.org/Test",
                "test body");

            SoapHeaderAccessor.AddHeader(
                message.Headers,
                TraceContextConstants.TraceParentHeaderName,
                "old-value");

            // Act
            SoapHeaderAccessor.SetHeader(
                message.Headers,
                TraceContextConstants.TraceParentHeaderName,
                "new-value");

            // Assert
            Assert.AreEqual("new-value",
                SoapHeaderAccessor.GetHeader(message.Headers, TraceContextConstants.TraceParentHeaderName));
        }

        [TestMethod]
        public void SetHeader_NewHeader_AddsIt()
        {
            // Arrange
            var message = Message.CreateMessage(
                MessageVersion.Soap12WSAddressing10,
                "http://tempuri.org/Test",
                "test body");

            // Act
            SoapHeaderAccessor.SetHeader(
                message.Headers,
                TraceContextConstants.TraceParentHeaderName,
                "new-value");

            // Assert
            Assert.AreEqual("new-value",
                SoapHeaderAccessor.GetHeader(message.Headers, TraceContextConstants.TraceParentHeaderName));
        }

        [TestMethod]
        public void GetHeader_NullHeaders_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => SoapHeaderAccessor.GetHeader(null!, "test"));
        }

        [TestMethod]
        public void AddHeader_NullHeaders_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => SoapHeaderAccessor.AddHeader(null!, "name", "value"));
        }

        [TestMethod]
        public void AddHeader_EmptyName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                var message = Message.CreateMessage(
                    MessageVersion.Soap12WSAddressing10,
                    "http://tempuri.org/Test",
                    "test body");

                SoapHeaderAccessor.AddHeader(message.Headers, "", "value");
            });
        }

        [TestMethod]
        public void AddHeader_EmptyValue_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                var message = Message.CreateMessage(
                    MessageVersion.Soap12WSAddressing10,
                    "http://tempuri.org/Test",
                    "test body");

                SoapHeaderAccessor.AddHeader(message.Headers, "name", "");
            });
        }

        [TestMethod]
        public void RemoveHeader_NullHeaders_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => SoapHeaderAccessor.RemoveHeader(null!, "test"));
        }

        [TestMethod]
        public void GetHeader_EmptyName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                var message = Message.CreateMessage(
                    MessageVersion.Soap12WSAddressing10,
                    "http://tempuri.org/Test",
                    "test body");

                SoapHeaderAccessor.GetHeader(message.Headers, "");
            });
        }

        [TestMethod]
        public void AddHeader_Soap11Message_Works()
        {
            // Arrange
            var message = Message.CreateMessage(
                MessageVersion.Soap11,
                "http://tempuri.org/Test",
                "test body");

            // Act
            SoapHeaderAccessor.AddHeader(
                message.Headers,
                TraceContextConstants.TraceParentHeaderName,
                "soap11-value");

            // Assert
            Assert.AreEqual("soap11-value",
                SoapHeaderAccessor.GetHeader(message.Headers, TraceContextConstants.TraceParentHeaderName));
        }
    }
}
