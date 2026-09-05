using HRomance.Data;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Tests;

public static class TestDatenbank
{
    public static string NeuerName()
    {
        return Guid.NewGuid().ToString();
    }

    public static ApplicationDbContext NeuerContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        return new ApplicationDbContext(options);
    }
}
