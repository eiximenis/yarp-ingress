using k8s;
using k8s.Models;
using System.Collections.Concurrent;
using YarpIngress.Extensions;
using YarpIngress.YarpIntegration;

namespace YarpIngress.K8sInt
{

   /// <summary>
   /// This class watches for global resources on ALL namespaces
   /// </summary>
    public class MainResourcesWatcher : BackgroundService
    {
        private readonly YarpConfiguration _yarpConfig;
        private readonly IKubernetes _client;
        private readonly ILogger _logger;
        private readonly YarpConfigurationProvider _configProvider;
        private readonly IngressWatcher _ingressWatcher;
        private readonly ServicesWatcher _servicesWatcher;
        private readonly EndpointsWatcher _endpointsWatcher;
        private readonly Reconciler _reconciler;

        public MainResourcesWatcher(ILoggerFactory loggerFactory, IKubernetesClientFactory k8sClientFactory, YarpConfigurationProvider configProvider)
        {
            _configProvider = configProvider;
            _logger = loggerFactory.CreateLogger<MainResourcesWatcher>();
            _yarpConfig = _configProvider.GetYarpConfiguration();
            _client = k8sClientFactory.GetClient();
            _ingressWatcher = new IngressWatcher(k8sClientFactory, loggerFactory);
            _servicesWatcher = new ServicesWatcher(k8sClientFactory, loggerFactory);
            _endpointsWatcher = new EndpointsWatcher(k8sClientFactory, loggerFactory);
            _reconciler = new Reconciler(loggerFactory, k8sClientFactory, configProvider);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ingressWatcher.OnIngressCreated += (_, ingress) => _reconciler.OnIngressCreated(ingress);
            _ingressWatcher.OnIngressUpdated += (_, ingress) => _reconciler.OnIngressUpdated(ingress);
            _ingressWatcher.OnIngressRemoved += (_, ingress) => _reconciler.OnIngressRemoved(ingress);

            _servicesWatcher.OnServiceCreated += (_, svc) => _reconciler.OnServiceCreated(svc);
            _servicesWatcher.OnServiceUpdated += (_, svc) => _reconciler.OnServiceUpdated(svc);
            _servicesWatcher.OnServiceRemoved += (_, svc) => _reconciler.OnServiceRemoved(svc);

            _endpointsWatcher.OnEndpointsCreated += (_, ep) => _reconciler.OnEndpointsCreated(ep);
            _endpointsWatcher.OnEndpointsUpdated += (_, ep) => _reconciler.OnEndpointsUpdated(ep);
            _endpointsWatcher.OnEndpointsRemoved += (_, ep) => _reconciler.OnEndpointsRemoved(ep);

            _ingressWatcher.Start();
            _servicesWatcher.Start();
            _endpointsWatcher.Start();

            while (true)
            {
                await Task.Delay(1500);
                _yarpConfig.TriggerConfigurationChangeIfNeeded();
            }
        }

    }
}
