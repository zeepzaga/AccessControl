using System.Text;
using System.Text.Json;
using AccessControl.Application.Access;
using AccessControl.Infrastructure.Data;
using AccessControl.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;

namespace AccessControl.Worker;

public class MqttCardReadWorker : BackgroundService
{
    private readonly ILogger<MqttCardReadWorker> _logger;
    private readonly IAccessDecisionService _decisionService;
    private readonly IOptions<DeviceIntegrationOptions> _options;
    private readonly AccessControlDbContext _db;
    private IMqttClient? _client;

    public MqttCardReadWorker(
        ILogger<MqttCardReadWorker> logger,
        IAccessDecisionService decisionService,
        IOptions<DeviceIntegrationOptions> options,
        AccessControlDbContext db)
    {
        _logger = logger;
        _decisionService = decisionService;
        _options = options;
        _db = db;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!string.Equals(_options.Value.Mode, "Mqtt", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Device integration mode is {Mode}. MQTT worker is idle.", _options.Value.Mode);
            return;
        }

        var mqttOptions = _options.Value.Mqtt;
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        _client.ApplicationMessageReceivedAsync += async args =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
                var message = JsonSerializer.Deserialize<CardReadMessage>(payload, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (message is null || string.IsNullOrWhiteSpace(message.CardUid))
                {
                    return;
                }

                if (message.AccessPointId is null && message.DeviceId is null)
                {
                    _logger.LogWarning("MQTT message missing accessPointId or deviceId");
                    return;
                }

                var accessPointId = message.AccessPointId;
                if (accessPointId is null && message.DeviceId is not null)
                {
                    var device = await _db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == message.DeviceId, stoppingToken);
                    if (device is null || device.AccessPointId is null)
                    {
                        _logger.LogWarning("Device not found or has no access point: {DeviceId}", message.DeviceId);
                        return;
                    }

                    accessPointId = device.AccessPointId;
                }

                var faceImage = string.IsNullOrWhiteSpace(message.FaceImageBase64)
                    ? null
                    : Convert.FromBase64String(message.FaceImageBase64);

                var request = new CardReadRequest(
                    message.CardUid,
                    accessPointId,
                    message.DeviceId,
                    faceImage,
                    DateTime.UtcNow);

                var decision = await _decisionService.ProcessCardReadAsync(request, stoppingToken);

                var response = new CardReadResponse
                {
                    CardUid = message.CardUid,
                    DeviceId = message.DeviceId,
                    AccessPointId = accessPointId,
                    Granted = decision.Granted,
                    Reason = decision.Reason.ToString(),
                    EmployeeId = decision.EmployeeId,
                    EmployeeName = decision.EmployeeName
                };

                var responseTopic = BuildResponseTopic(mqttOptions.TopicPublishPrefix, message.DeviceId);
                var responsePayload = JsonSerializer.Serialize(response);

                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(responseTopic)
                    .WithPayload(responsePayload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                if (_client?.IsConnected == true)
                {
                    await _client.PublishAsync(mqttMessage, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process MQTT message");
            }
        };

        var connectOptions = new MqttClientOptionsBuilder()
            .WithClientId(mqttOptions.ClientId)
            .WithTcpServer(mqttOptions.Host, mqttOptions.Port)
            .Build();

        await _client.ConnectAsync(connectOptions, stoppingToken);
        await _client.SubscribeAsync(mqttOptions.TopicSubscribe, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, stoppingToken);

        _logger.LogInformation("MQTT worker subscribed to {Topic}", mqttOptions.TopicSubscribe);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private static string BuildResponseTopic(string prefix, Guid? deviceId)
    {
        return deviceId.HasValue ? $"{prefix}/{deviceId}" : $"{prefix}/unknown";
    }

    private sealed class CardReadMessage
    {
        public string CardUid { get; set; } = string.Empty;
        public Guid? DeviceId { get; set; }
        public Guid? AccessPointId { get; set; }
        public string? FaceImageBase64 { get; set; }
    }

    private sealed class CardReadResponse
    {
        public string CardUid { get; set; } = string.Empty;
        public Guid? DeviceId { get; set; }
        public Guid? AccessPointId { get; set; }
        public bool Granted { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
    }
}
