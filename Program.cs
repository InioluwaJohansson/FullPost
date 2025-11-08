using System.Text;
using FullPost;
using FullPost.Authentication;
using FullPost.Context;
using FullPost.Implementations.Respositories;
using FullPost.Implementations.Services;
using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(x => x.AddPolicy("FullPost", c =>
{
    c.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin();
}));
// Add services to the container.
builder.Services.AddScoped<ISubscriptionPlanRepo, SubscriptionPlanRepo>();
builder.Services.AddScoped<IUserSubscriptionRepo, UserSubscriptionRepo>();
builder.Services.AddScoped<IPostRepo, PostRepo>();
builder.Services.AddScoped<ICustomerRepo, CustomerRepo>();
builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<ITwitterService, TwitterService>();
builder.Services.AddScoped<ITwitterService, TwitterService>();
builder.Services.AddScoped<IFacebookService, FacebookService>();
builder.Services.AddScoped<IInstagramService, InstagramService>();
builder.Services.AddScoped<IYouTubeService, YouTubeService>();
builder.Services.AddScoped<ITikTokService, TikTokService>();
builder.Services.AddScoped<ILinkedInService, LinkedInService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJWTAuthentication, JWTAuthentication>();
builder.Services.AddHostedService<FullPostBackgroundService>();
builder.Services.AddHttpContextAccessor();
var connectionString = builder.Configuration.GetConnectionString("FullPostContext");
connectionString = $"Server={Environment.GetEnvironmentVariable("MYSQLHOST")};Port={Environment.GetEnvironmentVariable("MYSQLPORT")};Database={Environment.GetEnvironmentVariable("MYSQLDATABASE")};User={Environment.GetEnvironmentVariable("MYSQLUSER")};Password={Environment.GetEnvironmentVariable("MYSQLPASSWORD")};";
Console.WriteLine($"Connection String: {connectionString}");
builder.Services.AddDbContext<FullPostContext>(c => c.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
}).AddCookie().AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "FullPost", Version = "v1" });
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FullPostContext>();
    if (db.Database.GetPendingMigrations().Any()) db.Database.Migrate();

}
app.Use(async (context, next) =>
{
    Console.WriteLine($"➡️ Incoming Request: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
});
// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
app.UseSwagger();

app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("FullPost");

app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();