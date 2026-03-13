using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Identity;

public class UserProfile : BaseEntity
{
    public Guid UserId { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string PreferredLanguage { get; private set; } = CobryxDefaults.Locale;
    public string Timezone { get; private set; } = CobryxDefaults.Timezone;


    public virtual User User { get; private set; } = null!;

    private UserProfile() { }

    public UserProfile(Guid userId, string? phoneNumber = null, string? avatarUrl = null)
    {
        UserId = userId;
        PhoneNumber = phoneNumber;
        AvatarUrl = avatarUrl;
    }

    public void Update(string? phoneNumber, string? avatarUrl, string preferredLanguage, string timezone)
    {
        PhoneNumber = phoneNumber;
        AvatarUrl = avatarUrl;
        PreferredLanguage = preferredLanguage;
        Timezone = timezone;
        UpdateTimestamp();
    }
}
