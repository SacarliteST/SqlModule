using SQLModule.Common.Results;

namespace SQLModule.Sandbox;

internal static class SandboxErrors
{
    internal static Error UnsupportedDbms(string systemName) =>
        Error.Failure("Sandbox.UnsupportedDbms", $"DBMS '{systemName}' is not supported by the sandbox.");

    internal static Error ContainerFailed(string message) =>
        Error.Failure("Sandbox.ContainerFailed", message);

    internal static Error SetupFailed(string message) =>
        Error.Validation("Sandbox.SetupFailed", message);
}
