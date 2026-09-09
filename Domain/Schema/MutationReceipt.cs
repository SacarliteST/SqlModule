using SQLModule.Domain.Common;

namespace SQLModule.Domain.Schema;

public sealed class MutationReceipt : BaseEntity
{
    public string Scope { get; private set; }
    public string IdempotencyKey { get; private set; }
    public string PayloadHash { get; private set; }
    public string ResponseJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private MutationReceipt(
        Guid id, string scope, string idempotencyKey, string payloadHash,
        string responseJson, DateTimeOffset createdAt) : base(id)
    {
        Scope = scope;
        IdempotencyKey = idempotencyKey;
        PayloadHash = payloadHash;
        ResponseJson = responseJson;
        CreatedAt = createdAt;
    }

    public static MutationReceipt Create(
        string scope, string idempotencyKey, string payloadHash, string responseJson) =>
        new(Guid.NewGuid(), scope, idempotencyKey, payloadHash, responseJson, DateTimeOffset.UtcNow);

    public void Complete(string responseJson) => ResponseJson = responseJson;
}
