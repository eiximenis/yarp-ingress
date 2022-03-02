using k8s.Models;

namespace YarpIngress.Extensions
{
    static class V1ServiceExtensions
    {
        public static string Key(this V1Service ingress)
        {
            var ns = ingress.Namespace();
            if (string.IsNullOrEmpty(ns)) { ns = "default"; }
            return $"{ingress.Name()}:{ns}";
        }

        public static string SafeNamespace(this V1Service ingress)
        {
            var ns = ingress.Namespace();
            return string.IsNullOrEmpty(ns) ? "default" : ns;
        }

        public static int? PortByName(this V1Service svc,  string portname) => svc.Spec.Ports.SingleOrDefault(p => p.Name == portname)?.Port;
    }
}
