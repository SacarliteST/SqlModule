using SQLModule.Common.Results;

namespace SQLModule.Web.Features.DbmsCatalog.ValidationCapabilities;

internal static class ValidationCapabilitiesErrors
{
    internal static Error DbmsNotFound(Guid dbmsId) =>
        Error.NotFound("DbmsDictionary", dbmsId);

    internal static Error AnalyzerNotSupported(string dbmsSystemName) =>
        Error.Validation(
            "ValidationCapabilities.AnalyzerNotSupported",
            $"Проверка SQL для СУБД '{dbmsSystemName}' пока не поддерживается.");
}
