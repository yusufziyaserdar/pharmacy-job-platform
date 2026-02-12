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
            var requiredRoles = new[] { "Worker", "PharmacyOwner", "Admin" };

            var existingRoles = context.Roles
                .Select(r => r.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingRoles = requiredRoles
                .Where(roleName => !existingRoles.Contains(roleName))
                .Select(roleName => new Role { Name = roleName })
                .ToList();

            if (missingRoles.Count > 0)
            {
                context.Roles.AddRange(missingRoles);
                await context.SaveChangesAsync();
            }
        }
    }
}
