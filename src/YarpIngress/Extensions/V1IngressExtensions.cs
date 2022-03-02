using k8s.Models;

namespace YarpIngress.Extensions
{
    static class V1IngressExtensions
    {

        public static string SafeNamespace(this V1Ingress ingress)
        {
            var ns = ingress.Namespace();
            return string.IsNullOrEmpty(ns) ? "default" : ns;
        }

        public static string Key(this V1Ingress ingress)
        {
            var ns = ingress.SafeNamespace();
            return $"{ingress.Name()}:{ns}";
        }

        public static AffinityMode YarpAffinity(this V1Ingress ingress)
        {
            var affinity = AffinityMode.Service;
            var annotations = ingress.Annotations();
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
