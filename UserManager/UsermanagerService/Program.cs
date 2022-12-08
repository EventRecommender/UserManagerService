using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UsermanagerService.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using UsermanagerService.Exceptions;

var builder = WebApplication.CreateBuilder(args);
var DB = new DBService(@"server=usermanager_database;userid=root;password=fuper;database=usermanager_db");

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


builder.Services.AddAuthorization();

var app = builder.Build();

app.MapGet("/login", [AllowAnonymous](string username, string password) =>
{
    
    try
    {
        User user = DB.ContainsCredentials(username, password);
        string token = DB.GenerateToken();
        Tuple<User, string> tuple = new Tuple<User, string>(user, token);

        return Results.Ok(JsonSerializer.Serialize(tuple));
      
    }
    catch (DatabaseException ex) { return Results.StatusCode(503); }
    catch (MultipleInstanceException ex) { return Results.Problem($"Found {ex.Message} users"); }
    catch (NoInstanceException) { return Results.Unauthorized(); }
    
});

app.MapGet("/fetch", (int userID) =>
{
    try
    {
        return Results.Ok(JsonSerializer.Serialize(DB.FetchUser(userID)));
    }
    catch (DatabaseException ex){ return Results.StatusCode(503); }//return serice unavailable
    catch (MultipleInstanceException ex) { return Results.Problem(ex.Message); }
    catch (NoInstanceException) { return Results.NotFound(false); }
}).RequireAuthorization();

app.MapGet("/verify", () =>
{
    return true;
}).RequireAuthorization();

app.MapPut("/Create", [AllowAnonymous] (string username, string password, string city, string institue, string role) =>
{
    User userObject = new User(0, username, city, institue, role);
    try
    {
        if (DB.AddNewUser(userObject, password))
        {
            return Results.Ok(true);
        }
        else { return Results.Conflict("User could not be created"); }
    }
    catch (DatabaseException ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/delete", (int userId) =>
{
    if (DB.DeleteUser(userId))
    {
        return Results.Ok(true);
    }
    else{ return Results.NotFound(false); }
});


app.UseAuthentication();
app.UseAuthorization();
app.Run();