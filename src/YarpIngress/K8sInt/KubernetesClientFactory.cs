using k8s;

namespace YarpIngress.K8sInt
{

    public class KubernetesClientFactory : IKubernetesClientFactory, IDisposable
    {
        private readonly Lazy<IKubernetes> _client;
        private bool _disposed;

        public KubernetesClientFactory()
        {
            _client = new Lazy<IKubernetes>(() =>
            {
                var k8sconfig = KubernetesClientConfiguration.IsInCluster() ? KubernetesClientConfiguration.InClusterConfig() : KubernetesClientConfiguration.BuildConfigFromConfigFile();
                return new Kubernetes(k8sconfig);
            });
            _disposed = false;
        }

        public IKubernetes GetClient() => _client.Value;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_client.IsValueCreated)
                    {
                        _client.Value.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
