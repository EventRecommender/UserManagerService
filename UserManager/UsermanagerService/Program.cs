using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UsermanagerService.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using UsermanagerService.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
var dBService = new DBService("");
Authenticator auth = new Authenticator();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapPost("/login", [AllowAnonymous] (string username, byte[] password) =>
{
    string issuer = builder.Configuration["JWT:Issuer"];
    string audience = builder.Configuration["JWT:Audience"];
    byte[] key = Encoding.ASCII.GetBytes
    (builder.Configuration["JWT:Key"]);

    try
    {
        User? user = auth.Login(username, password);
        
        if (user == null) { return Results.Unauthorized(); }

        string token = auth.GenerateToken(username, issuer, audience, key);

        UserAuth userAuth = new UserAuth(user.ID, user.username, user.role, token);

        return Results.Accepted(JsonSerializer.Serialize(userAuth));

    }
    catch (DatabaseException) { return Results.StatusCode(503); }
    catch (Exception ex) { return Results.Problem(detail:ex.Message); }
    
});

app.MapGet("/fetch", [Authorize] (int userID) =>
{ 
    try
    {
        User user = dBService.FetchUserFromID(userID);
        return Results.Accepted(JsonSerializer.Serialize(user));
    }
    catch (DatabaseException ) { return Results.StatusCode(503); }//return serice unavailable
    catch (InstanceException ex) { return Results.Problem(detail: $"Found {ex.Message} users"); }
});

app.MapGet("/verify", [Authorize] (string token) => { return Results.Accepted(); });

app.MapPut("/Create", [AllowAnonymous] (string username, byte[] password, string city, string institue, string role) =>
{
    UIntUser UnitializedUser = new UIntUser(username, city, institue, role);

    try
    {
        if (dBService.AddUser(UnitializedUser, password)){ return Results.Ok(true); }
        else { return Results.Conflict("User could not be created"); }
    }
    catch (DatabaseException ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/delete", [Authorize] (int userId) =>
{
    if (dBService.DeleteUser(userId))
    {
        return Results.Ok(true);
    }
    else{ return Results.NotFound(false); }
});

app.MapGet("/CreateToken", (string username, string password) =>
{

    if (username == "Test" && password == "test123")
    {
        var issuer = builder.Configuration["JWT:Issuer"];
        var audience = builder.Configuration["JWT:Audience"];
        var key = Encoding.ASCII.GetBytes
        (builder.Configuration["JWT:Key"]);

        var token = auth.GenerateToken(username, issuer, audience, key);
       
        return Results.Ok(token);
    }
    return Results.Unauthorized();

});

app.UseAuthentication();
app.UseAuthorization();
app.Run();

