namespace FlowCart.BuildingBlocks.Domain;

/// <summary>
/// Base class for entities requiring audit metadata tracking (creation, modification, soft delete).
/// </summary>
public abstract class AuditableEntity<TId> : Entity<TId>
    where TId : notnull
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; }

    protected AuditableEntity() { }

    protected AuditableEntity(TId id) : base(id) { }
}
