using MediaMinder.Common;
using MediaMinder.Service;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

// 配置Windows服务
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "MediaMinder";
});

// 强类型配置绑定
builder.Services.Configure<ServiceSettings>(builder.Configuration.GetSection("ServiceSettings"));

// 注册服务
builder.Services.AddHostedService<PlatformService>();
builder.Services.AddSingleton<CameraService>();
builder.Services.AddSingleton<PhotoDisplayService>();
builder.Services.AddSingleton<ICommunicationService, NamedPipeCommunicationService>();

var host = builder.Build();

try
{
    Log.Information("启动 MediaMinder 服务");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "服务启动失败");
}
finally
{
    Log.CloseAndFlush();
}
