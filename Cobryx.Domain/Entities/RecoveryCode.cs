using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class RecoveryCode : BaseEntity
{
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public virtual User User { get; private set; } = null!;

    private RecoveryCode() { }

    public RecoveryCode(Guid userId, string codeHash)
    {
        UserId = userId;
        CodeHash = codeHash;
        IsUsed = false;
    }

    public void Use()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public bool VerifyCode(string hashedCode)
    {
        return CodeHash == hashedCode;
    }
}
