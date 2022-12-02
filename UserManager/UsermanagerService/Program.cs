using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UsermanagerService.Models;

var builder = WebApplication.CreateBuilder(args);
var DB = new DBService();

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", [AllowAnonymous](string username, string password) =>
{
    if(DB.ContainsCredentials(username, password))
    {
        return true;
    }
    else
    {
        return false;
    }

});

app.MapGet("/fetch", (int userID) =>
{
    
}).RequireAuthorization();

app.MapGet("/verify", () =>
{
    return true;
}).RequireAuthorization();

app.MapPost("/Create", [AllowAnonymous] (string username, string password, string city, string institue, string role) =>
{

});
  

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});

app.Run();