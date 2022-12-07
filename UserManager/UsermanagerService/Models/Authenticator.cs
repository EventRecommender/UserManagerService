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
        DBService dBService = new DBService("");
        private readonly IConfiguration _configuration;
        public Authenticator(IConfiguration configuration)
        {
        _configuration = configuration;
        }

        public Tuple<User, string> Login (string username, byte[] Inputpassword)
        {
            try
            {
                dBService.FetchUserAndPasswordFromUsername(username, out User user, out byte[] password);

                if(Inputpassword == password)
                {
                    return new Tuple<User, string>(user, GenerateToken(user));
                }
                else { return null; }
                
            }
            catch (NoInstanceException) { throw new Exception("User not found"); }
            catch (MultipleInstanceException) { throw new Exception("Multiple users found"); } //This should never happen

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

        public string GenerateToken(User user)
        {
            List<Claim> claims = new List<Claim> { new Claim(ClaimTypes.Name, user.username) };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("JWT:Token").Value));

            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: cred);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
}
