using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Training.Progress;

public static class ProgressIdempotency
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Hash(Guid taskId) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(taskId.ToString("D"))));

    public static Result<StudentTaskProgressResponse> Replay(MutationReceipt receipt, string payloadHash)
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

    public static string Serialize(StudentTaskProgressResponse response) =>
        JsonSerializer.Serialize(response, JsonOptions);
}
