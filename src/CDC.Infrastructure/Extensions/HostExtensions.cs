using CDC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CDC.Infrastructure.Extensions;


public static class HostExtensions
{
    public static IApplicationBuilder ApplyMigration(this IApplicationBuilder app)
    {
        using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetService<CdcDbContext>();
            context.Database.Migrate();
        }
        return app;
    }

}
