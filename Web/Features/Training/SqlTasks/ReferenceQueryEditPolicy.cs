using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal enum ReferenceQueryEditRestriction
{
    Published,
    Archived,
    HasAttempts
}

internal static class ReferenceQueryEditPolicy
{
    internal static ReferenceQueryEditRestriction? GetRestriction(
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

    internal static string? GetUserMessage(ReferenceQueryEditRestriction? restriction) => restriction switch
    {
        ReferenceQueryEditRestriction.Published => "Эталон опубликованного задания нельзя редактировать.",
        ReferenceQueryEditRestriction.Archived => "Эталон архивного задания нельзя редактировать.",
        ReferenceQueryEditRestriction.HasAttempts => "Эталон нельзя редактировать: у задания уже есть попытки.",
        _ => null
    };
}
