namespace Unchained.Services.Background.Events
{
    public interface IEventHandler<in T> where T : class
    {
        Task HandleAsync(T eventData, CancellationToken cancellationToken = default);
    }
}
