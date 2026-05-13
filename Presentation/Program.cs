using Data.Contexts;
using Data.Entities;
using Data.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection")));
builder.Services.AddIdentity<UserEntity, IdentityRole>(x =>
{
    x.User.RequireUniqueEmail = true;
    x.Password.RequiredLength = 8;
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(x =>
{
    x.LoginPath = "auth/login";
    x.AccessDeniedPath = "auth/accessdenied";
    x.Cookie.HttpOnly = true;
    x.Cookie.IsEssential = true;
    x.Cookie.Expiration = TimeSpan.FromHours(1);
    x.SlidingExpiration = true;
});

builder.Services.AddScoped<IEventAttendanceRepository, EventAttendanceRepository>();
builder.Services.AddScoped<IEventChatRepository, EventChatRepository>();
builder.Services.AddScoped<IEventItemAssignmentRepository, EventItemAssignmentRepository>();
builder.Services.AddScoped<IEventItemRepository, EventItemRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventRoleRepository, EventRoleRepository>();
builder.Services.AddScoped<IEventTaskAssignmentRepository, EventTaskAssignmentRepository>();
builder.Services.AddScoped<IEventTaskRepository, EventTaskRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();
app.UseHsts();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.UseRewriter(new RewriteOptions().AddRedirect("^$", "/index"));
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
