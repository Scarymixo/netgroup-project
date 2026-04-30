using App.DAL.EF;
using App.Domain.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestProject.Integration;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtIssuer = "TestIssuer";
    public const string JwtAudience = "TestAudience";
    public const string JwtKey = "ThisIsATestSigningKey-AtLeast32CharsLong!!";

    public const string SeededUserEmail = "integration-tester@test.local";
    public Guid SeededUserId { get; private set; }

    private SqliteConnection _connection = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["JWT:Issuer"] = JwtIssuer,
                ["JWT:Audience"] = JwtAudience,
                ["JWT:Key"] = JwtKey,
                ["JWT:ExpiresInSeconds"] = "3600",
                ["DataInitialization:DropDatabase"] = "false",
                ["DataInitialization:MigrateDatabase"] = "false",
                ["DataInitialization:SeedIdentity"] = "false",
                ["DataInitialization:SeedData"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || d.ServiceType == typeof(DbContextOptions)
                    || d.ServiceType == typeof(AppDbContext)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration")
                        && d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext)))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var efServiceProvider = new ServiceCollection()
                .AddEntityFrameworkSqlite()
                .BuildServiceProvider();

            services.AddDbContext<AppDbContext>(options => options
                .UseSqlite(_connection)
                .UseInternalServiceProvider(efServiceProvider));
        });
    }

    public async Task InitializeAsync()
    {
        // Force host construction so ConfigureWebHost runs.
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var userManager = sp.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();
        var existing = await userManager.FindByEmailAsync(SeededUserEmail);
        if (existing == null)
        {
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = SeededUserEmail,
                Email = SeededUserEmail,
                EmailConfirmed = true,
            };
            var result = await userManager.CreateAsync(user, "Test.Password.123!");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to seed integration test user: " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
            SeededUserId = user.Id;
        }
        else
        {
            SeededUserId = existing.Id;
        }
    }

    public new async Task DisposeAsync()
    {
        if (_connection != null!)
        {
            await _connection.DisposeAsync();
        }
        await base.DisposeAsync();
    }
}
