using k8s.Models;

namespace YarpIngress.Extensions
{
    static class V1IngressExtensions
    {
        public static AffinityMode YarpAffinity(this V1Ingress ingres)
        {
            var affinity = AffinityMode.Service;
            var annotations = ingres.Annotations();
            if (annotations.ContainsKey(YarpIngressAnnotations.Affinity))
            {
                affinity = Enum.TryParse<AffinityMode>(annotations[YarpIngressAnnotations.Affinity], out var annotatedAffinity) ? annotatedAffinity : AffinityMode.Service;
            }

            return affinity;
        }

        public static bool IsForMe(this V1Ingress ingress)
        {
            var annotations = ingress.Annotations();
            return annotations.ContainsKey(YarpIngressAnnotations.IngressClass) && annotations[YarpIngressAnnotations.IngressClass] == "yarp";
        }
    }
}
