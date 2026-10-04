using ConfHub.Application.Accounts.Messages;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Domain.Accounts;
using ConfHub.Infrastructure.IntegrationTests.Messaging;
using ConfHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConfHub.Infrastructure.IntegrationTests.Persistence;

[Collection(DatabaseCollectionDefinition.Name)]
public class AccountPersistenceTests(OutboxFixture fixture)
{
    [Fact]
    public async Task MigrationSeedsFiveRoles()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();

        var codes = await dbContext.Set<Role>().Select(role => role.Code).OrderBy(code => code).ToListAsync();

        Assert.Equal(
            [RoleCodes.Admin, RoleCodes.Attendee, RoleCodes.Organizer, RoleCodes.Staff, RoleCodes.Supplier],
            codes);
    }

    [Fact]
    public async Task AddAssignsSequentialIdAndCreatedAtAndPublishesMessage()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
        var role = await GetRoleAsync(dbContext, RoleCodes.Attendee);
        var user = User.Register(UniqueEmail(), "hash", "Minh", role, null);

        // Id có ngay khi Add (trước SaveChanges) → domain event lấy được UserId.
        dbContext.Set<User>().Add(user);
        Assert.NotEqual(Guid.Empty, user.Id);

        await users.SaveChangesAsync();

        Assert.NotEqual(default, user.CreatedAt);
        Assert.Empty(user.DomainEvents);
        Assert.True(await WaitUntilConsumedAsync(user.Id, TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public async Task DuplicateEmailIsRejectedByDatabase()
    {
        var email = UniqueEmail();

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
            var role = await GetRoleAsync(scope.ServiceProvider.GetRequiredService<ConfHubDbContext>(), RoleCodes.Attendee);
            await users.AddAsync(User.Register(email, "hash", "Minh", role, null));
        }

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
            var role = await GetRoleAsync(scope.ServiceProvider.GetRequiredService<ConfHubDbContext>(), RoleCodes.Attendee);

            // Khác hoa/thường nhưng sau chuẩn hóa là cùng 1 email.
            var duplicate = User.Register(email.ToUpperInvariant(), "hash", "Minh 2", role, null);
            var exception = await Assert.ThrowsAsync<ConflictException>(() => users.AddAsync(duplicate));

            Assert.Equal("Duplicate", exception.Code);
        }
    }

    private static Task<Role> GetRoleAsync(ConfHubDbContext dbContext, string code)
    {
        return dbContext.Set<Role>().SingleAsync(role => role.Code == code);
    }

    private static string UniqueEmail()
    {
        return $"user-{Guid.NewGuid():N}@test.local";
    }

    private async Task<bool> WaitUntilConsumedAsync(Guid userId, TimeSpan timeout)
    {
        var stopAt = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < stopAt)
        {
            var consumed = fixture.Harness.Consumed
                .Select<SendVerificationEmailMessage>(message => message.Context.Message.UserId == userId)
                .Any();
            if (consumed)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        return false;
    }
}
