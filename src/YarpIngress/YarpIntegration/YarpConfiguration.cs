using k8s.Models;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

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

        public IngressConfig FillDataFrom(V1Ingress ingress)
        {
            var ns = string.IsNullOrEmpty(ingress.Namespace()) ? "default" : ingress.Namespace();
            foreach (var rule in ingress.Spec.Rules)
            {
                foreach (var path in rule.Http.Paths)
                {
                    var cluster = new ClusterConfig()
                    {
                        Destinations = new  Dictionary<string, DestinationConfig>
                        {
                            ["service"] = new DestinationConfig()
                            {
                                Address = $"http://{path.Backend.Service.Name}.{ns}.svc.cluster.local:{path.Backend.Service.Port.Number}"
                            }
                        },
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


        public void AddIngressDefinition(V1Ingress ingress)
        {
            var ns = string.IsNullOrEmpty(ingress.Namespace()) ? "default" : ingress.Namespace();
            var ingressConfig = new IngressConfig().FillDataFrom(ingress);
            _ingresses.Add($"{ingress.Name()}:{ns}",  ingressConfig);
            _clusterConfigs.AddRange(ingressConfig.ClusterConfigs);
            _routes.AddRange(ingressConfig.RouteConfigs);
            HasPendingChanges = true;
        }

        public IProxyConfig CurrentSnapshot() => _currentSnapshot;
    }
}
