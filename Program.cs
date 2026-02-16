using DocNotes.Data;
using DocNotes.Middlewares;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI;

namespace DocNotes
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options => { options.UseSqlServer(connectionString); });
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Build the actual web application from all the services we configured

            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            {
                // LOGIN SETTINGS
                options.SignIn.RequireConfirmedAccount = false;

                // PASSWORD SETTINGS (demo-friendly)
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;

                // 🔐 LOCKOUT SETTINGS (IMPORTANT)
                options.Lockout.AllowedForNewUsers = true;              // Enable lockout
                options.Lockout.MaxFailedAccessAttempts = 3;            // 3 wrong attempts
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromSeconds(30); // Freeze for 30 seconds
            })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });


            var app = builder.Build();


            app.UseMiddleware<ExceptionHandlingMiddleware>();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();




            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();


            // This block runs once when the app starts:
            // 1. Applies any pending migrations → creates tables if they don't exist
            // 2. Seeds roles and test users so you can log in immediately
            using (var scope = app.Services.CreateScope())
            {
                // Get our DbContext from the service provider
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();  // Automatically creates/updates database schema (tables)

                // Get managers for roles and users
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                // Array of role names we want to ensure exist
                string[] roles = { "Doctor", "Doctor" };

                // Create each role if it doesn't already exist
                foreach (var roleName in roles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Seed Doctor user if it doesn't exist
                var doctorEmail = "Dr.Sharma@clinic.com";
                var doctor = await userManager.FindByEmailAsync(doctorEmail);
                if (doctor == null)
                {
                    doctor = new IdentityUser
                    {
                        UserName = doctorEmail,
                        Email = doctorEmail,
                        EmailConfirmed = true  // Skip confirmation step for demo
                    };
                    var result = await userManager.CreateAsync(doctor, "Sharma@2026");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(doctor, "Doctor");
                    }
                }

                // Seed LabTech user if it doesn't exist
                var AdminEmail = "Dr.Mishra@clinic.com";
                var admin = await userManager.FindByEmailAsync(AdminEmail);
                if (admin == null)
                {
                    admin = new IdentityUser
                    {
                        UserName = AdminEmail,
                        Email = AdminEmail,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(admin, "Mishra@2026");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Doctor");
                    }
                }
            }


            app.Run();
        }
    }
}