using k8s;
using k8s.Models;

namespace YarpIngress.K8sInt
{
    internal class EndpointsWatcher
    {
        private readonly IKubernetes _client;
        private readonly ILogger _logger;


        public event EventHandler<V1Endpoints>? OnEndpointsCreated;
        public event EventHandler<V1Endpoints>? OnEndpointsRemoved;
        public event EventHandler<V1Endpoints>? OnEndpointsUpdated;


        public EndpointsWatcher(IKubernetesClientFactory k8sClientFactory, ILoggerFactory loggerFactory)
        {
            _client = k8sClientFactory.GetClient();
            _logger = loggerFactory.CreateLogger<EndpointsWatcher>();
        }

        public void Start()
        {
            var servicesRespTask = _client.ListEndpointsForAllNamespacesWithHttpMessagesAsync(watch: true);
            servicesRespTask.Watch<V1Endpoints, V1EndpointsList>(OnEndpointWatch, OnError, OnClosed);
        }

        private void OnClosed()
        {
            _logger.LogError("Connection closed by server");
        }

        private void OnError(Exception error)
        {
            _logger.LogError(error, "Error while watching endpoints resources");
        }

        private void OnEndpointWatch(WatchEventType type, V1Endpoints endpoints)
        {
            switch (type)
            {
                case WatchEventType.Added:
                    OnEndpointsCreated?.Invoke(this, endpoints);
                    break;

                case WatchEventType.Deleted:
                    OnEndpointsRemoved?.Invoke(this, endpoints);
                    break;
                case WatchEventType.Modified:
                    OnEndpointsUpdated?.Invoke(this, endpoints);
                    break;
            }
        }
    }
}