using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Client.Attempt;

internal sealed class AttemptClient(HttpClient httpClient)
    : CrudClientBase<CreateAttemptRequest, UpdateAttemptRequest, AttemptResponse>(httpClient), IAttemptClient
{
    protected override string Collection => ApiRoutes.Training.Attempts.Collection;
    protected override string ForId(Guid id) => ApiRoutes.Training.Attempts.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.Training.Attempts.ForPagination(offset, limit);
}
