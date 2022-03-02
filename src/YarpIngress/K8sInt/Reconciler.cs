using k8s;
using k8s.Models;
using System.Collections.Concurrent;
using YarpIngress.Extensions;
using YarpIngress.YarpIntegration;

namespace YarpIngress.K8sInt
{
    public class Reconciler : IK8sObjectsDefinitionsProvider
    {
        private readonly ConcurrentDictionary<string, IngressData> _ingresses;
        private readonly ConcurrentDictionary<string, V1Service> _services;
        private readonly ConcurrentDictionary<string, V1Endpoints> _endpoints;
        private List<string> _trackedServices;
        private List<string> _trackedEndpoints;
        private readonly ILogger _logger;
        private readonly IKubernetes _client;
        private readonly YarpConfiguration _yarpConfiguration;

        public Reconciler(ILoggerFactory loggerFactory, IKubernetesClientFactory clientFactory, YarpConfigurationProvider configProvider)
        {
            _trackedServices = new List<string>(0);
            _ingresses = new ConcurrentDictionary<string, IngressData>();
            _services = new ConcurrentDictionary<string, V1Service>();
            _endpoints = new ConcurrentDictionary<string, V1Endpoints>();
            _trackedEndpoints = new List<string>(0);
            _logger = loggerFactory.CreateLogger<Reconciler>();
            _client = clientFactory.GetClient();
            _yarpConfiguration = configProvider.GetYarpConfiguration();
        }
            

        internal void OnIngressUpdated(V1Ingress ingress)
        {
            var key = ingress.Key();
            var ingressData = new IngressData(ingress);
            _ingresses.AddOrUpdate(key, ingressData, (_, _) => ingressData);
            _trackedServices = _ingresses.Values.SelectMany(id => id.AffectedServices).ToList();
            _trackedEndpoints = _ingresses.Values.SelectMany(id => id.AffectedEndpoints).ToList();
            ReloadServicesIfNeeded(ingressData);
            ReloadEndpointsIfNeeded(ingressData);
            _logger.LogInformation("Updated ingress {name}", ingress.Key());
            _logger.LogInformation("Tracked Services {services}", _trackedServices);
        }
        internal void OnIngressRemoved(V1Ingress ingress)
        {
            _ingresses.Remove(ingress.Key(), out var _);
            _trackedServices = _ingresses.Values.SelectMany(id => id.AffectedServices).ToList();
            _trackedEndpoints = _ingresses.Values.SelectMany(id => id.AffectedEndpoints).ToList();
            _logger.LogInformation("Removed ingress {name}", ingress.Key());
            _logger.LogInformation("Tracked Services {services}", _trackedServices);
        }

        internal void OnIngressCreated(V1Ingress ingress)
        {
            var ingressData = new IngressData(ingress);
            _ingresses.TryAdd(ingress.Key(), ingressData);
            _trackedServices = _ingresses.Values.SelectMany(id => id.AffectedServices).ToList();
            _trackedEndpoints = _ingresses.Values.SelectMany(id => id.AffectedEndpoints).ToList();
            ReloadServicesIfNeeded(ingressData);
            ReloadEndpointsIfNeeded(ingressData);
            _logger.LogInformation("Added ingress {name}", ingress.Key());
            _logger.LogInformation("Tracked Services {services}", _trackedServices);
            _yarpConfiguration.AddIngressDefinition(ingress, this);
        }

        private void ReloadEndpointsIfNeeded(IngressData data)
        {
            if (data.V1Ingress.YarpAffinity().RequireEndpoints())
            {
                var loadedEndpoints = _endpoints.Keys.ToList();
                foreach (var endpointKey in data.AffectedEndpoints)
                {
                    if (!loadedEndpoints.Contains(endpointKey))
                    {
                        var tokens = endpointKey.Split(':');
                        var name = tokens[0];
                        var ns = tokens[1];
                        _logger.LogInformation("Endpoints {name} not loaded. Loading them...", endpointKey);
                        var ep = _client.ReadNamespacedEndpoints(name, ns);
                        if (ep != null)
                        {
                            OnEndpointsCreated(ep);
                        }
                        else
                        {
                            _logger.LogInformation("Endpoints {name} do not found. Probably will be created later", endpointKey);
                        }
                    }
                }
            }

        }

        private void ReloadServicesIfNeeded(IngressData data)
        {
            var loadedServices = _services.Keys.ToList();
            foreach (var serviceKey in data.AffectedServices)
            {
                if (!loadedServices.Contains(serviceKey))
                {
                    var tokens = serviceKey.Split(':');
                    var name = tokens[0];

                    var ns = tokens[1];
                    _logger.LogInformation("Service {name} not loaded. Loading it...", serviceKey);
                    var svc = _client.ReadNamespacedService(name, ns);
                    if (svc != null)
                    {
                        OnServiceCreated(svc);
                    }
                    else
                    {
                        _logger.LogInformation("Service {name} do not found. Probably will be created later", serviceKey);
                    }
                }
            }


        }

        internal void OnEndpointsUpdated(V1Endpoints ep)
        {
            _logger.LogInformation("Endpoints updated {name}", ep.Key());
        }

        internal void OnEndpointsRemoved(V1Endpoints ep)
        {
            _logger.LogInformation("Endpoints removed {name}", ep.Key());
        }
        
        internal void OnEndpointsCreated(V1Endpoints ep)
        {
            if (EndpointsNeedsToBeTracked(ep))
            {
                _endpoints.TryAdd(ep.Key(), ep);
                _logger.LogInformation("Added Endpoints {name}", ep.Key());
            }
        }

        internal void OnServiceRemoved(V1Service svc)
        {
            _services.TryRemove(svc.Key(), out var _);
            _logger.LogInformation("Removed service {name}", svc.Key());
        }

        internal void OnServiceUpdated(V1Service svc)
        {
            if (ServiceNeedsToBeTracked(svc))
            {
                _services.AddOrUpdate(svc.Key(), svc, (_, _) => svc);
                _logger.LogInformation("Updated service {name}", svc.Key());
            }
        }

        internal void OnServiceCreated(V1Service svc)
        {
            if (ServiceNeedsToBeTracked(svc))
            {
                _services.TryAdd(svc.Key(), svc);
                _logger.LogInformation("Added service {name}", svc.Key());
            }
        }

        private bool ServiceNeedsToBeTracked(V1Service svc)
        {
            return _trackedServices.Contains(svc.Key());
        }

        private bool EndpointsNeedsToBeTracked(V1Endpoints ep)
        {
            return _trackedEndpoints.Contains(ep.Key());
        }

        V1Service? IK8sObjectsDefinitionsProvider.GetServiceDefinition(string key)
        {
            return _services.TryGetValue(key, out var svc) ? svc : null;
        }

        V1Endpoints? IK8sObjectsDefinitionsProvider.GetEndpointsDefinition(string key)
        {
            return _endpoints.TryGetValue(key, out var ep) ? ep : null;
        }
    }
}
