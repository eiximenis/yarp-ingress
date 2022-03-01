using k8s;
using k8s.Models;
using YarpIngress.Extensions;

namespace YarpIngress.K8sInt
{
    public class IngressProcessor
    {
        private readonly V1Ingress _ingress;
        private readonly IKubernetes _client;
        private readonly IMainResourcesWatcher _owner;
        private readonly string _ns;

        public IngressProcessor(V1Ingress ingress, IMainResourcesWatcher owner)
        {
            _ingress = ingress;
            _owner = owner;
            _client = owner.Client;
            _ns = string.IsNullOrEmpty(ingress.Namespace()) ? "default" : ingress.Namespace();
        }

        public async Task Start()
        {

            var yarpConfig = _owner.YarpConfig;
            yarpConfig.AddIngressDefinition(_ingress);
            var affinity = _ingress.YarpAffinity();
            if (affinity.RequireEndpoints())
            {
                foreach (var rule in _ingress.Spec.Rules)
                {
                    foreach (var path in rule.Http.Paths)
                    {
                        if (path.Backend.Service is not null)
                        {
                            var watcher = await _owner.GetOrAddWatcherForServiceEndpoints(_ns, path.Backend.Service.Name);
                            watcher.Subscribe(OnServiceEndpointEvent);
                        }
                    }
                }
            }
        }

        private void OnServiceEndpointEvent(V1Endpoints endpoints, string service)
        {
            int i = 0;
        }
    }
}
