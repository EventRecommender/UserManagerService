using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UsermanagerService.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);
var DB = new DBService("");

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapGet("/login", [AllowAnonymous](string username, string password) =>
{
    try
    {
        User? user = DB.ContainsCredentials(username, password);
        if (user != null)
        {
            return JsonSerializer.Serialize(new Tuple<User, string>(user, DB.GenerateToken(builder)));
            
        }
        else
        {
            return "false";
        }
    }
    catch
    {
       return "Unavailable";
    }
    
});

app.MapGet("/fetch", (int userID) =>
{
    try
    {
        return JsonSerializer.Serialize(DB.FetchUser(userID));
    }
    catch (Exception ex)
    {
        return ex.Message; 
    }
}).RequireAuthorization();

app.MapGet("/verify", () =>
{
    return true;
}).RequireAuthorization();

app.MapPost("/Create", [AllowAnonymous] (string username, string password, string city, string institue, string role) =>
{
    User userObject = new User(0, username, city, institue, role);
    if (DB.AddNewUser(userObject, password))
    {
        return true;
    }else { return false; }

});

app.MapPost("/delete", (int userId) =>
{
    if (DB.DeleteUser(userId))
    {
        return true;
    }
    else{ return false; }
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

app.UseAuthentication();
app.UseAuthorization();
app.Run();