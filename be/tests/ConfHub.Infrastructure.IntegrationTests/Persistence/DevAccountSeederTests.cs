using ConfHub.Application.Common.Persistence;
using ConfHub.Domain.Accounts;
using ConfHub.Infrastructure.IntegrationTests.Messaging;
using ConfHub.Infrastructure.Persistence;
using ConfHub.Infrastructure.Persistence.Seeding;
using ConfHub.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConfHub.Infrastructure.IntegrationTests.Persistence;

[Collection(DatabaseCollectionDefinition.Name)]
public class DevAccountSeederTests(OutboxFixture fixture)
{
    private const string Password = "Confhub@123";

    [Fact]
    public async Task SeedingTwiceCreatesOneActiveAccountPerRole()
    {
        await SeedAsync(new DevSeedOptions { Enabled = true, Password = Password });
        await SeedAsync(new DevSeedOptions { Enabled = true, Password = Password });

        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
        var users = await dbContext.Set<User>()
            .Include(user => user.Role)
            .Where(user => EF.Functions.Like(user.Email, "%@confhub.local"))
            .ToListAsync();

        Assert.Equal(5, users.Count);
        var passwordHasher = new AspNetPasswordHasher();
        var roleCodes = new List<string>();
        foreach (var user in users)
        {
            Assert.Equal(UserStatus.Active, user.Status);
            Assert.True(passwordHasher.Verify(user.PasswordHash, Password));
            roleCodes.Add(user.Role.Code);
        }

        roleCodes.Sort(StringComparer.Ordinal);
        Assert.Equal(
            [RoleCodes.Admin, RoleCodes.Attendee, RoleCodes.Organizer, RoleCodes.Staff, RoleCodes.Supplier],
            roleCodes);
    }

    [Fact]
    public async Task EnabledWithoutPasswordFails()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SeedAsync(new DevSeedOptions { Enabled = true, Password = string.Empty }));
    }

    private async Task SeedAsync(DevSeedOptions options)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var seeder = new DevAccountSeeder(
            scope.ServiceProvider.GetRequiredService<IRepository<User>>(),
            scope.ServiceProvider.GetRequiredService<IReadRepository<Role>>(),
            new AspNetPasswordHasher(),
            Options.Create(options));

        await seeder.SeedAsync(CancellationToken.None);
    }
}
