using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DRB_TEMP.Service
{
    public class KepwareService
    {
        private readonly string _endpointUrl = "opc.tcp://192.168.41.30:49320";

        private ApplicationConfiguration _config;
        private Session _session;

        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private readonly SemaphoreSlim _readLock = new(1, 1);

        public async Task<object?> ReadTagAsync(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return null;
            }

            var values = await ReadMultipleTagsAsync(new List<string> { nodeId });
            var key = nodeId.Trim();

            return values.ContainsKey(key) ? values[key] : null;
        }

        public async Task<Dictionary<string, object?>> ReadMultipleTagsAsync(List<string> nodeIds)
        {
            var data = new Dictionary<string, object?>();

            if (nodeIds == null || nodeIds.Count == 0)
            {
                return data;
            }

            var validNodeIds = nodeIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            foreach (var nodeId in validNodeIds)
            {
                data[nodeId] = null;
            }

            if (validNodeIds.Count == 0)
            {
                return data;
            }

            var connected = await EnsureConnectedAsync();

            if (!connected || _session == null || !_session.Connected)
            {
                return data;
            }

            var lockTaken = await _readLock.WaitAsync(TimeSpan.FromSeconds(3));

            if (!lockTaken)
            {
                return data;
            }

            try
            {
                const int batchSize = 80;

                for (int start = 0; start < validNodeIds.Count; start += batchSize)
                {
                    if (_session == null || !_session.Connected)
                    {
                        break;
                    }

                    var batch = validNodeIds
                        .Skip(start)
                        .Take(batchSize)
                        .ToList();

                    var nodesToRead = new ReadValueIdCollection();
                    var readKeys = new List<string>();

                    foreach (var nodeId in batch)
                    {
                        try
                        {
                            nodesToRead.Add(new ReadValueId
                            {
                                NodeId = NodeId.Parse(nodeId),
                                AttributeId = Attributes.Value
                            });

                            readKeys.Add(nodeId);
                        }
                        catch
                        {
                            data[nodeId] = null;
                        }
                    }

                    if (nodesToRead.Count == 0)
                    {
                        continue;
                    }

                    DataValueCollection results;
                    DiagnosticInfoCollection diagnosticInfos;

                    _session.Read(
                        null,
                        0,
                        TimestampsToReturn.Neither,
                        nodesToRead,
                        out results,
                        out diagnosticInfos);

                    for (int i = 0; i < readKeys.Count; i++)
                    {
                        var key = readKeys[i];

                        if (results == null || i >= results.Count)
                        {
                            data[key] = null;
                            continue;
                        }

                        var result = results[i];

                        if (result == null || StatusCode.IsBad(result.StatusCode))
                        {
                            data[key] = null;
                        }
                        else
                        {
                            data[key] = result.Value;
                        }
                    }
                }
            }
            catch
            {
                ResetSession();
            }
            finally
            {
                _readLock.Release();
            }

            return data;
        }

        private async Task<bool> EnsureConnectedAsync()
        {
            if (_session != null && _session.Connected)
            {
                return true;
            }

            var lockTaken = await _connectLock.WaitAsync(TimeSpan.FromSeconds(5));

            if (!lockTaken)
            {
                return false;
            }

            try
            {
                if (_session != null && _session.Connected)
                {
                    return true;
                }

                ResetSession();

                _config = new ApplicationConfiguration
                {
                    ApplicationName = "DRB_TEMP",
                    ApplicationUri = "urn:localhost:DRB_TEMP",
                    ApplicationType = ApplicationType.Client,

                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier
                        {
                            StoreType = "Directory",
                            StorePath = "OPCFoundation/CertificateStores/MachineDefault",
                            SubjectName = "DRB_TEMP"
                        },

                        TrustedPeerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPCFoundation/CertificateStores/UA Applications"
                        },

                        TrustedIssuerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPCFoundation/CertificateStores/UA Certificate Authorities"
                        },

                        RejectedCertificateStore = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPCFoundation/CertificateStores/RejectedCertificates"
                        },

                        AutoAcceptUntrustedCertificates = true,
                        AddAppCertToTrustedStore = true
                    },

                    TransportConfigurations = new TransportConfigurationCollection(),

                    TransportQuotas = new TransportQuotas
                    {
                        OperationTimeout = 1500,
                        MaxStringLength = 1048576,
                        MaxByteStringLength = 1048576,
                        MaxArrayLength = 65535,
                        MaxMessageSize = 4194304,
                        MaxBufferSize = 65535,
                        ChannelLifetime = 600000,
                        SecurityTokenLifetime = 3600000
                    },

                    ClientConfiguration = new ClientConfiguration
                    {
                        DefaultSessionTimeout = 60000,
                        MinSubscriptionLifetime = 10000
                    }
                };

                await _config.Validate(ApplicationType.Client);

                _config.CertificateValidator.CertificateValidation += (sender, e) =>
                {
                    e.Accept = true;
                };

                var endpointDescription = CoreClientUtils.SelectEndpoint(
                    _config,
                    _endpointUrl,
                    false);

                var endpointConfiguration = EndpointConfiguration.Create(_config);

                var endpoint = new ConfiguredEndpoint(
                    null,
                    endpointDescription,
                    endpointConfiguration);

                _session = await Session.Create(
                    _config,
                    endpoint,
                    false,
                    "DRB_TEMP_SESSION",
                    60000,
                    null,
                    null);

                return _session != null && _session.Connected;
            }
            catch
            {
                ResetSession();
                return false;
            }
            finally
            {
                _connectLock.Release();
            }
        }

        private void ResetSession()
        {
            try
            {
                _session?.Close();
                _session?.Dispose();
            }
            catch
            {
            }

            _session = null;
        }
    }
}