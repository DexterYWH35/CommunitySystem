namespace CommunitySystem.Services.External;

public interface IExternalIntegrationService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
