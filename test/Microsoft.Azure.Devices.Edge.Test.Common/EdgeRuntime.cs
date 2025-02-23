// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Test.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Edge.Test.Common.Config;
    using Microsoft.Azure.Devices.Edge.Util;
    using Newtonsoft.Json;

    public class EdgeRuntime
    {
        readonly Option<string> agentImage;
        readonly Option<string> hubImage;
        readonly IotHub iotHub;
        readonly bool optimizeForPerformance;
        readonly Option<Uri> proxy;
        readonly IEnumerable<Registry> registries;

        public string DeviceId { get; }

        public EdgeRuntime(string deviceId, Option<string> agentImage, Option<string> hubImage, Option<Uri> proxy, IEnumerable<Registry> registries, bool optimizeForPerformance, IotHub iotHub)
        {
            this.agentImage = agentImage;
            this.hubImage = hubImage;
            this.iotHub = iotHub;
            this.optimizeForPerformance = optimizeForPerformance;
            this.proxy = proxy;
            this.registries = registries;

            this.DeviceId = deviceId;
        }

        // DeployConfigurationAsync builds a configuration that includes Edge Agent, Edge Hub, and
        // anything added by addConfig(). It deploys the config and waits for the edge device to
        // receive it and start up all the modules.
        public async Task<EdgeDeployment> DeployConfigurationAsync(
            Action<EdgeConfigBuilder> addConfig,
            CancellationToken token,
            bool nestedEdge,
            ManifestSettings enableManifestSigning1 = null)
        {
            var enableManifestSigning = Option.Maybe(enableManifestSigning1);
            (string, string)[] hubEnvVar = new (string, string)[] { ("RuntimeLogLevel", "debug"), ("SslProtocols", "tls1.2") };

            if (nestedEdge == true)
            {
                hubEnvVar.Append(("DeviceScopeCacheRefreshDelaySecs", "0"));
            }
            else
            {
                hubEnvVar.Append(("NestedEdgeEnabled", "false"));
            }

            var builder = new EdgeConfigBuilder(this.DeviceId);
            builder.AddRegistries(this.registries);
            builder.AddEdgeAgent(this.agentImage.OrDefault())
                .WithEnvironment(new[] { ("RuntimeLogLevel", "debug") })
                .WithProxy(this.proxy);
            builder.AddEdgeHub(this.hubImage.OrDefault(), this.optimizeForPerformance)
                .WithEnvironment(hubEnvVar)
                .WithProxy(this.proxy);

            addConfig(builder);
            DateTime deployTime = DateTime.Now;
            EdgeConfiguration edgeConfiguration = builder.Build();
            string signedConfig = string.Empty;

            if (enableManifestSigning.HasValue)
            {
                // Write the current config into a file: EdgeConfiguration ToString() outputs the ConfigurationContent
                string deploymentPath = enableManifestSigning.OrDefault().ManifestSigningDeploymentPath.OrDefault();
                File.WriteAllText(deploymentPath, edgeConfiguration.ToString());

                // Run Manifest signer client
                string projectDirectory = enableManifestSigning.OrDefault().ManifestSignerClientProjectPath.OrDefault();
                string dotnetCmdText = "run -p " + projectDirectory;
                CancellationTokenSource manifestSignerCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

                await Process.RunAsync(
                    "dotnet",
                    dotnetCmdText,
                    manifestSignerCts.Token);

                // write the signed deployment into a file
                string signedDeploymentPath = enableManifestSigning.OrDefault().ManifestSigningSignedDeploymentPath.OrDefault();
                signedConfig = File.ReadAllText(signedDeploymentPath);
            }

            if (!string.IsNullOrEmpty(signedConfig))
            {
                // Convert signed config to ConfigurationContent
                edgeConfiguration.Config = JsonConvert.DeserializeObject<ConfigurationContent>(signedConfig);
            }

            await edgeConfiguration.DeployAsync(this.iotHub, token);
            EdgeModule[] modules = edgeConfiguration.ModuleNames
                .Select(id => new EdgeModule(id, this.DeviceId, this.iotHub))
                .ToArray();
            await EdgeModule.WaitForStatusAsync(modules, EdgeModuleStatus.Running, token);
            await edgeConfiguration.VerifyAsync(this.iotHub, token);
            return new EdgeDeployment(deployTime, modules);
        }

        public Task<EdgeDeployment> DeployConfigurationAsync(CancellationToken token, bool nestedEdge) =>
            this.DeployConfigurationAsync(_ => { }, token, nestedEdge);
    }
}
