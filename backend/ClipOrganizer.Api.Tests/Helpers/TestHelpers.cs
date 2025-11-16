using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.Models;

namespace ClipOrganizer.Api.Tests.Helpers;

public static class TestHelpers
{
    public static ClipDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ClipDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new ClipDbContext(options);
    }

    public static async Task<ClipDbContext> SeedTestDataAsync(
        ClipDbContext? context = null,
        Action<ClipDbContext>? seedAction = null)
    {
        context ??= CreateInMemoryDbContext();
        
        seedAction?.Invoke(context);
        await context.SaveChangesAsync();
        
        return context;
    }

    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    public static Mock<IConfiguration> CreateMockConfiguration(Dictionary<string, string>? settings = null)
    {
        var mockConfig = new Mock<IConfiguration>();
        
        if (settings != null)
        {
            foreach (var setting in settings)
            {
                mockConfig.Setup(c => c[setting.Key]).Returns(setting.Value);
                mockConfig.Setup(c => c.GetSection(setting.Key).Value).Returns(setting.Value);
            }
        }

        return mockConfig;
    }

    public static Mock<IConfigurationSection> CreateMockConfigurationSection(string key, string value)
    {
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Key).Returns(key);
        mockSection.Setup(s => s.Value).Returns(value);
        return mockSection;
    }
}

