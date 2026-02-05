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
MongoConnectionURISelect(builder, mongoConnectionLocallHost, mongoConnectionAtlas);

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
        policy.WithOrigins("http://localhost:5173")
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// FileHandler – uses Cloudinary + config-driven validation
builder.Services.AddScoped<FileHandler>();
var app = builder.Build();
app.UseCors("AllowLocalClient");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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
static void MongoConnectionURISelect(WebApplicationBuilder builder, string? mongoConnectionLocallHost, string? mongoConnectionAtlas)
{
    if (!string.IsNullOrEmpty(mongoConnectionAtlas))
    {
        builder.Services.AddSingleton(new DbManager(mongoConnectionAtlas));
    }
    else if (!string.IsNullOrEmpty(mongoConnectionLocallHost))
    {
        builder.Services.AddSingleton(new DbManager(mongoConnectionLocallHost));
    }
}