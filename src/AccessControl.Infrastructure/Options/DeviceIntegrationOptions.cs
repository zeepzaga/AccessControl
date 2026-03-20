namespace AccessControl.Infrastructure.Options;

public class DeviceIntegrationOptions
{
    public const string SectionName = "DeviceIntegration";

    public string Mode { get; set; } = "Http";
    public MqttOptions Mqtt { get; set; } = new();
}

public class MqttOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = "accesscontrol";
    public string TopicSubscribe { get; set; } = "accesscontrol/cards/read";
    public string TopicPublishPrefix { get; set; } = "accesscontrol/cards/response";
}
