using EduHealth.Data.Entities;
using EduHealth.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Data.Seeders
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(AppDbContext context)
        {
            if (await context.Users.AnyAsync())
            {
                return;
            }

            var admin = new User
            {
                Phone = "0900000001",
                Role = "ADMIN",
                FullName = "System Admin",
                IsActive = true,
                Email = "admin@eduhealth.local",
                PasswordHash = PasswordHelper.HashPassword("123456Aa@"),
                Avatar = null
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}