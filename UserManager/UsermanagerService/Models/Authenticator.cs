using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using UsermanagerService.Exceptions;

namespace UsermanagerService.Models
{
    public class Authenticator
    {
        DBService DBService;
        private string Issuer;
        private string Audience;
        private byte[] Key;
        public Authenticator(DBService database, string issuer, string audience, byte[] key)
        {
            DBService = database;
            Issuer = issuer;
            Audience = audience;
            Key = key;
        }

        public User? Login(string username, string Inputpassword)
        {
            try
            {
                DBService.FetchUserAndPasswordFromUsername(username, out User user, out string password);

                if (Inputpassword == password)
                {
                    return user;
                }
                else { return null; }
                 
            }
            catch (InstanceException) { throw new Exception("0"); }
        }

        public void CreatePasswordHash(string password, out byte[] Hash, out byte[] Salt)
        {
            using (var hmac = new HMACSHA512())
            {
                Salt = hmac.Key;
                Hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        public byte[] HashPassword(string password, byte[] salt)
        {
            using (var hmac = new HMACSHA512(salt))
            {
                return hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        public string GenerateToken(string username)
        {
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
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = this.Issuer,
                Audience = this.Audience,
                SigningCredentials = new SigningCredentials
            (new SymmetricSecurityKey(this.Key),
            SecurityAlgorithms.HmacSha512Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);
            return jwtToken;
        }

        public bool isTokenValid(string token)
        {
            if(token == null) return false;

            var TokenHandler = new JwtSecurityTokenHandler();

            try
            {
                TokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(this.Key),
                    ValidAudience = this.Audience,
                    ValidIssuer = this.Issuer,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken); ;

                var jwToken = (JwtSecurityToken)validatedToken;

                return true;
            } catch (Exception) { return false; }
        }
    }
}
