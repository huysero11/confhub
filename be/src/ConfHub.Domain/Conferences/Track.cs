using ConfHub.Domain.Common;

namespace ConfHub.Domain.Conferences;

public sealed class Track : BaseEntity, IAggregateRoot
{
    public const int MaxPerConference = 5;

    private Track()
    {
    }

    public Guid ConferenceId { get; private set; }
    public string NameVi { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;

    public static Track Create(Guid conferenceId, string nameVi, string nameEn)
    {
        var track = new Track { ConferenceId = conferenceId };
        track.Rename(nameVi, nameEn);
        return track;
    }

    public void Rename(string nameVi, string nameEn)
    {
        NameVi = nameVi.Trim();
        NameEn = nameEn.Trim();
    }
}
