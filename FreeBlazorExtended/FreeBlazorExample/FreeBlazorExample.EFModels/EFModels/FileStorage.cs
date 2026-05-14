using System;
using System.ComponentModel.DataAnnotations;

namespace FreeBlazorExample.EFModels.EFModels;

// Stub FileStorage entity retained for compatibility with removed Files module.
// Not mapped to a real table — kept only so existing code compiles.
public partial class FileStorage
{
    [Key]
    public Guid FileId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ItemId { get; set; }
    public string? SourceFileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long? Bytes { get; set; }
    public byte[]? Value { get; set; }
    public DateTime UploadDate { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public bool Deleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
