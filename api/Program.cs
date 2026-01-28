using api.Config;
using Microsoft.AspNetCore.Builder;
using User;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var mongoConnectionLocallHost = builder.Configuration.GetConnectionString("MongoDb");
var mongoConnectionAtlas = builder.Configuration.GetConnectionString("AtlasURI");
MongoConnectionURISelect(builder, mongoConnectionLocallHost, mongoConnectionAtlas);



//builder.Services.AddAuthorization();
builder.Configuration.GetRequiredSection("ConnectionStrings");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

}
app.UseHttpsRedirection();

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