using Docker.DotNet.Models;

namespace SQLModule.Sandbox.Pooling;

internal static class SandboxContainerSecurity
{
    private const long BytesInMegabyte = 1024 * 1024;
    private const long NanoCpusInCpu = 1_000_000_000;

    internal static void Apply(
        CreateContainerParameters parameters,
        SandboxPoolResourceOptions resources)
    {
        parameters.HostConfig ??= new HostConfig();
        parameters.HostConfig.Memory = checked(resources.MemoryLimitMegabytes * BytesInMegabyte);
        parameters.HostConfig.NanoCPUs = checked((long)(resources.CpuLimit * NanoCpusInCpu));
        parameters.HostConfig.PidsLimit = resources.PidsLimit;
        parameters.HostConfig.Privileged = false;
        parameters.HostConfig.NetworkMode = "bridge";
        parameters.HostConfig.Binds = [];
        parameters.HostConfig.Mounts = [];
    }

    internal static bool IsImagePinned(string image)
    {
        var normalized = image.Trim();
        if (normalized.Contains("@sha256:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var lastSegment = normalized[(normalized.LastIndexOf('/') + 1)..];
        var separator = lastSegment.LastIndexOf(':');
        return separator > 0 &&
               !lastSegment[(separator + 1)..].Equals("latest", StringComparison.OrdinalIgnoreCase);
    }
}
