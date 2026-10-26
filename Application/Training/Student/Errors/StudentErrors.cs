using SQLModule.Common.Results;

namespace SQLModule.Application.Training.Student;

/// <summary>Ошибки студенческих запросов; закрытые ресурсы скрываются пустым 404.</summary>
public static class StudentErrors
{
    /// <summary>Ресурс отсутствует или недоступен текущему студенту.</summary>
    public static Error NotFound { get; } = new("Student.NotFound", "Ресурс не найден.", ErrorType.NotFound);
}
