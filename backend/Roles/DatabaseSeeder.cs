using Marketify.Entites;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Marketify.Roles
{
    public static class DatabaseSeeder
    {
        private const string DevelopmentSuperAdminEmail = "test@gmail.com";
        private const string DevelopmentSuperAdminPassword = "Test@123";

        public static async Task SeedRolesAsync(
            IServiceProvider serviceProvider,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roleNames = { AppRoles.SuperAdmin, AppRoles.Admin, AppRoles.Customer };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (roleExist)
                {
                    logger?.LogInformation("Role {RoleName} already exists.", roleName);
                    continue;
                }

                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                    logger?.LogError("Failed to create role {RoleName}: {Errors}", roleName, errors);
                    throw new InvalidOperationException($"Failed to seed role '{roleName}': {errors}");
                }

                logger?.LogInformation("Created role {RoleName}.", roleName);
            }
        }

        public static async Task SeedDevelopmentSuperAdminAsync(
            IServiceProvider serviceProvider,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await EnsureRoleExistsAsync(roleManager, AppRoles.SuperAdmin, logger);

            var user = await userManager.FindByEmailAsync(DevelopmentSuperAdminEmail);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = DevelopmentSuperAdminEmail,
                    Email = DevelopmentSuperAdminEmail,
                    FirstName = "Development",
                    LastName = "SuperAdmin",
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, DevelopmentSuperAdminPassword);
                EnsureSucceeded(createResult, "create development SuperAdmin account", logger);

                var addToRoleResult = await userManager.AddToRoleAsync(user, AppRoles.SuperAdmin);
                EnsureSucceeded(addToRoleResult, "assign development SuperAdmin role", logger);

                logger?.LogInformation("✓ Development SuperAdmin created successfully.");
                return;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;

                var updateResult = await userManager.UpdateAsync(user);
                EnsureSucceeded(updateResult, "confirm development SuperAdmin email", logger);
            }

            if (!await userManager.IsInRoleAsync(user, AppRoles.SuperAdmin))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(user, AppRoles.SuperAdmin);
                EnsureSucceeded(addToRoleResult, "assign existing user to SuperAdmin role", logger);
            }

            logger?.LogInformation("✓ Development SuperAdmin already exists.");
        }

        private static async Task EnsureRoleExistsAsync(
            RoleManager<IdentityRole> roleManager,
            string roleName,
            ILogger? logger)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                return;
            }

            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            EnsureSucceeded(createRoleResult, $"create role '{roleName}'", logger);
            logger?.LogInformation("Created role {RoleName}.", roleName);
        }

        private static void EnsureSucceeded(
            IdentityResult result,
            string operation,
            ILogger? logger)
        {
            if (result.Succeeded)
            {
                return;
            }

            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            logger?.LogError("Failed to {Operation}: {Errors}", operation, errors);
            throw new InvalidOperationException($"Failed to {operation}: {errors}");
        }
    }
}
