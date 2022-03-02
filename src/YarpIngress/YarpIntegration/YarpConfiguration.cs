using k8s.Models;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using YarpIngress.Extensions;
using YarpIngress.K8sInt;

namespace YarpIngress.YarpIntegration
{
    
    public class IngressConfig
    {
        private readonly List<ClusterConfig> _clusters;
        private readonly List<RouteConfig> _routes;
        public IEnumerable<ClusterConfig> ClusterConfigs { get => _clusters; }
        public IEnumerable<RouteConfig> RouteConfigs { get => _routes; }

        public IngressConfig()
        {
            _routes = new List<RouteConfig>();
            _clusters = new List<ClusterConfig>();
        }

        public IngressConfig FillDataFrom(V1Ingress ingress, IK8sObjectsDefinitionsProvider serviceDefinitionsProvider)
        {
            foreach (var rule in ingress.Spec.Rules)
            {
                foreach (var path in rule.Http.Paths)
                {
                    var cluster = new ClusterConfig()
                    {
                        Destinations = GetDestinations(ingress, path, serviceDefinitionsProvider),
                        ClusterId = Guid.NewGuid().ToString()
                    };

                    var route = new RouteConfig()
                    {
                        ClusterId = cluster.ClusterId,
                        Match = new RouteMatch()
                        {
                            Hosts = new[] { rule.Host },
                            Path = path.Path,
                        },
                        RouteId = Guid.NewGuid().ToString()
                    };
                    _clusters.Add(cluster);
                    _routes.Add(route);
                }

            }
            return this;
        }

        private IReadOnlyDictionary<string, DestinationConfig> GetDestinations(V1Ingress ingress, V1HTTPIngressPath path, IK8sObjectsDefinitionsProvider serviceDefinitionsProvider)
        {

            var dict = ingress.YarpAffinity() switch
            {
                AffinityMode.Service =>
                    new Dictionary<string, DestinationConfig>()
                    {
                        ["service"] = new DestinationConfig()
                        {
                            Address = GetDestinationAddressByServiceName(ingress, path, serviceDefinitionsProvider)
                        }
                    },
                AffinityMode.ServiceCusterIP =>
                    new Dictionary<string, DestinationConfig>()
                    {
                        ["service"] = new DestinationConfig()
                        {
                            Address = GetDestinationAddressByServiceClusterIP(ingress, path, serviceDefinitionsProvider)
                        }
                    },

                AffinityMode.RandomPod => GetDestinationAddressesByEndpoints(ingress, path, serviceDefinitionsProvider),
                _ => new Dictionary<string, DestinationConfig>()
            };
            return dict;

        }

        private Dictionary<string, DestinationConfig> GetDestinationAddressesByEndpoints(V1Ingress ingress, V1HTTPIngressPath path, IK8sObjectsDefinitionsProvider serviceDefinitionsProvider)
        {
            var ns = ingress.SafeNamespace();
            var endpointName = path.Backend.Service.Name;
            var endpoints = serviceDefinitionsProvider.GetEndpointsDefinition($"{endpointName}:{ns}");
            var destinations = new Dictionary<string, DestinationConfig>();
            foreach (var subset in endpoints.Subsets)
            {
                foreach (var address in subset.Addresses) 
                {
                    destinations.Add(Guid.NewGuid().ToString(), new DestinationConfig()
                    {
                        Address = $"http://{address.Ip}"
                    });
                }
            }

            return destinations;
        }

        private string GetDestinationAddressByServiceClusterIP(V1Ingress ingress, V1HTTPIngressPath path, IK8sObjectsDefinitionsProvider serviceDefinitionsProvider)
        {
            var ns = ingress.SafeNamespace();
            var serviceName = path.Backend.Service.Name;
            var svc = serviceDefinitionsProvider.GetServiceDefinition($"{serviceName}:{ns}");
            var portNumber = path.Backend.Service.Port.Number ?? svc?.PortByName(path.Backend.Service.Port.Name);
            var ip = svc.Spec.ClusterIP;
            return $"http://{ip}:{portNumber}";
        }

        private string GetDestinationAddressByServiceName(V1Ingress ingress, V1HTTPIngressPath path, IK8sObjectsDefinitionsProvider serviceDefinitionsProvider)
        {
            var ns = string.IsNullOrEmpty(ingress.Namespace()) ? "default" : ingress.Namespace();
            var serviceName = path.Backend.Service.Name;
            var portNumber = path.Backend.Service.Port.Number ?? serviceDefinitionsProvider.GetServiceDefinition(serviceName)?.PortByName(path.Backend.Service.Port.Name);
            return $"http://{serviceName}.{ns}.svc.cluster.local:{portNumber}";
        }
    }


    public record YarpConfigurationSnapshot : IProxyConfig
    {
        public IReadOnlyList<RouteConfig> Routes { get; init; }

        public IReadOnlyList<ClusterConfig> Clusters { get; init; }

        public IChangeToken ChangeToken { get; }

        private readonly CancellationTokenSource _cts;

        public YarpConfigurationSnapshot()
        {
            _cts = new CancellationTokenSource();
            ChangeToken = new CancellationChangeToken(_cts.Token);
        }

        public void TriggerChange()
        {
            _cts.Cancel();
        }

    }

    public class YarpConfiguration 
    {
        private readonly List<RouteConfig> _routes;
        private readonly List<ClusterConfig> _clusterConfigs;
        public bool HasPendingChanges { get; private set; }
        private readonly Dictionary<string, IngressConfig> _ingresses;

        private YarpConfigurationSnapshot _currentSnapshot;

        public YarpConfiguration()
        {
            _routes = new List<RouteConfig>();
            _clusterConfigs = new List<ClusterConfig>();
            _ingresses = new Dictionary<string, IngressConfig>();
            _currentSnapshot = new YarpConfigurationSnapshot() { Clusters = _clusterConfigs, Routes = _routes };
        }

        public void TriggerConfigurationChangeIfNeeded()
        {
            if (HasPendingChanges)
            {
                HasPendingChanges = false;
                var oldSnapshot = _currentSnapshot;
                _currentSnapshot = new YarpConfigurationSnapshot() { Clusters = _clusterConfigs, Routes = _routes };
                oldSnapshot.TriggerChange();
            }

        }


        public void AddIngressDefinition(V1Ingress ingress, IK8sObjectsDefinitionsProvider serviceDefinitionsProvider)
        {
            var ns = string.IsNullOrEmpty(ingress.Namespace()) ? "default" : ingress.Namespace();
            var ingressConfig = new IngressConfig().FillDataFrom(ingress, serviceDefinitionsProvider);
            _ingresses.Add($"{ingress.Name()}:{ns}",  ingressConfig);
            _clusterConfigs.AddRange(ingressConfig.ClusterConfigs);
            _routes.AddRange(ingressConfig.RouteConfigs);
            HasPendingChanges = true;
        }

        public IProxyConfig CurrentSnapshot() => _currentSnapshot;
    }
}
