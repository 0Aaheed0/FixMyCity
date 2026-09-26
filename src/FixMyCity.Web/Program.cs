using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FixMyCity.Data;
using FixMyCity.Data.Models;
using FixMyCity.Services.Common;
using FixMyCity.Services.Localization;
using FixMyCity.Services.Notifications;
using FixMyCity.Services.Payments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// Add MVC with views and Razor pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Localization configuration (English & Bangla)
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en-US"), new CultureInfo("bn-BD") };
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

// Database connection resolution with smart fallback
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
var mysqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? builder.Configuration["MYSQL_PASSWORD"];
if (mysqlPassword != null && !string.IsNullOrWhiteSpace(connectionString))
{
    var mysqlConnection = new MySqlConnectionStringBuilder(connectionString)
    {
        Password = mysqlPassword
    };
    connectionString = mysqlConnection.ConnectionString;
}

ServerVersion serverVersion;
try
{
    serverVersion = ServerVersion.AutoDetect(connectionString);
}
catch (MySqlConnector.MySqlException)
{
    // Fallback to empty password for standard local XAMPP setups
    var fallbackConnection = new MySqlConnectionStringBuilder(connectionString) { Password = string.Empty };
    serverVersion = ServerVersion.AutoDetect(fallbackConnection.ConnectionString);
    connectionString = fallbackConnection.ConnectionString;
}

builder.Services.AddDbContext<FixMyCityDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<FixMyCityDbContext>();

// Register Smart Municipal Services
builder.Services.AddSingleton<IGeoLocationService, GeoLocationService>();
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

// Payment Gateways
builder.Services.AddTransient<IPaymentGateway, BkashPaymentGateway>();
builder.Services.AddTransient<IPaymentGateway, NagadPaymentGateway>();
builder.Services.AddTransient<IPaymentGateway, RocketPaymentGateway>();
builder.Services.AddTransient<IPaymentGateway, CardPaymentGateway>();
builder.Services.AddTransient<IPaymentGateway, MockPaymentGateway>();
builder.Services.AddTransient<IPaymentGatewayFactory, PaymentGatewayFactory>();

// Scoped Domain Services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMunicipalBillingService, MunicipalBillingService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");
app.UseStaticFiles();

