using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Domain.Accounts;
using ConfHub.Domain.Common;
using ConfHub.Domain.Conferences;
using ConfHub.Infrastructure.IntegrationTests.Messaging;
using ConfHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConfHub.Infrastructure.IntegrationTests.Persistence;

[Collection(DatabaseCollectionDefinition.Name)]
public class ConferencePersistenceTests(OutboxFixture fixture)
{
    // ---------- LƯU VÀ XÓA AGGREGATE ----------

    // Đọc bằng context mới để kiểm complex property, UTC và cascade thật ở SQL Server.
    [Fact]
    public async Task RoundTripPreservesTimeRangesAndUtcAndDeleteCascades()
    {
        Guid conferenceId;
        Guid dayId;
        Guid breakId;
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var conference = await CreateConferenceAsync(services);
            conferenceId = conference.Id;
            dayId = conference.Days.First().Id;
            var breakSlot = conference.AddBreak(dayId, " Nghỉ trưa ", TimeRange.Create(new TimeOnly(11, 30), new TimeOnly(13, 0)));
            breakId = breakSlot.Id;
            await services.GetRequiredService<IRepository<Conference>>().SaveChangesAsync();
            await services.GetRequiredService<IRepository<Room>>().AddAsync(Room.Create(conferenceId, " P101 ", 100));
            await services.GetRequiredService<IRepository<Track>>().AddAsync(Track.Create(conferenceId, " Công nghệ ", " Technology "));
        }

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var conferences = scope.ServiceProvider.GetRequiredService<IRepository<Conference>>();
            var conference = await conferences.FirstOrDefaultAsync(new ConferenceWithDaysByIdSpec(conferenceId));
            Assert.NotNull(conference);
            Assert.Equal("Conference", conference.NameEn);
            Assert.Equal(DateTimeKind.Utc, conference.RegistrationCloseAt.Kind);
            Assert.Equal(new DateTime(2026, 12, 1, 16, 59, 0, DateTimeKind.Utc), conference.RegistrationCloseAt);
            Assert.Equal(2, conference.Days.Count);
            var day = conference.FindDay(dayId);
            Assert.NotNull(day);
            Assert.Equal(TimeRange.Create(new TimeOnly(8, 0), new TimeOnly(17, 0)), day.Hours);
            var breakSlot = day.FindBreak(breakId);
            Assert.NotNull(breakSlot);
            Assert.Equal("Nghỉ trưa", breakSlot.Name);
            Assert.Equal(TimeRange.Create(new TimeOnly(11, 30), new TimeOnly(13, 0)), breakSlot.Time);

            var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
            var track = await dbContext.Set<Track>().SingleAsync(track => track.ConferenceId == conferenceId);
            Assert.Equal("Technology", track.NameEn);

