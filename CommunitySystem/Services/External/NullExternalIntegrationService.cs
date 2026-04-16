namespace CommunitySystem.Services.External;

public class NullExternalIntegrationService : IExternalIntegrationService
{
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
