using Microsoft.EntityFrameworkCore;

namespace HVO.Enterprise.Telemetry.Data.EfCore.Tests.Helpers
{
    /// <summary>
    /// Minimal <see cref="DbContext"/> used only by integration connectivity tests
    /// to verify the EF Core telemetry interceptor against a real database.
    /// </summary>
    internal sealed class IntegrationTestDbContext : DbContext
    {
        public IntegrationTestDbContext(DbContextOptions<IntegrationTestDbContext> options)
            : base(options) { }
    }
}
