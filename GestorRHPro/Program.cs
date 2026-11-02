using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestorRHPro.Data;
using GestorRHPro.Models;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=gestorrhpro.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads")
        && !context.User.IsInRole("Administrator")
        && !context.User.IsInRole("HRManager"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    foreach (var r in new[] { "Administrator", "HRManager", "Employee" })
        if (!await roleManager.RoleExistsAsync(r)) await roleManager.CreateAsync(new IdentityRole(r));

    var adminEmail = builder.Configuration["BootstrapAdmin:Email"];
    var adminPassword = builder.Configuration["BootstrapAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrador",
                Role = "Administrator",
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Não foi possível criar a conta inicial: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Administrator"))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, "Administrator");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException("Não foi possível atribuir a função de administrador à conta inicial.");
        }
    }

    // Invalidar a conta de desenvolvimento com palavra-passe fixa criada por versões anteriores.
    var legacyAdmin = await userManager.FindByEmailAsync("admin@gestorrhpro.local");
    if (legacyAdmin is not null)
    {
        if (string.Equals(adminEmail, legacyAdmin.Email, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(legacyAdmin);
            var resetResult = await userManager.ResetPasswordAsync(legacyAdmin, token, adminPassword);
            if (!resetResult.Succeeded)
                throw new InvalidOperationException("Não foi possível substituir a palavra-passe da antiga conta inicial.");
        }
        else
        {
            var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            legacyAdmin.PasswordHash = userManager.PasswordHasher.HashPassword(legacyAdmin, randomPassword);
            var updateResult = await userManager.UpdateAsync(legacyAdmin);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException("Não foi possível invalidar a antiga conta inicial.");
            await userManager.UpdateSecurityStampAsync(legacyAdmin);
        }
    }
}

app.Run();
