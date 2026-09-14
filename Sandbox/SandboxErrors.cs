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

    internal static Error UnpinnedPoolImage() =>
        Error.Failure("Sandbox.UnpinnedPoolImage", "Sandbox pool image must use an exact tag or digest.");

    internal static Error PoolIsStopping() =>
        Error.Unavailable("Sandbox.PoolIsStopping", "Sandbox pool is stopping and does not accept new leases.");

    internal static Error PoolProfileNotConfigured(string systemName) =>
        Error.Unavailable(
            "Sandbox.PoolProfileNotConfigured",
            $"Sandbox pool profile for DBMS '{systemName}' is not configured.");

    internal static Error IsolationPreparationFailed() =>
        Error.Unavailable("Sandbox.IsolationPreparationFailed", "Sandbox isolation could not be prepared.");

    internal static Error PreparationTimeout() =>
        Error.Unavailable("Sandbox.PreparationTimeout", "Sandbox preparation exceeded the configured timeout.");

    internal static Error IsolationCleanupFailed() =>
        Error.Unavailable("Sandbox.IsolationCleanupFailed", "Sandbox isolation could not be cleaned up safely.");
}
