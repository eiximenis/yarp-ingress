using Yarp.ReverseProxy.Configuration;
using YarpIngress.K8sInt;
using YarpIngress.YarpIntegration;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSingleton(new YarpConfigurationProvider());
builder.Services.AddSingleton<IProxyConfigProvider>(sp => sp.GetRequiredService<YarpConfigurationProvider>());
builder.Services.AddLogging();
builder.Services.AddHostedService<MainResourcesWatcher>();
builder.Services.AddReverseProxy();


var app = builder.Build();

app.MapGet("/$config", (IProxyConfigProvider cfg) => cfg.GetConfig());

app.UseRouting();
// Register the reverse proxy routes
app.UseEndpoints(endpoints =>
{
    endpoints.MapReverseProxy();
});

app.Run();
