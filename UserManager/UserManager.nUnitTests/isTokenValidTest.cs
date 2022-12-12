using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Text;
using UsermanagerService.Models;
using System.IdentityModel.Tokens.Jwt;
namespace UserManager
{
    public class isTokenValidTest
    {
        Authenticator auth { get; set; }
        String TestToken { get; set; }

        WebApplicationBuilder builder { get; set; }
        [SetUp]
        public void Setup()
        {
            builder = WebApplication.CreateBuilder();
            var dBService = new DBService(@"server=usermanager_database;userid=root;password=fuper;database=usermanager_db");
            string issuer = builder.Configuration["JWT:Issuer"];
            string audience = builder.Configuration["JWT:Audience"];
            byte[] key = Encoding.ASCII.GetBytes
            (builder.Configuration["JWT:Key"]);
            auth = new Authenticator(dBService, issuer, audience, key);
            string username = "test";
            TestToken = auth.GenerateToken(username);
            
        }

        [Test]
        public void TokenGeneratedByAuthenticator_TokenIsAccepted()
        {
            //Arrange
            string token = TestToken;
            
            //Act
            bool validity = auth.isTokenValid(token);

            //Assert
            Assert.IsTrue(validity);
        }

        [Test]
        public void ModifiedToken_TokenIsRejected()
        {
            //Arrange
            string token =  ScrambleString(TestToken);

            //Act

            bool validity = auth.isTokenValid(token);

            //Assert
            Assert.IsFalse(validity);
        }

        [Test]
        public void RandomString_TokenIsRejected()
        {
            //Arrange
            string randomString = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJJZCI6IjIwZDEyY6jOjLTRmMWUtNGRmNy04YWRjLTU1YTdiZjNmOTJmOCIsInN1YiI6IlRlc3QiLCJlbWFpbCI6IlRlc3QiLCJqdGkiOiI4ZjNhNmJjOC02OTFlLTQxZTEtOGMxNC1mMDg1OWM2ZGVkYzEiLCJuYmYiOjE2NzA4MzgwNzMsImV4cCI6MTY3MDg0MTY3MywiaWF0IjoxNjcwODM4MDczLCJpc3MiOiJVc2VyTWFuYWdlclNlcnZpY2UyMjAyIiwiYXVkIjoiRXZlbnRSZWNvbW1lbmRlclN5c3RlbSJ9.ZOxe8cBBat0GpKk3qDIp_xKSrzoYfe6zAf6DXjyUzsGAEgzC-aWt4Kw0BRYK3hUT0VKen62CdgGnhNXnSGfxbg";
            
            //Act
            bool validity = auth.isTokenValid(randomString); 
            
            //Assert
            Assert.IsFalse(validity);

        }

        private string ScrambleString(string s)
        {
            char[] chars = new char[s.Length];
            Random random = new Random();
            int index = 0;
            while (s.Length > 0)
            { // Get a random number between 0 and the length of the word. 
                int next = random.Next(0, s.Length - 1); // Take the character from the random position 
                                                          //and add to our char array. 
                chars[index] = s[next];                // Remove the character from the word. 
                s = s.Substring(0, next) + s.Substring(next + 1);
                ++index;
            }
            return new String(chars);

        }
    }   
        
}