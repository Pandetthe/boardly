namespace Boardly.Backend.Services;

public interface IDbInitializator
{
    public Task InitAsync(CancellationToken cancellationToken = default);
}
