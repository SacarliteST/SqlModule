using SQLModule.Common.Results;

namespace SQLModule.Application.DbmsCatalog.ValidationCapabilities;

public static class ValidationCapabilitiesErrors
{
    public static Error DbmsNotFound(Guid dbmsId) =>
        Error.NotFound("DbmsDictionary", dbmsId);

    public static Error AnalyzerNotSupported(string dbmsSystemName) =>
        Error.Validation(
            "ValidationCapabilities.AnalyzerNotSupported",
            $"Проверка SQL для СУБД '{dbmsSystemName}' пока не поддерживается.");
}
