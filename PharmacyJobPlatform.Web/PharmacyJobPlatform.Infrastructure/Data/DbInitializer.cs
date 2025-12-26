using PharmacyJobPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmacyJobPlatform.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAsync(ApplicationDbContext context)
        {
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { Name = "Worker" },
                    new Role { Name = "PharmacyOwner" }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}
