using AccessControl.Infrastructure;
using AccessControl.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAccessControlInfrastructure(builder.Configuration);
builder.Services.AddHostedService<MqttCardReadWorker>();

var host = builder.Build();
host.Run();
