using k8s;
using k8s.Models;

namespace YarpIngress.K8sInt
{
    public class ServicesWatcher
    {
        private readonly ILogger _logger;
        private readonly IKubernetes _client;
        private Watcher<V1Service>? _watcher;

        public event EventHandler<V1Service>? OnServiceCreated;
        public event EventHandler<V1Service>? OnServiceRemoved;
        public event EventHandler<V1Service>? OnServiceUpdated;

        public ServicesWatcher(IKubernetesClientFactory clientFactory, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ServicesWatcher>();
            _client = clientFactory.GetClient();
        }

        public void Start()
        {
            var servicesRespTask = _client.ListServiceForAllNamespacesWithHttpMessagesAsync(watch: true);
            _watcher = servicesRespTask.Watch<V1Service, V1ServiceList>(OnServiceWatch, OnServiceWatchError, OnConnectionClosed);
        }

        private void OnConnectionClosed()
        {
            _logger.LogError("Connection closed by server");
        }

        private void OnServiceWatchError(Exception error)
        {
            _logger.LogError(error, "Error while watching ingress resources");
        }

        private void OnServiceWatch(WatchEventType type, V1Service service)
        {
            switch (type)
            {
                case WatchEventType.Added:
                    OnServiceCreated?.Invoke(this, service);
                    break;

                case WatchEventType.Deleted:
                    OnServiceRemoved?.Invoke(this, service);
                    break;
                case WatchEventType.Modified:
                    OnServiceUpdated?.Invoke(this, service);
                    break;
            }
        }
    }
}