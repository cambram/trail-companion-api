using Template.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Template.Infrastructure.Tests;

internal static class TestDbContext
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"template-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
