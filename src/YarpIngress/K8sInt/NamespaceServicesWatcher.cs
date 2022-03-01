using k8s;
using k8s.Models;

namespace YarpIngress.K8sInt
{
    public class NamespaceServicesWatcher
    {
        private readonly string _namespace;
        private readonly IMainResourcesWatcher _owner;
        private readonly IKubernetes _client;
        private readonly Dictionary<string, V1Service> _services;
        public NamespaceServicesWatcher(string ns, IMainResourcesWatcher owner)
        {
            _namespace = string.IsNullOrEmpty(ns) ? "default" : ns;
            _owner = owner;
            _client = owner.Client;
            _services = new Dictionary<string, V1Service>();
        }

        public Task Start()
        {
            var servicesTask = _client.ListNamespacedServiceWithHttpMessagesAsync(_namespace);
            servicesTask.Watch<V1Service, V1ServiceList>(OnServiceEvent);
            return Task.CompletedTask;
        }

        private void OnServiceEvent(WatchEventType type, V1Service item)
        {
            switch (type)
            {
                case WatchEventType.Added:
                    OnAddedService(item);
                    break;
                case WatchEventType.Deleted:
                    OnDeletedService(item);
                    break;
            }
        }

        private void OnAddedService(V1Service item)
        {
            _services.Add(item.Name(), item);
        }

        private void OnDeletedService(V1Service item)
        {
            var key = item.Name();
            if (_services.ContainsKey(key))
            {
                _services.Remove(key);
            }
        }
    }
}
