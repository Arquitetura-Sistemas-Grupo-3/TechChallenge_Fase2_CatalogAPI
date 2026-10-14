using Core.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using WebAPI.Interface;

namespace WebAPI.Service {
    public class PaymentProcessedConsumer(
        IConnection connection,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<PaymentProcessedConsumer> logger) : BackgroundService {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var queue = configuration["RabbitMQ:PaymentProcessedQueue"] ?? "catalog-payment-processed";

            var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) => {
                try {
                    var evt = JsonSerializer.Deserialize<PaymentProcessedEvent>(
                        Encoding.UTF8.GetString(ea.Body.ToArray()), EventPublisher.JsonOptions);

                    if (evt is not null) {
                        using var scope = scopeFactory.CreateScope();
                        var compraService = scope.ServiceProvider.GetRequiredService<ICompraService>();
                        await compraService.ProcessarPagamento(evt);
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Erro ao processar PaymentProcessedEvent");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };

            await channel.BasicConsumeAsync(queue, autoAck: false, consumer, stoppingToken);
        }
    }
}
