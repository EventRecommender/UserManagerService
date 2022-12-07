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

app.MapPost("/login", (string username, byte[] password) =>
{    
    try
    {
        var login = auth.Login(username, password);
        
        if (login == null) { return Results.Unauthorized(); }

        return Results.Accepted(JsonSerializer.Serialize(login));

    }
    catch (DatabaseException ex) { return Results.StatusCode(503); }
    catch (Exception ex) { return Results.Problem(detail:ex.Message); }
    
    
});

app.MapGet("/fetch", (int userID) =>
{ 
    try
    {
        User user = dBService.FetchUserFromID(userID);
        return Results.Accepted(JsonSerializer.Serialize(user));
    }
    catch (DatabaseException ex) { return Results.StatusCode(503); }//return serice unavailable
    catch (InstanceException ex) { return Results.Problem(detail: $"Found {ex.Message} users"); }
});

app.MapGet("/verify", (string token) =>
{
    return true;
});

app.MapPut("/Create", (string username, byte[] password, string city, string institue, string role) =>
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

app.MapPost("/delete", (int userId) =>
{
    if (dBService.DeleteUser(userId))
    {
        return Results.Ok(true);
    }
    else{ return Results.NotFound(false); }
});

app.MapPost("/CreateToken", (string username, string password) =>
{

    if (username == "Test" && password == "test123")
    {
        var issuer = builder.Configuration["JWT:Issuer"];
        var audience = builder.Configuration["JWT:Audience"];
        var key = Encoding.ASCII.GetBytes
        (builder.Configuration["JWT:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Id", Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Email, username),
                new Claim(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
             }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials
            (new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha512Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);
        var stringToken = tokenHandler.WriteToken(token);
        return Results.Ok(stringToken);
    }
    return Results.Unauthorized();

});
app.UseAuthentication();
app.UseAuthorization();
app.Run();

