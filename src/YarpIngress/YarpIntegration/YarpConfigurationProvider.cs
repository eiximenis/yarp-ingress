using Yarp.ReverseProxy.Configuration;

namespace YarpIngress.YarpIntegration
{
    public class YarpConfigurationProvider : IProxyConfigProvider
    {
        private readonly YarpConfiguration _yarpConfiguration;

        public YarpConfigurationProvider()
        {
            _yarpConfiguration = new YarpConfiguration();
        }

        public IProxyConfig GetConfig() => _yarpConfiguration.CurrentSnapshot();

        public YarpConfiguration GetYarpConfiguration() => _yarpConfiguration;
    }
}
