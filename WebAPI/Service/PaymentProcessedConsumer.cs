using Core.Events;
using Core.Messaging;
using Infra.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using WebAPI.Interface;

namespace WebAPI.Service
{
    public class PaymentProcessedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitMqSettings _settings;
        private readonly ILogger<PaymentProcessedConsumer> _logger;

        public PaymentProcessedConsumer(
            IServiceScopeFactory scopeFactory,
            RabbitMqSettings settings,
            ILogger<PaymentProcessedConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = settings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var connection = _settings.CreateFactory().CreateConnection();
                    using var channel = connection.CreateModel();
                    channel.QueueDeclare(Queues.PaymentProcessed, durable: true, exclusive: false, autoDelete: false);
                    channel.BasicQos(0, 1, false);

                    var consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.Received += async (_, ea) =>
                    {
                        try
                        {
                            var evento = JsonSerializer.Deserialize<PaymentProcessedEvent>(
                                Encoding.UTF8.GetString(ea.Body.ToArray()));

                            if (evento is not null)
                            {
                                using var scope = _scopeFactory.CreateScope();
                                var service = scope.ServiceProvider.GetRequiredService<ICompraService>();
                                await service.ProcessarPagamento(evento);
                            }

                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao processar PaymentProcessedEvent");
                            channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                    };

                    channel.BasicConsume(Queues.PaymentProcessed, false, consumer);
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RabbitMQ indisponível. Nova tentativa em 5s");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
    }
}
