namespace YarpIngress
{
    public enum AffinityMode
    {
        Service = 0,
        RandomPod = 1,
        RoundRobin = 2,
        Cookie = 3
    }

    public static class AffinityModeExtensions
    {
        public static bool RequireEndpoints(this AffinityMode mode) => mode != AffinityMode.Service;
    }
}


