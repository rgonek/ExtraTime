namespace ExtraTime.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
