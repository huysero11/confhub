using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ConfHub.Api.IntegrationTests.Support;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Common.Time;
using ConfHub.Domain.Accounts;
using ConfHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConfHub.Api.IntegrationTests.Conferences;

[Collection(ApiCollectionDefinition.Name)]
public sealed class ConferenceApiTests : IDisposable
{
    private const string BasePath = "/api/manage/conferences";
    private const string Password = "matkhau123";

    private readonly ConfHubApiFactory _databaseFactory;
    private readonly WebApplicationFactory<Program> _webFactory;
    private readonly HttpClient _client;

    // Cố định riêng giờ nghiệp vụ G3; đồng hồ JWT của các test tài khoản giữ nguyên.
    public ConferenceApiTests(ConfHubApiFactory factory)
    {
        _databaseFactory = factory;
        var clock = new TestClock(new DateTimeOffset(2026, 11, 5, 3, 0, 0, TimeSpan.Zero));
        var venueClock = new VenueClock(clock, TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"));
        _webFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(venueClock);
        }));
        _client = _webFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    // Giải phóng client và host riêng của test, không hủy fixture CSDL chung.
    public void Dispose()
    {
        _client.Dispose();
        _webFactory.Dispose();
    }

    // ---------- QUYỀN VÀ CHỦ SỞ HỮU ----------

    // Cả bốn controller đều yêu cầu token và permission đúng.
    [Theory]
    [InlineData("", "GET")]
    [InlineData("/00000000-0000-0000-0000-000000000001/days", "POST")]
    [InlineData("/00000000-0000-0000-0000-000000000001/rooms", "GET")]
    [InlineData("/00000000-0000-0000-0000-000000000001/tracks", "GET")]
    public async Task EveryControllerRequiresAuthenticationAndPermission(string suffix, string method)
    {
        var httpMethod = new HttpMethod(method);
        var anonymous = await SendAsync(httpMethod, BasePath + suffix, null, DayBody());
        await AssertProblemAsync(anonymous, HttpStatusCode.Unauthorized, "Unauthorized");
        var attendeeToken = await LoginAsNewUserAsync(RoleCodes.Attendee);
        var forbidden = await SendAsync(httpMethod, BasePath + suffix, attendeeToken, DayBody());
        await AssertProblemAsync(forbidden, HttpStatusCode.Forbidden, "Forbidden");
    }

    // BTC khác không thể đọc, sửa, xóa hoặc thêm dữ liệu vào hội nghị của chủ thứ nhất.
    [Fact]
    public async Task AnotherOrganizerSeesNotFoundAndAnEmptyList()
    {
        var ownerToken = await LoginAsNewUserAsync(RoleCodes.Organizer);
        var conferenceId = await CreateConferenceAsync(ownerToken);
        var otherToken = await LoginAsNewUserAsync(RoleCodes.Organizer);
        var path = $"{BasePath}/{conferenceId}";
        await AssertProblemAsync(await SendAsync(HttpMethod.Get, path, otherToken), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Put, path, otherToken, DetailsBody()), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Delete, path, otherToken), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Post, path + "/rooms", otherToken, new { name = "P101", capacity = 100 }), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Post, path + "/tracks", otherToken, new { nameVi = "Công nghệ", nameEn = "Technology" }), HttpStatusCode.NotFound, "NotFound");
        var list = await ReadJsonAsync(await SendAsync(HttpMethod.Get, BasePath, otherToken));
        Assert.Equal(0, list.GetArrayLength());
    }

    // Id con thuộc hội nghị khác cũng phải trả 404 dù người gọi sở hữu cả hai hội nghị.
    [Fact]
    public async Task ChildIdsCannotBeUsedAcrossConferencesOrDays()
    {
        var token = await LoginAsNewUserAsync(RoleCodes.Organizer);
        var firstId = await CreateConferenceAsync(token);
        var secondId = await CreateConferenceAsync(token);
        var firstPath = $"{BasePath}/{firstId}";
        var secondPath = $"{BasePath}/{secondId}";
        var firstDetail = await ReadJsonAsync(await SendAsync(HttpMethod.Get, firstPath, token));
        var firstDayId = firstDetail.GetProperty("days")[0].GetProperty("id").GetGuid();
        var otherDayId = firstDetail.GetProperty("days")[1].GetProperty("id").GetGuid();
        var roomId = await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, firstPath + "/rooms", token, new { name = "P101", capacity = 100 }));
        var trackId = await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, firstPath + "/tracks", token, new { nameVi = "Công nghệ", nameEn = "Technology" }));
        var breakId = await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, $"{firstPath}/days/{firstDayId}/breaks", token, BreakBody()));

        await AssertProblemAsync(await SendAsync(HttpMethod.Put, $"{secondPath}/days/{firstDayId}", token, DayBody()), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Delete, $"{secondPath}/days/{firstDayId}", token), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Post, $"{secondPath}/days/{firstDayId}/breaks", token, BreakBody()), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Put, $"{firstPath}/days/{otherDayId}/breaks/{breakId}", token, BreakBody()), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Delete, $"{firstPath}/days/{otherDayId}/breaks/{breakId}", token), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Put, $"{secondPath}/rooms/{roomId}", token, new { name = "P102", capacity = 50 }), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Delete, $"{secondPath}/rooms/{roomId}", token), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Put, $"{secondPath}/tracks/{trackId}", token, new { nameVi = "Kinh tế", nameEn = "Economics" }), HttpStatusCode.NotFound, "NotFound");
        await AssertProblemAsync(await SendAsync(HttpMethod.Delete, $"{secondPath}/tracks/{trackId}", token), HttpStatusCode.NotFound, "NotFound");
    }

    // ---------- HỘI NGHỊ ----------

    // BR20: Không gửi tùy chọn thì hội nghị mặc định yêu cầu duyệt đăng ký.
    [Fact]
    public async Task OmittedApprovalDefaultsToTrue()
    {
        var token = await LoginAsNewUserAsync(RoleCodes.Organizer);
        var request = new
        {
            nameVi = "Mặc định duyệt",
            nameEn = "Approval by default",
            venue = "Hà Nội",
            startDate = "2026-12-03",
            endDate = "2026-12-04",
            dayStartTime = "08:00",
            dayEndTime = "17:00",
            registrationCloseAt = "2026-12-01T23:59:00+07:00"
        };
        var conferenceId = await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, BasePath, token, request));
        var detail = await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"{BasePath}/{conferenceId}", token));
        Assert.True(detail.GetProperty("requireApproval").GetBoolean());
    }

    // BR17: Thiếu tên tiếng Anh trả lỗi Validation từ validator trước khi tạo hội nghị.
    [Fact]
    public async Task CreateConferenceWithoutNameEnReturnsValidation()
    {
        var token = await LoginAsNewUserAsync(RoleCodes.Organizer);
        var request = new
        {
            nameVi = "Thiếu tên tiếng Anh",
            venue = "Hà Nội",
            startDate = "2026-12-03",
            endDate = "2026-12-04",
            dayStartTime = "08:00",
            dayEndTime = "17:00",
            registrationCloseAt = "2026-12-01T23:59:00+07:00"
        };

        var response = await SendAsync(HttpMethod.Post, BasePath, token, request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.Equal("Validation", problem.GetProperty("code").GetString());
        Assert.Contains(problem.GetProperty("errors").GetProperty("NameEn").EnumerateArray(), error => error.GetString() == "NameEnLength");
    }

    // Kiểm JSON hợp đồng, phép chiếu danh sách, cập nhật và xóa hội nghị.
    [Fact]
    public async Task ConferenceLifecycleReturnsExpectedJsonAndRoomCount()
    {
        var token = await LoginAsNewUserAsync(RoleCodes.Organizer);
        var conferenceId = await CreateConferenceAsync(token);
        var path = $"{BasePath}/{conferenceId}";
        var detail = await ReadJsonAsync(await SendAsync(HttpMethod.Get, path, token));
        Assert.Equal("Draft", detail.GetProperty("status").GetString());
        Assert.Equal(2, detail.GetProperty("days").GetArrayLength());
        Assert.Equal("08:00", detail.GetProperty("days")[0].GetProperty("startTime").GetString());
        Assert.Equal("17:00", detail.GetProperty("days")[0].GetProperty("endTime").GetString());
        Assert.Equal("2026-12-01T16:59:00Z", detail.GetProperty("registrationCloseAt").GetString());
        Assert.Equal("Công nghệ Xanh 2026", detail.GetProperty("nameVi").GetString());
        Assert.Equal("Green Technology 2026", detail.GetProperty("nameEn").GetString());
        foreach (var capacity in new[] { 40, 100, 60 })
        {
            await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, path + "/rooms", token, new { name = $"P{capacity}", capacity }));
        }

        var list = await ReadJsonAsync(await SendAsync(HttpMethod.Get, BasePath, token));
        Assert.Equal(3, list[0].GetProperty("roomCount").GetInt32());
        Assert.Equal("Green Technology 2026", list[0].GetProperty("nameEn").GetString());
        Assert.Equal("2026-12-03", list[0].GetProperty("firstDate").GetString());
        Assert.Equal("2026-12-04", list[0].GetProperty("lastDate").GetString());
        var update = await SendAsync(HttpMethod.Put, path, token, DetailsBody());
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        detail = await ReadJsonAsync(await SendAsync(HttpMethod.Get, path, token));
        Assert.Equal("Đã sửa", detail.GetProperty("nameVi").GetString());
        Assert.Equal("Updated", detail.GetProperty("nameEn").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, path, token)).StatusCode);
        await AssertProblemAsync(await SendAsync(HttpMethod.Get, path, token), HttpStatusCode.NotFound, "NotFound");
    }

    // ---------- NGÀY VÀ KHUNG NGHỈ ----------

    // Kiểm thêm, sửa, xóa ngày và các mã lỗi cần hiển thị trên giao diện.
    [Fact]
    public async Task DaysValidateDuplicatePastAndLastDayAndSupportUpdates()
    {
        var token = await LoginAsNewUserAsync(RoleCodes.Organizer);
        var conferenceId = await CreateConferenceAsync(token);
        var path = $"{BasePath}/{conferenceId}";
        await AssertProblemAsync(await SendAsync(HttpMethod.Post, path + "/days", token, DayBody()), HttpStatusCode.UnprocessableEntity, "DayDateTaken");
        await AssertProblemAsync(await SendAsync(HttpMethod.Post, path + "/days", token, DayBody("2026-11-04")), HttpStatusCode.UnprocessableEntity, "DayInPast");
        var addedDayId = await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, path + "/days", token, DayBody("2026-12-05")));
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Put, $"{path}/days/{addedDayId}", token, DayBody("2026-12-06", "09:00", "12:00"))).StatusCode);
        var detail = await ReadJsonAsync(await SendAsync(HttpMethod.Get, path, token));
        Assert.Equal("2026-12-06", detail.GetProperty("days")[2].GetProperty("date").GetString());
        Assert.Equal("09:00", detail.GetProperty("days")[2].GetProperty("startTime").GetString());
        foreach (var day in detail.GetProperty("days").EnumerateArray())
        {
            var dayId = day.GetProperty("id").GetGuid();
            var deletion = await SendAsync(HttpMethod.Delete, $"{path}/days/{dayId}", token);
            if (dayId == addedDayId)
            {
                await AssertProblemAsync(deletion, HttpStatusCode.UnprocessableEntity, "LastDayRequired");
            }
            else
            {
                Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);
            }
        }
    }

    // Khung nghỉ chồng hoặc ngoài ngày bị chặn, sửa chính nó và xóa thành công.
    [Fact]
    public async Task BreaksValidateContainmentOverlapAndDayResize()
    {
        var token = await LoginAsNewUserAsync(RoleCodes.Organizer);
        var conferenceId = await CreateConferenceAsync(token);
        var path = $"{BasePath}/{conferenceId}";
        var detail = await ReadJsonAsync(await SendAsync(HttpMethod.Get, path, token));
        var dayId = detail.GetProperty("days")[0].GetProperty("id").GetGuid();
        var breaksPath = $"{path}/days/{dayId}/breaks";
        var breakId = await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, breaksPath, token, BreakBody()));
        await AssertProblemAsync(await SendAsync(HttpMethod.Post, breaksPath, token, BreakBody("11:00", "11:45")), HttpStatusCode.UnprocessableEntity, "BreakOverlap");
        await AssertProblemAsync(await SendAsync(HttpMethod.Post, breaksPath, token, BreakBody("07:00", "08:15")), HttpStatusCode.UnprocessableEntity, "BreakOutsideDay");
        await AssertProblemAsync(await SendAsync(HttpMethod.Put, $"{path}/days/{dayId}", token, DayBody(endTime: "12:00")), HttpStatusCode.UnprocessableEntity, "BreakOutsideDay");
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Put, $"{breaksPath}/{breakId}", token, BreakBody())).StatusCode);
        await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, breaksPath, token, BreakBody("13:00", "13:15")));
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"{breaksPath}/{breakId}", token)).StatusCode);
        detail = await ReadJsonAsync(await SendAsync(HttpMethod.Get, path, token));
        var breaks = detail.GetProperty("days")[0].GetProperty("breaks");
        Assert.Equal(1, breaks.GetArrayLength());
        Assert.Equal("13:00", breaks[0].GetProperty("startTime").GetString());
    }

    // Model binding từ chối sai định dạng; validator từ chối sai bước 15 phút.
    [Theory]
    [InlineData("8h", false)]
    [InlineData("8:00", false)]
    [InlineData("08:00:00", false)]
    [InlineData("08:10", true)]
    public async Task InvalidTimeReturnsProblemDetails(string startTime, bool isValidationError)
    {
        var token = await LoginAsNewUserAsync(RoleCodes.Organizer);
        var response = await SendAsync(HttpMethod.Post, BasePath, token, ConferenceBody(startTime));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("errors", out var errors));
        if (isValidationError)
        {
            Assert.Equal("Validation", problem.GetProperty("code").GetString());
            Assert.Contains(errors.GetProperty("DayStartTime").EnumerateArray(), error => error.GetString() == "QuarterHourStep");
        }
    }

    // ---------- PHÒNG VÀ TRACK ----------

    // Phòng kiểm trùng không phân biệt hoa thường, giữ tên khi sửa và sắp sức chứa.
    [Fact]
    public async Task RoomsValidateNamesCapacityAndSortOrder()
    {
        var token = await LoginAsNewUserAsync(RoleCodes.Organizer);
        var conferenceId = await CreateConferenceAsync(token);
        var path = $"{BasePath}/{conferenceId}/rooms";
        var roomId = await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, path, token, new { name = " P101 ", capacity = 100 }));
        await AssertProblemAsync(await SendAsync(HttpMethod.Post, path, token, new { name = "p101", capacity = 50 }), HttpStatusCode.Conflict, "RoomNameTaken");
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Put, $"{path}/{roomId}", token, new { name = "P101", capacity = 100 })).StatusCode);
        await AssertProblemAsync(await SendAsync(HttpMethod.Post, path, token, new { name = "P102", capacity = 0 }), HttpStatusCode.BadRequest, "Validation");
        await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, path, token, new { name = "P103", capacity = 40 }));
        var secondRoomId = await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, path, token, new { name = "P102", capacity = 60 }));
        await AssertProblemAsync(await SendAsync(HttpMethod.Put, $"{path}/{secondRoomId}", token, new { name = "p101", capacity = 60 }), HttpStatusCode.Conflict, "RoomNameTaken");
        var rooms = await ReadJsonAsync(await SendAsync(HttpMethod.Get, path, token));
        Assert.Equal([100, 60, 40], rooms.EnumerateArray().Select(room => room.GetProperty("capacity").GetInt32()));
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"{path}/{roomId}", token)).StatusCode);
    }

    // Track thứ sáu bị chặn; đổi tên, kiểm trùng và xóa đi qua đủ endpoint.
    [Fact]
    public async Task TracksValidateLimitDuplicateAndCreationOrder()
    {
        var token = await LoginAsNewUserAsync(RoleCodes.Organizer);
        var conferenceId = await CreateConferenceAsync(token);
        var path = $"{BasePath}/{conferenceId}/tracks";
        var trackIds = new List<Guid>();
        foreach (var name in new[] { "Z", "A", "M", "B", "X" })
        {
            trackIds.Add(await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, path, token, new { nameVi = name, nameEn = name })));
        }

        await AssertProblemAsync(await SendAsync(HttpMethod.Post, path, token, new { nameVi = "z", nameEn = "z" }), HttpStatusCode.Conflict, "TrackNameTaken");
        await AssertProblemAsync(await SendAsync(HttpMethod.Post, path, token, new { nameVi = "Thứ sáu", nameEn = "Sixth" }), HttpStatusCode.UnprocessableEntity, "TooManyTracks");
        var tracks = await ReadJsonAsync(await SendAsync(HttpMethod.Get, path, token));
        Assert.Equal(trackIds, tracks.EnumerateArray().Select(track => track.GetProperty("id").GetGuid()));
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Put, $"{path}/{trackIds[0]}", token, new { nameVi = "Z", nameEn = " English " })).StatusCode);
        tracks = await ReadJsonAsync(await SendAsync(HttpMethod.Get, path, token));
        Assert.Equal("English", tracks[0].GetProperty("nameEn").GetString());
        await AssertProblemAsync(await SendAsync(HttpMethod.Put, $"{path}/{trackIds[1]}", token, new { nameVi = "z", nameEn = "z" }), HttpStatusCode.Conflict, "TrackNameTaken");
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"{path}/{trackIds[0]}", token)).StatusCode);
        await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, path, token, new { nameVi = "Thay thế", nameEn = "Replacement" }));
    }

    // ---------- HELPER ----------

    // Helper của CreateConferenceAsync, InvalidTimeReturnsProblemDetails (cùng lớp): Body không có id route.
    private static object ConferenceBody(string startTime = "08:00")
    {
        return new
        {
            nameVi = " Công nghệ Xanh 2026 ", nameEn = " Green Technology 2026 ", venue = " Hà Nội ",
            startDate = "2026-12-03", endDate = "2026-12-04", dayStartTime = startTime, dayEndTime = "17:00",
            registrationCloseAt = "2026-12-01T23:59:00+07:00", requireApproval = true
        };
    }

    // Helper của ConferenceLifecycleReturnsExpectedJsonAndRoomCount, AnotherOrganizerSeesNotFoundAndAnEmptyList (cùng lớp): Chỉ sửa thông tin.
    private static object DetailsBody()
    {
        return new { nameVi = "Đã sửa", nameEn = "Updated", venue = "Hà Nội", registrationCloseAt = "2026-12-01T23:59:00+07:00", requireApproval = true };
    }

    // Helper của các test ngày (cùng lớp): Dựng giờ mặc định hợp lệ.
    private static object DayBody(string date = "2026-12-03", string startTime = "08:00", string endTime = "17:00")
    {
        return new { date, startTime, endTime };
    }

    // Helper của các test khung nghỉ (cùng lớp): Dựng một khung nghỉ trưa.
    private static object BreakBody(string startTime = "11:30", string endTime = "13:00")
    {
        return new { name = "Nghỉ trưa", startTime, endTime };
    }

    // Helper của các test API (cùng lớp): Đọc JSON để kiểm cả hợp đồng định dạng giờ.
    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    // Helper của các test tạo dữ liệu (cùng lớp): Kiểm 201 và id không rỗng.
    private static async Task<Guid> ReadCreatedIdAsync(HttpResponseMessage response)
    {
        var body = await ReadJsonAsync(response);
        Assert.True(response.StatusCode == HttpStatusCode.Created, body.ToString());
        var id = body.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        return id;
    }

    // Helper của các test lỗi API (cùng lớp): Kiểm status và mã lỗi nghiệp vụ.
    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string code)
    {
        var body = await ReadJsonAsync(response);
        Assert.True(response.StatusCode == expectedStatus, body.ToString());
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(code, body.GetProperty("code").GetString());
    }

    // Helper của các test API (cùng lớp): Tạo request độc lập để token không rò giữa người dùng.
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    // Helper của các test hội nghị (cùng lớp): Tạo hội nghị qua API thật.
    private async Task<Guid> CreateConferenceAsync(string token)
    {
        return await ReadCreatedIdAsync(await SendAsync(HttpMethod.Post, BasePath, token, ConferenceBody()));
    }

    // Helper của các test phân quyền (cùng lớp): Tài khoản Active lấy quyền từ seed, đăng nhập qua endpoint thật.
    private async Task<string> LoginAsNewUserAsync(string roleCode)
    {
        var email = $"conference-{Guid.NewGuid():N}@test.local";
        await using (var scope = _databaseFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
            var role = await dbContext.Set<Role>().SingleAsync(role => role.Code == roleCode);
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            dbContext.Set<User>().Add(User.CreateActive(email, passwordHasher.Hash(Password), "Lan", role, "Trường"));
            await dbContext.SaveChangesAsync();
        }

        var response = await SendAsync(HttpMethod.Post, "/api/auth/login", null, new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        var accessToken = body.GetProperty("accessToken").GetString();
        Assert.NotNull(accessToken);
        return accessToken;
    }
}
