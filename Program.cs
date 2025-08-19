// ---- START: ADD THESE USING STATEMENTS ----
using hrms.Data;
using Microsoft.EntityFrameworkCore;
// ---- END: ADD THESE USING STATEMENTS ----

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


// ---- START: ADD THIS DATABASE CONFIGURATION SECTION ----
// This is the crucial part that was missing.
// It reads your connection string and sets up the PostgreSQL database connection.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// ---- END: ADD THIS DATABASE CONFIGURATION SECTION ----


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();