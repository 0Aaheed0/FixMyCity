using FixMyCity.Data;
using FixMyCity.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var mysqlPassword = builder.Configuration["MYSQL_PASSWORD"];
if (!string.IsNullOrWhiteSpace(mysqlPassword) && !string.IsNullOrWhiteSpace(connectionString))
{
    var mysqlConnection = new MySqlConnectionStringBuilder(connectionString)
    {
        Password = mysqlPassword
    };
    connectionString = mysqlConnection.ConnectionString;
}
builder.Services.AddDbContext<FixMyCityDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<FixMyCityDbContext>();

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Keep a fresh local installation usable by seeding the reference data required
// by the report form. Existing records are preserved.
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<FixMyCityDbContext>();
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

    // Populate a fresh local installation with representative civic activity.
    // This runs only when there are no reports and never overwrites user data.
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

    // Keep the local dashboard lively with a larger, repeat-safe demo stream.
    if (await database.Reports.CountAsync() < 15)
    {
        var demoUser = await database.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();
        var categories = await database.Categories.OrderBy(c => c.Id).Take(5).ToListAsync();
        if (demoUser != null && categories.Count >= 4)
        {
            var existingDemo = await database.Reports.Where(r => r.Description.StartsWith("Demo civic report ")).Select(r => r.Description).ToListAsync();
            var extraSamples = new[]
            {
                (0, "Demo civic report 01 - damaged road surface near school", "Reported"),
                (1, "Demo civic report 02 - signal outage at the north junction", "InProgress"),
                (2, "Demo civic report 03 - waste collection missed this week", "Resolved"),
                (3, "Demo civic report 04 - blocked drain beside the park", "Reported"),
                (4, "Demo civic report 05 - public light flickering at night", "InProgress"),
                (0, "Demo civic report 06 - uneven pavement near clinic", "Reported"),
                (2, "Demo civic report 07 - litter around bus shelter", "Resolved"),
                (3, "Demo civic report 08 - standing water after rainfall", "InProgress"),
                (1, "Demo civic report 09 - pedestrian crossing button broken", "Reported"),
                (4, "Demo civic report 10 - dark walkway near library", "Verified")
            };
            foreach (var sample in extraSamples.Where(s => !existingDemo.Contains(s.Item2)))
            {
                var issue = new Issue { CategoryId = categories[sample.Item1].Id, Status = sample.Item3, PriorityScore = sample.Item3 == "Resolved" || sample.Item3 == "Verified" ? 2 : 4 };
                database.Issues.Add(issue);
                await database.SaveChangesAsync();
                database.Reports.Add(new Report { Description = sample.Item2, CategoryId = categories[sample.Item1].Id, UserId = demoUser.Id, IssueId = issue.Id, Latitude = 23.77 + (sample.Item1 * .004), Longitude = 90.40 + (sample.Item1 * .004), CreatedAt = DateTime.UtcNow.AddHours(-existingDemo.Count - 2) });
            }
            await database.SaveChangesAsync();
        }
    }
}

app.Run();
