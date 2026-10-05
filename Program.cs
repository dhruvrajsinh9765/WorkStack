using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkStack.Data;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICES
// ============================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(
        options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
        })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();


// ============================================================
// APPLICATION
// ============================================================

var app = builder.Build();


// ============================================================
// HTTP REQUEST PIPELINE
// ============================================================

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

// Authentication identifies the logged-in user.
app.UseAuthentication();

// Authorization checks whether that user is allowed
// to access the requested resource.
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();


// ============================================================
// DEMO DATA
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context =
        services.GetRequiredService<ApplicationDbContext>();

    var userManager =
        services.GetRequiredService<UserManager<IdentityUser>>();

    await DemoDataSeeder.SeedAsync(
        context,
        userManager);
}


app.Run();