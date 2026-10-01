namespace Core.Events
{
    public enum PaymentStatus
    {
        Approved = 1,
        Rejected = 2
    }

    public record PaymentProcessedEvent(int OrderId, int UserId, int GameId, PaymentStatus Status);
}
