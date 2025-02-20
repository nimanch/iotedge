// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Agent.IoTHub.Test
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Edge.Agent.Core;
    using Microsoft.Azure.Devices.Edge.Agent.Edgelet;
    using Microsoft.Azure.Devices.Edge.Agent.IoTHub.SdkClient;
    using Microsoft.Azure.Devices.Edge.Util;
    using Microsoft.Azure.Devices.Edge.Util.Test.Common;
    using Moq;
    using Xunit;

    [Unit]
    public class ModuleClientProviderTest
    {
        [Theory]
        [MemberData(nameof(GetTestData))]
        public async Task CreateFromEnvironmentTest(
            Option<UpstreamProtocol> upstreamProtocol,
            Option<IWebProxy> webProxy,
            string productInfo)
        {
            // Arrange
            ITransportSettings receivedTransportSettings = null;

            var sdkModuleClient = new Mock<ISdkModuleClient>();
            var runtimeInfoProvider = new Mock<IRuntimeInfoProvider>();
            var systemInfo = new SystemInfo("foo", "bar", "baz");

            runtimeInfoProvider.Setup(p => p.GetSystemInfo(CancellationToken.None))
                .ReturnsAsync(systemInfo);

            var sdkModuleClientProvider = new Mock<ISdkModuleClientProvider>();
            sdkModuleClientProvider.Setup(s => s.GetSdkModuleClient(It.IsAny<ITransportSettings>()))
                .Callback<ITransportSettings>(t => receivedTransportSettings = t)
                .ReturnsAsync(sdkModuleClient.Object);

            bool closeOnIdleTimeout = false;
            TimeSpan idleTimeout = TimeSpan.FromMinutes(5);
            ConnectionStatusChangesHandler handler = (status, reason) => { };

            // Act
            var moduleClientProvider = new ModuleClientProvider(
                sdkModuleClientProvider.Object,
                Option.Some(Task.FromResult(runtimeInfoProvider.Object)),
                upstreamProtocol,
                webProxy,
                productInfo,
                closeOnIdleTimeout,
                idleTimeout,
                false);
            IModuleClient moduleClient = await moduleClientProvider.Create(handler);

            // Assert
            Assert.NotNull(moduleClient);
            sdkModuleClientProvider.Verify(s => s.GetSdkModuleClient(It.IsAny<ITransportSettings>()), Times.Once);

            // Write product info explicitly
            sdkModuleClient.Verify(s => s.SetProductInfo($"{productInfo} (kernel_name=foo;cpu_architecture=bar;)"), Times.Once);

            Assert.NotNull(receivedTransportSettings);
            UpstreamProtocol up = upstreamProtocol.GetOrElse(UpstreamProtocol.Amqp);
            Assert.Equal(up, moduleClient.UpstreamProtocol);
            switch (up)
            {
                case UpstreamProtocol.Amqp:
                case UpstreamProtocol.AmqpWs:
                    var amqpTransportSettings = receivedTransportSettings as AmqpTransportSettings;
                    Assert.NotNull(amqpTransportSettings);

                    if (up == UpstreamProtocol.AmqpWs)
                    {
                        webProxy.ForEach(w => Assert.Equal(w, amqpTransportSettings.Proxy));
                    }

                    break;

                case UpstreamProtocol.Mqtt:
                case UpstreamProtocol.MqttWs:
                    var mqttTransportSettings = receivedTransportSettings as MqttTransportSettings;
                    Assert.NotNull(mqttTransportSettings);

                    if (up == UpstreamProtocol.MqttWs)
                    {
                        webProxy.ForEach(w => Assert.Equal(w, mqttTransportSettings.Proxy));
                    }

                    break;
            }

            sdkModuleClient.Verify(s => s.OpenAsync(), Times.Once);
        }

        [Theory]
        [MemberData(nameof(GetTestData))]
        public async Task CreateFromEnvironment_NoRuntimeInfoProvider(
            Option<UpstreamProtocol> upstreamProtocol,
            Option<IWebProxy> webProxy,
            string productInfo)
        {
            // Arrange
            ITransportSettings receivedTransportSettings = null;

            var sdkModuleClient = new Mock<ISdkModuleClient>();

            var sdkModuleClientProvider = new Mock<ISdkModuleClientProvider>();
            sdkModuleClientProvider.Setup(s => s.GetSdkModuleClient(It.IsAny<ITransportSettings>()))
                .Callback<ITransportSettings>(t => receivedTransportSettings = t)
                .ReturnsAsync(sdkModuleClient.Object);

            bool closeOnIdleTimeout = false;
            TimeSpan idleTimeout = TimeSpan.FromMinutes(5);
            ConnectionStatusChangesHandler handler = (status, reason) => { };

            // Act
            var moduleClientProvider = new ModuleClientProvider(
                sdkModuleClientProvider.Object,
                Option.None<Task<IRuntimeInfoProvider>>(),
                upstreamProtocol,
                webProxy,
                productInfo,
                closeOnIdleTimeout,
                idleTimeout,
                false);
            IModuleClient moduleClient = await moduleClientProvider.Create(handler);

            // Write product info explicitly
            sdkModuleClient.Verify(s => s.SetProductInfo(productInfo), Times.Once);
        }

        [Fact]
        public void CreateFromEnvironment_NullProductInfo_ShouldThrow()
        {
            // Arrange
            ITransportSettings receivedTransportSettings = null;

            var sdkModuleClient = new Mock<ISdkModuleClient>();
            var runtimeInfoProvider = new Mock<IRuntimeInfoProvider>();

            var sdkModuleClientProvider = new Mock<ISdkModuleClientProvider>();
            sdkModuleClientProvider.Setup(s => s.GetSdkModuleClient(It.IsAny<ITransportSettings>()))
                .Callback<ITransportSettings>(t => receivedTransportSettings = t)
                .ReturnsAsync(sdkModuleClient.Object);

            bool closeOnIdleTimeout = false;
            TimeSpan idleTimeout = TimeSpan.FromMinutes(5);
            ConnectionStatusChangesHandler handler = (status, reason) => { };

            // Assert
            Assert.Throws<ArgumentNullException>(() => new ModuleClientProvider(
                sdkModuleClientProvider.Object,
                Option.Some(Task.FromResult(runtimeInfoProvider.Object)),
                Option.None<UpstreamProtocol>(),
                Option.None<IWebProxy>(),
                null,
                closeOnIdleTimeout,
                idleTimeout,
                false));
        }

        [Theory]
        [MemberData(nameof(GetTestData))]
        public async Task CreateFromConnectionStringTest(
            Option<UpstreamProtocol> upstreamProtocol,
            Option<IWebProxy> webProxy,
            string productInfo)
        {
            // Arrange
            string connectionString = "DummyConnectionString";
            ITransportSettings receivedTransportSettings = null;

            var sdkModuleClient = new Mock<ISdkModuleClient>();

            var sdkModuleClientProvider = new Mock<ISdkModuleClientProvider>();
            sdkModuleClientProvider.Setup(s => s.GetSdkModuleClient(connectionString, It.IsAny<ITransportSettings>()))
                .Callback<string, ITransportSettings>((c, t) => receivedTransportSettings = t)
                .Returns(sdkModuleClient.Object);

            bool closeOnIdleTimeout = false;
            TimeSpan idleTimeout = TimeSpan.FromMinutes(5);
            ConnectionStatusChangesHandler handler = (status, reason) => { };

            // Act
            var moduleClientProvider = new ModuleClientProvider(
                connectionString,
                sdkModuleClientProvider.Object,
                upstreamProtocol,
                webProxy,
                productInfo,
                closeOnIdleTimeout,
                idleTimeout,
                false);
            IModuleClient moduleClient = await moduleClientProvider.Create(handler);

            // Assert
            Assert.NotNull(moduleClient);
            sdkModuleClientProvider.Verify(s => s.GetSdkModuleClient(connectionString, It.IsAny<ITransportSettings>()), Times.Once);

            sdkModuleClient.Verify(s => s.SetProductInfo(productInfo), Times.Once);

            Assert.NotNull(receivedTransportSettings);
            UpstreamProtocol up = upstreamProtocol.GetOrElse(UpstreamProtocol.Amqp);
            Assert.Equal(up, moduleClient.UpstreamProtocol);
            switch (up)
            {
                case UpstreamProtocol.Amqp:
                case UpstreamProtocol.AmqpWs:
                    var amqpTransportSettings = receivedTransportSettings as AmqpTransportSettings;
                    Assert.NotNull(amqpTransportSettings);

                    if (up == UpstreamProtocol.AmqpWs)
                    {
                        webProxy.ForEach(w => Assert.Equal(w, amqpTransportSettings.Proxy));
                    }

                    break;

                case UpstreamProtocol.Mqtt:
                case UpstreamProtocol.MqttWs:
                    var mqttTransportSettings = receivedTransportSettings as MqttTransportSettings;
                    Assert.NotNull(mqttTransportSettings);

                    if (up == UpstreamProtocol.MqttWs)
                    {
                        webProxy.ForEach(w => Assert.Equal(w, mqttTransportSettings.Proxy));
                    }

                    break;
            }

            sdkModuleClient.Verify(s => s.OpenAsync(), Times.Once);
        }

        public static IEnumerable<object[]> GetTestData()
        {
            yield return new object[]
            {
                Option.None<UpstreamProtocol>(),
                Option.None<IWebProxy>(),
                Constants.IoTEdgeAgentProductInfoIdentifier
            };

            yield return new object[]
            {
                Option.Some(UpstreamProtocol.Amqp),
                Option.None<IWebProxy>(),
                Constants.IoTEdgeAgentProductInfoIdentifier
            };

            yield return new object[]
            {
                Option.Some(UpstreamProtocol.Amqp),
                Option.Some(Mock.Of<IWebProxy>()),
                Constants.IoTEdgeAgentProductInfoIdentifier + "/1.0.0"
            };
        }
    }
}