            var startTime = await dbContext.Database.SqlQuery<TimeOnly>(
                $"SELECT StartTime AS Value FROM ConferenceDays WHERE Id = {dayId}").SingleAsync();
            var endTime = await dbContext.Database.SqlQuery<TimeOnly>(
                $"SELECT EndTime AS Value FROM BreakSlots WHERE Id = {breakId}").SingleAsync();
            Assert.Equal(new TimeOnly(8, 0), startTime);
            Assert.Equal(new TimeOnly(13, 0), endTime);
        }

        // Không nạp entity con: phép xóa phải do cascade của CSDL thực hiện.
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var conferences = scope.ServiceProvider.GetRequiredService<IRepository<Conference>>();
            var conference = await conferences.GetByIdAsync(conferenceId);
            Assert.NotNull(conference);
            Assert.Equal("Conference", conference.NameEn);
            await conferences.DeleteAsync(conference);
        }

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
            Assert.False(await dbContext.Set<ConferenceDay>().AnyAsync(day => day.ConferenceId == conferenceId));
            Assert.False(await dbContext.Set<BreakSlot>().AnyAsync(breakSlot => breakSlot.Id == breakId));
            Assert.False(await dbContext.Set<Room>().AnyAsync(room => room.ConferenceId == conferenceId));
            Assert.False(await dbContext.Set<Track>().AnyAsync(track => track.ConferenceId == conferenceId));
        }
    }

    // Gỡ ngày đã nạp phải xóa cả khung nghỉ qua orphan delete và cascade.
    [Fact]
    public async Task RemovingDayDeletesItsBreaks()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var conference = await CreateConferenceAsync(services);
        var day = conference.Days.First();
        var breakSlot = conference.AddBreak(day.Id, "Nghỉ", TimeRange.Create(new TimeOnly(10, 0), new TimeOnly(10, 15)));
        var conferences = services.GetRequiredService<IRepository<Conference>>();
        await conferences.SaveChangesAsync();
        conference.RemoveDay(day.Id);
        await conferences.SaveChangesAsync();
        var dbContext = services.GetRequiredService<ConfHubDbContext>();
        Assert.False(await dbContext.Set<ConferenceDay>().AnyAsync(storedDay => storedDay.Id == day.Id));
        Assert.False(await dbContext.Set<BreakSlot>().AnyAsync(storedBreak => storedBreak.Id == breakSlot.Id));
    }

    // ---------- SEED VÀ RÀNG BUỘC CSDL ----------

    // Migration chỉ cấp ba quyền đầu tiên của G3 cho Organizer.
    [Fact]
    public async Task MigrationSeedsOrganizerPermissionsOnly()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
        var roles = await dbContext.Set<Role>().ToListAsync();
        foreach (var role in roles)
        {
            if (role.Code == RoleCodes.Organizer)
            {
                Assert.Equal([Permissions.ConferenceManage, Permissions.RoomManage, Permissions.SessionManage], role.Permissions);
            }
            else
            {
                Assert.Empty(role.Permissions);
            }
        }
    }

    // Unique index là lớp bảo vệ cuối khi hai request vượt kiểm tra trùng tên.
    [Fact]
    public async Task CaseInsensitiveRoomUniqueIndexReturnsDuplicate()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var conference = await CreateConferenceAsync(scope.ServiceProvider);
        var rooms = scope.ServiceProvider.GetRequiredService<IRepository<Room>>();
        await rooms.AddAsync(Room.Create(conference.Id, "P101", 100));
        var exception = await Assert.ThrowsAsync<ConflictException>(() => rooms.AddAsync(Room.Create(conference.Id, "p101", 50)));
        Assert.Equal("Duplicate", exception.Code);
    }

    // ---------- HELPER ----------

    // Helper của các test lưu trữ (cùng lớp): Tạo chủ hội nghị và aggregate bằng repository.
    private static async Task<Conference> CreateConferenceAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<ConfHubDbContext>();
        var role = await dbContext.Set<Role>().SingleAsync(role => role.Code == RoleCodes.Organizer);
        var owner = User.CreateActive($"conference-{Guid.NewGuid():N}@test.local", "hash", "Lan", role, "Trường");
        await services.GetRequiredService<IRepository<User>>().AddAsync(owner);
        var details = new ConferenceDetails(
            "Hội nghị",
            " Conference ",
            null,
            null,
            "Hà Nội",
            new DateTime(2026, 12, 1, 16, 59, 0, DateTimeKind.Utc),
            true);
        var conference = Conference.Create(
            owner.Id,
            details,
            new DateOnly(2026, 12, 3),
            new DateOnly(2026, 12, 4),
            TimeRange.Create(new TimeOnly(8, 0), new TimeOnly(17, 0)),
            new DateOnly(2026, 11, 5),
            new DateTime(2026, 11, 5, 3, 0, 0, DateTimeKind.Utc),
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"));
        await services.GetRequiredService<IRepository<Conference>>().AddAsync(conference);
        return conference;
    }
}
