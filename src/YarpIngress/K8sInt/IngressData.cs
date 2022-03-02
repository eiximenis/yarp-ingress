using k8s.Models;
using YarpIngress.Extensions;

namespace YarpIngress.K8sInt
{
    public class IngressData
    {
        private readonly List<string> _affectedServices;
        private readonly List<string> _affectedEndpoints;
        public V1Ingress V1Ingress { get; }
        public IEnumerable<string> AffectedServices { get => _affectedServices;  }
        public IEnumerable<string> AffectedEndpoints { get => _affectedEndpoints; }

        public IngressData(V1Ingress v1Ingress)
        {
            V1Ingress = v1Ingress;
            _affectedEndpoints = new List<string>();
            _affectedServices = new List<string>();
            FillAffectedServcicesAndEndpoints();
        }    
        

        private  void FillAffectedServcicesAndEndpoints()
        {
            var ns = V1Ingress.SafeNamespace();
            var requireEndpoints = V1Ingress.YarpAffinity().RequireEndpoints();
            foreach (var rule in V1Ingress.Spec.Rules)
            {
                foreach (var path in rule.Http.Paths)
                {
                    var svc = path.Backend.Service?.Name;
                    if (!string.IsNullOrEmpty(svc))
                    {
                        var trackedsvc = $"{svc}:{ns}";
                        if (!_affectedServices.Contains(trackedsvc))
                        {
                            _affectedServices.Add(trackedsvc);
                        }
                        if (requireEndpoints && !_affectedEndpoints.Contains(trackedsvc))
                        {
                            _affectedEndpoints.Add(trackedsvc);
                        }
                    }
                }
            }
        }
    }
}
