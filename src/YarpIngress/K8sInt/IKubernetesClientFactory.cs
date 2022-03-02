using k8s;

namespace YarpIngress.K8sInt
{
    public interface IKubernetesClientFactory
    {
        IKubernetes GetClient();
    }
}