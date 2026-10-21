using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.SqlTasks;

public enum ReferenceQueryEditRestriction
{
    Published,
    Archived,
    HasAttempts
}

public static class ReferenceQueryEditPolicy
{
    public static ReferenceQueryEditRestriction? GetRestriction(
        PublicationStatus publicationStatus,
        bool hasAttempts)
    {
        if (publicationStatus == PublicationStatus.Published)
        {
            return ReferenceQueryEditRestriction.Published;
        }

        if (publicationStatus == PublicationStatus.Archived)
        {
            return ReferenceQueryEditRestriction.Archived;
        }

        return hasAttempts ? ReferenceQueryEditRestriction.HasAttempts : null;
    }

    public static string? GetUserMessage(ReferenceQueryEditRestriction? restriction) => restriction switch
    {
        ReferenceQueryEditRestriction.Published => "Эталон опубликованного задания нельзя редактировать.",
        ReferenceQueryEditRestriction.Archived => "Эталон архивного задания нельзя редактировать.",
        ReferenceQueryEditRestriction.HasAttempts => "Эталон нельзя редактировать: у задания уже есть попытки.",
        _ => null
    };
}
