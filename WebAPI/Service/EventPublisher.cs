using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RabbitMQ.Client;

namespace WebAPI.Service {
    public class EventPublisher(IConnection connection) {
        public static readonly JsonSerializerOptions JsonOptions = new() {
            Converters = { new JsonStringEnumConverter() }
        };

        public async Task PublishAsync<T>(string queue, T message) {
            await using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));
            await channel.BasicPublishAsync(exchange: "", routingKey: queue, body: body);
        }
    }
}