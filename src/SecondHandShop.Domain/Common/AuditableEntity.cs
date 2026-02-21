namespace SecondHandShop.Domain.Common;

public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;
    public Guid? CreatedByAdminUserId { get; protected set; }
    public Guid? UpdatedByAdminUserId { get; protected set; }

    protected void SetCreatedAudit(Guid? adminUserId, DateTime utcNow)
    {
        CreatedByAdminUserId = adminUserId;
        UpdatedByAdminUserId = adminUserId;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    protected void Touch(Guid? adminUserId, DateTime utcNow)
    {
        UpdatedByAdminUserId = adminUserId;
        UpdatedAt = utcNow;
    }
}
