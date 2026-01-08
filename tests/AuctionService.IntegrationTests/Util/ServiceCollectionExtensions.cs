using System;
using AuctionService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests.Util;

public static class ServiceCollectionExtensions
{
    public static void RemoveDbContext<T>(this IServiceCollection services) where T : DbContext
    {
       var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<T>)); 

            if (descriptor is not null) services.Remove(descriptor);
    }

    public static void EnsureCreated<T>(this IServiceProvider services)  where T : DbContext
    {
                using var scope = services.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<T>();
                
                db.Database.Migrate();
                if (db is AuctionDbContext auctionDb)
                {
                    DBHelper.InitiDbForTests(auctionDb);
                }
    }

    // Convenience overload for calling during ConfigureTestServices where IServiceCollection is available
    public static void EnsureCreated<T>(this IServiceCollection services) where T : DbContext
    {
                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<T>();
                
                db.Database.Migrate();
                if (db is AuctionDbContext auctionDb)
                {
                    DBHelper.InitiDbForTests(auctionDb);
                }
    }
}
