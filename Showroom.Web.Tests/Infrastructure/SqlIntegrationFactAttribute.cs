using Xunit;

namespace Showroom.Web.Tests.Infrastructure;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class SqlIntegrationFactAttribute : FactAttribute
{
    public SqlIntegrationFactAttribute()
    {
        var connectionString = Environment.GetEnvironmentVariable("SHOWROOM_TEST_SQL_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Skip = "SQL integration tests require SHOWROOM_TEST_SQL_CONNECTION_STRING.";
        }
    }
}
