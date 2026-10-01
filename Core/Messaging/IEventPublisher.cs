namespace Core.Messaging
{
    public static class Queues
    {
        public const string OrderPlaced = "order-placed";
        public const string PaymentProcessed = "payment-processed";
    }

    public interface IEventPublisher
    {
        void Publish<T>(string queue, T evento);
    }
}
