namespace Core.Events
{
    public record OrderPlacedEvent(int OrderId, int UserId, int GameId, decimal Price);
}