// Apply Request Localization
var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Database initialization & reference data seeding
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<FixMyCityDbContext>();
    await database.Database.MigrateAsync();

    // 1. Seed Municipal Departments & Categories
    if (!await database.Categories.AnyAsync())
    {
        var departments = new[]
        {
            new Department { Name = "Roads & Transport", Description = "Potholes, traffic signals and public roads" },
            new Department { Name = "Waste Management", Description = "Garbage collection and public cleanliness" },
            new Department { Name = "Water & Utilities", Description = "Leaks, drainage and water infrastructure" },
            new Department { Name = "Public Lighting", Description = "Streetlights and electrical civic assets" }
        };
        database.Departments.AddRange(departments);
        await database.SaveChangesAsync();

        database.Categories.AddRange(
            new Category { Name = "Pothole / Road Damage", DepartmentId = departments[0].Id, ResponsibleDepartment = departments[0].Name },
            new Category { Name = "Broken Traffic Signal", DepartmentId = departments[0].Id, ResponsibleDepartment = departments[0].Name },
            new Category { Name = "Garbage / Waste", DepartmentId = departments[1].Id, ResponsibleDepartment = departments[1].Name },
            new Category { Name = "Water Leak / Drainage", DepartmentId = departments[2].Id, ResponsibleDepartment = departments[2].Name },
            new Category { Name = "Broken Streetlight", DepartmentId = departments[3].Id, ResponsibleDepartment = departments[3].Name },
            new Category { Name = "Damaged Public Property", DepartmentId = departments[0].Id, ResponsibleDepartment = departments[0].Name }
        );
        await database.SaveChangesAsync();
    }

    // 2. Seed Municipal Services & Providers (Online Gateway)
    if (!await database.MunicipalServices.AnyAsync())
    {
        var electricity = new MunicipalService
        {
            Name = "Electricity",
            NameBn = "Electricity",
            Category = "Electricity",
            Description = "Pay pre-paid and post-paid electricity utility bills for Dhaka metropolitan area.",
            DescriptionBn = "Pay pre-paid and post-paid electricity utility bills for Dhaka metropolitan area.",
            IconClass = "bi-lightning-charge-fill",
            IsActive = true
        };

        var gas = new MunicipalService
        {
            Name = "Gas Utility",
            NameBn = "Gas Utility",
            Category = "Gas",
            Description = "Domestic and commercial piped gas bill settlement.",
            DescriptionBn = "Domestic and commercial piped gas bill settlement.",
            IconClass = "bi-fire",
            IsActive = true
        };

        var water = new MunicipalService
        {
            Name = "Water & Sewerage",
            NameBn = "Water & Sewerage",
            Category = "Water",
            Description = "Municipal water supply and drainage bill payments.",
            DescriptionBn = "Municipal water supply and drainage bill payments.",
            IconClass = "bi-droplet-fill",
            IsActive = true
        };

        var cityTax = new MunicipalService
        {
            Name = "City Corporation Tax",
            NameBn = "City Corporation Tax",
            Category = "CityTax",
            Description = "Holding tax, municipal property fees, and conservancy assessments.",
            DescriptionBn = "Holding tax, municipal property fees, and conservancy assessments.",
            IconClass = "bi-building-check",
            IsActive = true
        };

        var otherServices = new MunicipalService
        {
            Name = "Municipal Permits & Fees",
            NameBn = "Municipal Permits & Fees",
            Category = "Other",
            Description = "Trade licenses, waste collection fees, and civil certificates.",
            DescriptionBn = "Trade licenses, waste collection fees, and civil certificates.",
            IconClass = "bi-file-earmark-text-fill",
            IsActive = true
        };

        database.MunicipalServices.AddRange(electricity, gas, water, cityTax, otherServices);
        await database.SaveChangesAsync();

        // Seed Providers
        database.MunicipalProviders.AddRange(
            new MunicipalProvider { Name = "DPDC", NameBn = "DPDC", Code = "DPDC", ServiceId = electricity.Id, AccountNumberLabel = "Customer No (8 Digits)", AccountNumberLabelBn = "Customer No (8 Digits)", IsActive = true },
            new MunicipalProvider { Name = "DESCO", NameBn = "DESCO", Code = "DESCO", ServiceId = electricity.Id, AccountNumberLabel = "Account No (8 Digits)", AccountNumberLabelBn = "Account No (8 Digits)", IsActive = true },
            new MunicipalProvider { Name = "Titas Gas", NameBn = "Titas Gas", Code = "TITAS", ServiceId = gas.Id, AccountNumberLabel = "Consumer Code (10 Digits)", AccountNumberLabelBn = "Consumer Code (10 Digits)", IsActive = true },
            new MunicipalProvider { Name = "Dhaka WASA", NameBn = "Dhaka WASA", Code = "WASA", ServiceId = water.Id, AccountNumberLabel = "WASA Account No", AccountNumberLabelBn = "WASA Account No", IsActive = true },
            new MunicipalProvider { Name = "DNCC Tax (North)", NameBn = "DNCC Tax (North)", Code = "DNCC_TAX", ServiceId = cityTax.Id, AccountNumberLabel = "Holding Number (Ward-Road-Holding)", AccountNumberLabelBn = "Holding Number", IsActive = true },
            new MunicipalProvider { Name = "DSCC Tax (South)", NameBn = "DSCC Tax (South)", Code = "DSCC_TAX", ServiceId = cityTax.Id, AccountNumberLabel = "Holding Number", AccountNumberLabelBn = "Holding Number", IsActive = true },
            new MunicipalProvider { Name = "Trade License Renewal", NameBn = "Trade License Renewal", Code = "TRADE_LIC", ServiceId = otherServices.Id, AccountNumberLabel = "License Serial No", AccountNumberLabelBn = "License Serial No", IsActive = true }
        );
        await database.SaveChangesAsync();
    }

    // 3. Seed Sample Announcements
    if (!await database.Announcements.AnyAsync())
    {
        database.Announcements.AddRange(
            new Announcement
            {
                Title = "Emergency Road Maintenance on Mirpur Road",
                TitleBn = "Emergency Road Maintenance on Mirpur Road",
                Description = "Road repair and resurfacing work will be underway between Asad Gate and Technical Junction from 10:00 PM to 6:00 AM. Please use alternate routes.",
                DescriptionBn = "Road repair and resurfacing work will be underway between Asad Gate and Technical Junction from 10:00 PM to 6:00 AM. Please use alternate routes.",
                Category = "RoadClosure",
                Priority = "High",
                Latitude = 23.7712,
                Longitude = 90.3621,
                RadiusKm = 2.0,
                Status = "Active",
                StartDate = DateTime.UtcNow.AddHours(-2),
                ExpiryDate = DateTime.UtcNow.AddDays(3),
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Announcement
            {
                Title = "Scheduled Water Supply Pipeline Cleaning in Dhanmondi",
                TitleBn = "Scheduled Water Supply Pipeline Cleaning in Dhanmondi",
                Description = "Dhaka WASA will conduct pipeline flushing in Dhanmondi Wards 15 & 16. Temporary low pressure may occur between 1:00 PM and 5:00 PM.",
                DescriptionBn = "Dhaka WASA will conduct pipeline flushing in Dhanmondi Wards 15 & 16. Temporary low pressure may occur between 1:00 PM and 5:00 PM.",
                Category = "WaterSupply",
                Priority = "Normal",
                Latitude = 23.7461,
                Longitude = 90.3742,
                RadiusKm = 2.0,
                Status = "Active",
                StartDate = DateTime.UtcNow.AddHours(-5),
                ExpiryDate = DateTime.UtcNow.AddDays(2),
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            }
        );
        await database.SaveChangesAsync();
    }

    // 4. Seed Active Emergency Alert (City-wide)
    if (!await database.EmergencyAlerts.AnyAsync())
    {
        database.EmergencyAlerts.Add(new EmergencyAlert
        {
            Title = "Heavy Monsoon Rain & Drainage Warning Across Metropolitan Areas",
            TitleBn = "Heavy Monsoon Rain & Drainage Warning Across Metropolitan Areas",
            Description = "Met office forecasts intensive rain for the next 24 hours. Municipal quick-response pumping crews have been stationed at major junctions. Drive carefully and avoid downed electrical lines.",
            DescriptionBn = "Met office forecasts intensive rain for the next 24 hours. Municipal quick-response pumping crews have been stationed at major junctions. Drive carefully and avoid downed electrical lines.",
            Severity = "Critical",
            StartAt = DateTime.UtcNow.AddHours(-1),
            ExpireAt = DateTime.UtcNow.AddDays(2),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        });
        await database.SaveChangesAsync();
    }

    // 5. Seed Lost & Found Community Posts
    if (!await database.LostFoundPosts.AnyAsync())
    {
        var demoUser = await database.Users.FirstOrDefaultAsync();
        var demoUserId = demoUser?.Id ?? "system-seed";

        database.LostFoundPosts.AddRange(
            new LostFoundPost
            {
                PostType = "Lost",
                Title = "Lost Black Samsung Galaxy S23",
                TitleBn = "Lost Black Samsung Galaxy S23",
                Description = "Left on passenger seat in a CNG auto-rickshaw near Dhanmondi 27. Black protective case with national ID photocopy inside back cover.",
                DescriptionBn = "Left on passenger seat in a CNG auto-rickshaw near Dhanmondi 27. Black protective case with national ID photocopy inside back cover.",
                Category = "MobilePhone",
                EventDate = DateTime.UtcNow.AddDays(-1),
                LocationName = "Dhanmondi 27 near Rapa Plaza",
                Latitude = 23.7540,
                Longitude = 90.3768,
                ContactInfo = "01711-000001",
                Status = "Active",
                UserId = demoUserId,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new LostFoundPost
            {
                PostType = "Found",
                Title = "Found Brown Leather Wallet",
                TitleBn = "Found Brown Leather Wallet",
                Description = "Found near Farmgate metro station walkway containing university student card and minor cash. Safe with the metro security booth.",
                DescriptionBn = "Found near Farmgate metro station walkway containing university student card and minor cash. Safe with the metro security booth.",
                Category = "Wallet",
                EventDate = DateTime.UtcNow.AddHours(-8),
                LocationName = "Farmgate Metro Station Exit A",
                Latitude = 23.7561,
                Longitude = 90.3872,
                ContactInfo = "Inquire at Station Booth",
                Status = "Active",
                UserId = demoUserId,
                CreatedAt = DateTime.UtcNow.AddHours(-8)
            }
        );
        await database.SaveChangesAsync();
    }

    // 6. Seed Initial Civic Reports (if needed)
    if (!await database.Reports.AnyAsync(r => r.Description == "Deep pothole beside the main bus stop"))
    {
        var demoUser = await database.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();
        var categories = await database.Categories.OrderBy(c => c.Id).Take(5).ToListAsync();
        if (demoUser != null && categories.Count >= 4)
        {
            var samples = new[]
            {
                (0, "Deep pothole beside the main bus stop", "Reported"),
                (1, "Traffic signal timing is unsafe at the crossing", "InProgress"),
                (2, "Overflowing waste bins near the community market", "Resolved"),
                (3, "Drainage leak flooding the footpath after rain", "Reported"),
                (4, "Streetlight is not working on the east lane", "Verified")
            };
            foreach (var sample in samples)
            {
                var issue = new Issue { CategoryId = categories[sample.Item1].Id, Status = sample.Item3, PriorityScore = sample.Item3 == "Resolved" || sample.Item3 == "Verified" ? 2 : 5 };
                database.Issues.Add(issue);
                await database.SaveChangesAsync();
                database.Reports.Add(new Report { Description = sample.Item2, CategoryId = categories[sample.Item1].Id, UserId = demoUser.Id, IssueId = issue.Id, Latitude = 23.7806 + sample.Item1 * .001, Longitude = 90.4070 + sample.Item1 * .001, CreatedAt = DateTime.UtcNow.AddDays(-sample.Item1 - 1) });
            }
            await database.SaveChangesAsync();
        }
    }

    // 7. Seed Identity Roles and Default Test Accounts for All 4 Roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var roles = new[] { "Administrator", "DepartmentManager", "DepartmentStaff", "Citizen" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    async Task EnsureUserAsync(string email, string password, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                Role = role,
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
        else
        {
            bool modified = false;
            if (user.Role != role)
            {
                user.Role = role;
                modified = true;
            }
            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                user.FullName = fullName;
                modified = true;
            }
            if (modified)
            {
                await userManager.UpdateAsync(user);
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, token, password);

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }

    await EnsureUserAsync("admin@fixmycity.com", "Admin@12345", "System Administrator", "Administrator");
    await EnsureUserAsync("noman.cse.20230104088@aust.edu", "Admin@12345", "Abdullah Al Noman (Admin)", "Administrator");
    await EnsureUserAsync("manager@fixmycity.com", "Manager@12345", "Operations Department Manager", "DepartmentManager");
    await EnsureUserAsync("staff@fixmycity.com", "Staff@12345", "Field Inspection & Action Staff", "DepartmentStaff");
    await EnsureUserAsync("citizen@fixmycity.com", "Citizen@12345", "Civic Community Member", "Citizen");
}

app.Run();
