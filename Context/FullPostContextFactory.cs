using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace FullPost.Context;
public class FullPostContextFactory : IDesignTimeDbContextFactory<FullPostContext>
{
    public FullPostContext CreateDbContext(string[] args)
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json")
            .Build();
        var connectionString = config.GetConnectionString("FullPostContext");
        var optionsBuilder = new DbContextOptionsBuilder<FullPostContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new FullPostContext(optionsBuilder.Options);
    }
}