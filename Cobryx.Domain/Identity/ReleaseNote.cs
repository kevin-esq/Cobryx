using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Identity;

public class ReleaseNote : BaseEntity
{
    public string ReleaseVersion { get; private set; }
    public string Title { get; private set; }
    public string Content { get; private set; }
    public DateTime ReleaseDate { get; private set; }
    public bool IsPublished { get; private set; }

    private ReleaseNote()
    {
        ReleaseVersion = null!;
        Title = null!;
        Content = null!;
    }

    public ReleaseNote(string releaseVersion, string title, string content, DateTime releaseDate)
    {
        ReleaseVersion = releaseVersion;
        Title = title;
        Content = content;
        ReleaseDate = releaseDate;
        IsPublished = false;
    }

    public void Publish()
    {
        IsPublished = true;
        UpdateTimestamp();
    }
}
