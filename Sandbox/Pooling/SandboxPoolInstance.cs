namespace SQLModule.Sandbox.Pooling;

internal sealed class SandboxPoolInstance
{
    internal string Id { get; } = Guid.NewGuid().ToString("N");
}
