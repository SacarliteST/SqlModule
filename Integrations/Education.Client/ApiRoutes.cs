namespace SQLModule.Education.Client;

internal static class ApiRoutes
{
    internal const string PrefixV1 = "api/v1";

    internal static class ModuleSessions
    {
        internal const string Collection = PrefixV1 + "/module-sessions";
        internal const string Complete = Collection + "/{sessionId}/complete";

        internal static string ForComplete(Guid sessionId) =>
            $"{Collection}/{sessionId:D}/complete";
    }
}
