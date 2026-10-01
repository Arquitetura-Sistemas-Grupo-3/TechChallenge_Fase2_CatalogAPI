using Core.Messaging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Infra.Messaging
{
    public class RabbitMqSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string User { get; set; } = "guest";
        public string Password { get; set; } = "guest";

        public ConnectionFactory CreateFactory() => new()
        {
            HostName = Host,
            Port = Port,
            UserName = User,
            Password = Password,
            DispatchConsumersAsync = true
        };
    }

    public class RabbitMqPublisher : IEventPublisher, IDisposable
    {
        private readonly RabbitMqSettings _settings;
        private readonly object _lock = new();
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMqPublisher(RabbitMqSettings settings)
        {
            _settings = settings;
        }

        public void Publish<T>(string queue, T evento)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento));

            lock (_lock)
            {
                if (_channel is null || !_channel.IsOpen)
                {
                    _connection?.Dispose();
                    _connection = _settings.CreateFactory().CreateConnection();
                    _channel = _connection.CreateModel();
                }

                _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
                var props = _channel.CreateBasicProperties();
                props.Persistent = true;
                _channel.BasicPublish(string.Empty, queue, props, body);
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
