using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using PharmacyJobPlatform.Domain.Entities;


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

        public static async Task SeedSystemUserAsync(ApplicationDbContext context)
        {
            const string systemEmail = "system@pharmacyjobplatform.local";

            if (await context.Users.AnyAsync(u => u.Email == systemEmail))
            {
                return;
            }

            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
            {
                return;
            }

            context.Users.Add(new User
            {
                FirstName = "Platform",
                LastName = "Sistem",
                Email = systemEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                PhoneNumber = "0000000000",
                RoleId = adminRole.Id,
                EmailConfirmed = true
            });

            await context.SaveChangesAsync();
        }
    }
}
