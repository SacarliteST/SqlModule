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

    internal static Error PoolAcquireTimeout() =>
        Error.Unavailable("Sandbox.PoolAcquireTimeout", "Sandbox worker is not available within the configured timeout.");

    internal static Error PoolProfileConflict() =>
        Error.Failure("Sandbox.PoolProfileConflict", "Conflicting limits were supplied for the same sandbox profile.");

    internal static Error PoolWorkerCreationFailed() =>
        Error.Unavailable("Sandbox.PoolWorkerCreationFailed", "Sandbox worker could not be created.");
}
