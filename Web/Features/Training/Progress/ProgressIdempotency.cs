using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Schema;

namespace SQLModule.Web.Features.Training.Progress;

internal static class ProgressIdempotency
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static string Hash(Guid taskId) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(taskId.ToString("D"))));

    internal static Result<StudentTaskProgressResponse> Replay(MutationReceipt receipt, string payloadHash)
    {
        if (receipt.PayloadHash != payloadHash)
        {
            return Result<StudentTaskProgressResponse>.Fail(ProgressErrors.IdempotencyPayloadMismatch);
        }

        var response = JsonSerializer.Deserialize<StudentTaskProgressResponse>(receipt.ResponseJson, JsonOptions);
        return response is null
            ? Result<StudentTaskProgressResponse>.Fail(Error.Conflict(
                "IdempotencyRequestInProgress",
                "Запрос с этим Idempotency-Key ещё выполняется."))
            : response;
    }

    internal static string Serialize(StudentTaskProgressResponse response) =>
        JsonSerializer.Serialize(response, JsonOptions);
}
