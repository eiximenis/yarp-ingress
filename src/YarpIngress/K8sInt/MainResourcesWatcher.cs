using k8s;
using k8s.Models;
using System.Collections.Concurrent;
using YarpIngress.Extensions;
using YarpIngress.YarpIntegration;

namespace YarpIngress.K8sInt
{
    public class MainResourcesWatcher : BackgroundService, IMainResourcesWatcher, IDisposable
    {
        public YarpConfiguration YarpConfig { get; }
        public IKubernetes Client { get; }
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, NamespaceServiceEndpointsWatcher> _endpointsWatcher;
        private readonly Dictionary<string, IngressProcessor> _ingressWatchers;

        private readonly ConcurrentDictionary<string, NamespaceServicesWatcher> _serviceWatchers;
        private readonly YarpConfigurationProvider _configProvider;

        public MainResourcesWatcher(ILogger<MainResourcesWatcher> logger, YarpConfigurationProvider configProvider)
        {
            _configProvider = configProvider;
            var k8sconfig = KubernetesClientConfiguration.IsInCluster() ? KubernetesClientConfiguration.InClusterConfig() : KubernetesClientConfiguration.BuildConfigFromConfigFile();             
            Client = new Kubernetes(k8sconfig);
            YarpConfig = _configProvider.GetYarpConfiguration();
            _logger = logger;
            _ingressWatchers = new Dictionary<string, IngressProcessor>();
            _endpointsWatcher = new ConcurrentDictionary<string, NamespaceServiceEndpointsWatcher>();
            _serviceWatchers = new ConcurrentDictionary<string, NamespaceServicesWatcher>();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var ingressRespTask = Client.ListIngressForAllNamespacesWithHttpMessagesAsync(watch: true);
            ingressRespTask.Watch<V1Ingress, V1IngressList>(OnIngressWatch);

            while (true)
            {
                await Task.Delay(1500);
                YarpConfig.TriggerConfigurationChangeIfNeeded();
            }
        }

        private void OnIngressWatch(WatchEventType type, V1Ingress item)
        {
            _logger.LogInformation($"Ingress watch event {type} on ingress {item.Name()}");

            if (!item.IsForMe())
            {
                _logger.LogInformation($"Ignored ingress {item.Name()} because its not for yarp ingress controller");
                return;
            }

            var name = item.Metadata.Name;
            var ns = item.Metadata.NamespaceProperty;
            if (string.IsNullOrEmpty(ns)) { ns = "default"; }
            var key = $"{name}:{ns}";
            switch (type)
            {
                case WatchEventType.Added:
                    {
                        var watcher = new IngressProcessor(item, this);
                        _ingressWatchers.Add(key, watcher);
                        _logger.LogInformation($"Starting ingress watcher for {key}");
                        watcher.Start().ContinueWith(t =>
                        {
                            _logger.LogInformation($"Started ingress watcher for {key}");
                        });
                        break;
                    }
                case WatchEventType.Deleted:
                    {
                        if (_ingressWatchers.ContainsKey(key))
                        {
                            _ingressWatchers.Remove(key);
                        }
                        break;
                    }
            }
        }


        private void OnServicesWatch(WatchEventType type, V1Service item)
        {
            _logger.LogInformation($"Service watch event {type} on service {item.Name()}");
        }



        public override void Dispose()
        {
            Client.Dispose();
        }



        async Task<SingleServiceEndpointsWatcher> IMainResourcesWatcher.GetOrAddWatcherForServiceEndpoints(string ns, string name)
        {
            var nsWatcher = _endpointsWatcher.GetValueOrDefault(ns);
            if (nsWatcher is not null)
            {
                return nsWatcher.GetOrAddWatcherForServiceEndpoints(name);
            }
            return await AddWatcherForServiceEndpoints(ns, name);
        }

        private async Task<SingleServiceEndpointsWatcher> AddWatcherForServiceEndpoints(string ns, string name)
        {
            _logger.LogInformation($"Adding watcher for endpoints of namespace {ns}");

            var nsWatcher = new NamespaceServiceEndpointsWatcher(ns, this);
            var singleWatcher = nsWatcher.GetOrAddWatcherForServiceEndpoints(name);
            await nsWatcher.Start();
            return singleWatcher;
        }
    }
}
