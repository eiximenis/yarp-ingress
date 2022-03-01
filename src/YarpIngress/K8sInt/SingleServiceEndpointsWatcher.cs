using k8s;
using k8s.Models;

namespace YarpIngress.K8sInt
{
    public class SingleServiceEndpointsWatcher
    {
        private readonly string _name;
        private readonly List<Action<V1Endpoints, string>> _subscribers;
        public SingleServiceEndpointsWatcher(string serviceName)
        {
            _name = serviceName;
            _subscribers = new List<Action<V1Endpoints, string>>();
        }

        public void Subscribe(Action<V1Endpoints, string> subscriber)
        {
            _subscribers.Add(subscriber); 
        }

        internal void OnEndpointEvent(WatchEventType type, V1Endpoints item)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber(item, _name);
            }
        }
    }
}