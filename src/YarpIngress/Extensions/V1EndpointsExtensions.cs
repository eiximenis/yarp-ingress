using k8s.Models;

namespace YarpIngress.Extensions
{
    internal static class V1EndpointsExtensions
    {
        public static string SafeNamespace(this V1Endpoints endpoints)
        {
            var ns = endpoints.Namespace();
            return string.IsNullOrEmpty(ns) ? "default" : ns;
        }

        public static string Key(this V1Endpoints endpoints)
        {
            var ns = endpoints.SafeNamespace();
            return $"{endpoints.Name()}:{ns}";
        }
    }
}
