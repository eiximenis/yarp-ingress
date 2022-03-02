using k8s.Models;

namespace YarpIngress.K8sInt
{
    public interface IK8sObjectsDefinitionsProvider
    {
        V1Service? GetServiceDefinition(string key);
        V1Endpoints? GetEndpointsDefinition(string key);
    }
}
