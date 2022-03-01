using k8s;
using YarpIngress.YarpIntegration;

namespace YarpIngress.K8sInt
{
    public interface IMainResourcesWatcher
    {
        IKubernetes Client { get; }

        Task<SingleServiceEndpointsWatcher> GetOrAddWatcherForServiceEndpoints(string ns, string name);

        YarpConfiguration YarpConfig { get; }
    }
}