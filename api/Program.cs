using api.Config;
using api.Middleware;
using Microsoft.Extensions.Options;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var mongoConnectionLocallHost = builder.Configuration.GetConnectionString("MongoDb");
var mongoConnectionAtlas = builder.Configuration.GetConnectionString("AtlasURI");
var databaseName = builder.Configuration.GetConnectionString("DBName");
MongoConnectionURISelect(builder, mongoConnectionLocallHost, mongoConnectionAtlas, databaseName);

// bind config objects
var jwtConfig = builder.Configuration.GetSection("JWT").Get<JWTConfig>();
builder.Services.AddSingleton(jwtConfig);
builder.Services.AddSingleton(builder.Configuration.GetSection("AccessToken").Get<AccessTokens>());
builder.Services.AddSingleton(builder.Configuration.GetSection("RefreshToken").Get<RefreshTokens>());
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig?.Key ?? string.Empty)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token) && context.Request.Cookies.TryGetValue("PFToken", out var cookieToken))
            {
                context.Token = cookieToken;
            }
            return Task.CompletedTask;
        }
    };
});



// Bind the "Cloudinary" section to FileConfig via options
builder.Services.Configure<FileConfig>(builder.Configuration.GetSection("Cloudinary"));

// Cloudinary client (secure HTTPS URLs)
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<FileConfig>>().Value;
    var account = new Account(cfg.CloudName, cfg.APIKey, cfg.APISrcset);
    return new Cloudinary(account) { Api = { Secure = true } };
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://websocket-api-codebybiswajit.onrender.com")
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddScoped<FileHandler>();
builder.Services.AddScoped<GetAuth>();
var app = builder.Build();
app.UseCors("AllowLocalClient");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

}
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAuthorization();

app.MapControllers();

app.Run();
static void MongoConnectionURISelect(WebApplicationBuilder builder, string? mongoConnectionLocallHost, string? mongoConnectionAtlas, string? databaseName)
{
    if (!string.IsNullOrEmpty(mongoConnectionAtlas) && !string.IsNullOrEmpty(databaseName))
    {
        builder.Services.AddSingleton(new DbManager(mongoConnectionAtlas, databaseName));
    }
    else if (!string.IsNullOrEmpty(mongoConnectionLocallHost) && !string.IsNullOrEmpty(databaseName))
    {
        builder.Services.AddSingleton(new DbManager(mongoConnectionLocallHost, databaseName));    
    }
}