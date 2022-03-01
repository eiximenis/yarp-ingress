using k8s;
using k8s.Models;
using System.Collections.Concurrent;

namespace YarpIngress.K8sInt
{
    public class NamespaceServiceEndpointsWatcher
    {
        private readonly IMainResourcesWatcher _owner;
        private readonly IKubernetes _client;
        private readonly string _ns;
        private Dictionary<string, SingleServiceEndpointsWatcher> _monitoredEndpoints;

        public NamespaceServiceEndpointsWatcher(string ns, IMainResourcesWatcher owner)
        {
            _owner = owner;
            _client = owner.Client;
            _monitoredEndpoints = new Dictionary<string, SingleServiceEndpointsWatcher>();
            _ns = string.IsNullOrEmpty(ns) ? "default" : ns;
        }

        public Task Start()
        {
            var servicesRespTask = _client.ListNamespacedEndpointsWithHttpMessagesAsync(_ns, watch: true);
            servicesRespTask.Watch<V1Endpoints, V1EndpointsList>(OnEndpointEvent);
            return Task.CompletedTask;
        }

        private void OnEndpointEvent(WatchEventType type, V1Endpoints item)
        {
            var name = item.Name();
            if (_monitoredEndpoints.ContainsKey(name))
            {
                _monitoredEndpoints[name].OnEndpointEvent(type, item);
            }
        }


        internal SingleServiceEndpointsWatcher GetOrAddWatcherForServiceEndpoints(string name)
        {
            if (_monitoredEndpoints.ContainsKey(name))
            {
                return _monitoredEndpoints[name];
            }
            var serviceWatcher = new SingleServiceEndpointsWatcher(name);
            _monitoredEndpoints.Add(name, serviceWatcher);
            return serviceWatcher;
        }
    }
}