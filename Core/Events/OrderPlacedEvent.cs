namespace Core.Events {
    public record OrderPlacedEvent(
        int OrderId,
        int UserId,
        int GameId,
        string GameName,
        string UserEmail,
        string UserRole,
        string? UserName,
        decimal Price);
}