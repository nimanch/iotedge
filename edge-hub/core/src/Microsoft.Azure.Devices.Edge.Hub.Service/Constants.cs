// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Hub.Service
{
    public static class Constants
    {
        public const int CertificateValidityDays = 90;
        public const string ConfigFileName = "appsettings_hub.json";
        public const string EdgeHubStorageFolder = "edgeHub";
        public const string EdgeHubStorageBackupFolder = "edgeHub_backup";
        public const string InitializationVectorFileName = "EdgeHubIV";
        public const string TopicNameConversionSectionName = "mqttTopicNameConversion";
        public const string VersionInfoFileName = "versionInfo.json";
        public const string WorkloadApiVersion = "2020-07-07";

        public static class ConfigKey
        {
            public const string DeviceId = "IOTEDGE_DEVICEID";
            public const string EdgeDeviceHostName = "EDGEDEVICEHOSTNAME";
            public const string EdgeHubServerCAChainCertificateFile = "EdgeModuleHubServerCAChainCertificateFile";
            public const string EdgeHubServerCertificateFile = "EdgeModuleHubServerCertificateFile";
            public const string IotHubConnectionString = "IotHubConnectionString";
            public const string IotHubHostname = "IOTEDGE_IOTHUBHOSTNAME";
            public const string GatewayHostname = "IOTEDGE_GATEWAYHOSTNAME";
            public const string ModuleGenerationId = "IOTEDGE_MODULEGENERATIONID";
            public const string ModuleId = "IOTEDGE_MODULEID";
            public const string WorkloadUri = "IOTEDGE_WORKLOADURI";
            public const string WorkloadAPiVersion = "IOTEDGE_APIVERSION";
            public const string EdgeHubDevServerCertificateFile = "EdgeHubDevServerCertificateFile";
            public const string EdgeHubDevServerPrivateKeyFile = "EdgeHubDevServerPrivateKeyFile";
            public const string EdgeHubDevTrustBundleFile = "EdgeHubDevTrustBundleFile";
            public const string EdgeHubClientCertAuthEnabled = "ClientCertAuthEnabled";
            public const string SslProtocols = "SslProtocols";
            public const string StorageMaxTotalWalSize = "RocksDB_MaxTotalWalSize";
            public const string StorageMaxManifestFileSize = "RocksDB_MaxManifestFileSize";
            public const string StorageMaxOpenFiles = "RocksDB_MaxOpenFiles";
            public const string StorageLogLevel = "Storage_LogLevel";
            public const string ExperimentalFeatures = "experimentalFeatures";
            public const string NestedEdgeEnabled = "NestedEdgeEnabled";
            public const string MqttBrokerEnabled = "mqttBrokerEnabled";
        }
    }
}
