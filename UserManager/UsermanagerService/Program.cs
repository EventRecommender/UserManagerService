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
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Ocsp;

var builder = WebApplication.CreateBuilder(args);
var dBService = new DBService(@"server=usermanager_database;userid=root;password=fuper;database=usermanager_db");
string issuer = builder.Configuration["JWT:Issuer"];
string audience = builder.Configuration["JWT:Audience"];
byte[] key = Encoding.ASCII.GetBytes
(builder.Configuration["JWT:Key"]);
Authenticator auth = new Authenticator(dBService,issuer, audience, key);

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

app.MapPost("/login", [AllowAnonymous] (LoginInput input) =>
{
    

    try
    {
        User? user = auth.Login(input.Username, input.Password);
        
        if (user == null) { return Results.Unauthorized(); }

        string token = auth.GenerateToken(user.username);

        UserAuth userAuth = new UserAuth(user.ID, user.username, user.role, token);

        return Results.Ok(JsonSerializer.Serialize(userAuth));
    }
    catch (DatabaseException) { return Results.StatusCode(503); }
    catch (Exception ex) { return Results.Problem(detail:ex.Message); }
  
});

app.MapGet("/fetch", [Authorize] (int userID) =>
{ 
    try
    {
        User user = dBService.FetchUserFromID(userID);
        return Results.Ok(JsonSerializer.Serialize(user));
    }
    catch (DatabaseException ) { return Results.StatusCode(503); }//return serice unavailable
    catch (InstanceException ex) { return Results.Problem(detail: $"Found {ex.Message} users"); }
});

app.MapGet("/verify", [Authorize] (HttpRequest req) => {

    string inputToken = req.Headers.Authorization.ToString().Split(" ")[1];

    if (auth.isTokenValid(inputToken)) { return Results.Ok(value: true); }

    return Results.BadRequest(false);

}).RequireAuthorization();

app.MapPut("/Create", [AllowAnonymous] (UIntUser UnitializedUser) =>
{
   
    try
    {
        int id = dBService.AddUser(UnitializedUser);
        if (id != -1){ return Results.Ok(id); }
        else { return Results.Conflict("User already exist"); }
    }
    catch (DatabaseException ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/delete", [Authorize] (int userId) =>
{
    try
    {
        if (dBService.DeleteUser(userId))
        {
            return Results.Ok(true);
        }
        else { return Results.NotFound(false); }
    }
    catch (DatabaseException ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/CreateToken", (LoginInput login) =>
{
    if (login.Username == "Test" && login.Password == "test123")
    {
        var token = auth.GenerateToken(login.Username);
        
        return Results.Ok(token);
    }
    return Results.Unauthorized();

});

app.MapGet("/FetchAllUsers", [Authorize]() =>
{
    try
    {
        List<User> users = dBService.FetchAllUsers();

        return Results.Ok(JsonSerializer.Serialize(users));
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
    
});

app.UseAuthentication();
app.UseAuthorization();
app.Run();

