namespace SQLModule.Sandbox.Pooling;

internal sealed class EmptySandboxPoolProfileSource : ISandboxPoolProfileSource
{
    public Task<IReadOnlyCollection<SandboxDbmsSpec>> GetProfilesAsync(
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<SandboxDbmsSpec>>([]);
}
