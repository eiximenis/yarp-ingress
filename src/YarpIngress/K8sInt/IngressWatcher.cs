using k8s;
using k8s.Models;
using YarpIngress.Extensions;

namespace YarpIngress.K8sInt
{

    public class IngressWatcher
    {
        private readonly ILogger _logger;
        private readonly IKubernetes _client;
        private Watcher<V1Ingress>? _watcher;

        public event EventHandler<V1Ingress> OnIngressCreated;
        public event EventHandler<V1Ingress> OnIngressRemoved;
        public event EventHandler<V1Ingress> OnIngressUpdated;

        public IngressWatcher(IKubernetesClientFactory clientFactory, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<IngressWatcher>();
            _client = clientFactory.GetClient();
        }

        public void Start()
        {
            var ingressRespTask = _client.ListIngressForAllNamespacesWithHttpMessagesAsync(watch: true);
            _watcher = ingressRespTask.Watch<V1Ingress, V1IngressList>(OnIngressWatch, OnIngressWatchError, OnConnectionClosed);
        }

        private void OnConnectionClosed()
        {
            _logger.LogError("Connection closed by server");
        }

        private void OnIngressWatchError(Exception error)
        {
            _logger.LogError(error, "Error while watching ingress resources");
        }

        private void OnIngressWatch(WatchEventType type, V1Ingress ingress)
        {
            _logger.LogInformation("Ingress watch event {eventType} on ingress {name}", type, ingress.Name());

            if (!ingress.IsForMe())
            {
                _logger.LogInformation("Ignored ingress {name} because its not for yarp ingress controller", ingress.Name());
                return;
            }

            switch (type)
            {
                case WatchEventType.Added:
                    OnIngressCreated?.Invoke(this, ingress);
                    break;
                   
                case WatchEventType.Deleted:
                    OnIngressRemoved?.Invoke(this, ingress);
                    break;
                case WatchEventType.Modified:
                    OnIngressUpdated?.Invoke(this, ingress);
                    break;
            }
        }
    }
}
