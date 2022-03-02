namespace YarpIngress
{
    public enum AffinityMode
    {
        Service = 0,
        ServiceCusterIP = 1,
        RandomPod = 2,
        RoundRobin = 3,
        Cookie = 4
    }

    public static class AffinityModeExtensions
    {
        public static bool RequireEndpoints(this AffinityMode mode) => mode != AffinityMode.Service || mode != AffinityMode.ServiceCusterIP;
    }
}


