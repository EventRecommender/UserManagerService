using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Text;
using UsermanagerService.Models;
namespace UserManager
{
    public class Tests
    {
        Authenticator auth { get; set; }
        String TestToken { get; set; }

        [SetUp]
        public void Setup()
        {
            var builder = WebApplication.CreateBuilder();
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
        public void isTokenValid_TokenGeneratedByAuthenticator_TokenIsAccepted()
        {
            //Arrange
            string token = TestToken;
            
            //Act
            bool valid = auth.isTokenValid(token);

            //Assert
            Assert.IsTrue(valid);
        }

        [Test]
        public void isTokenValid_ModifiedToken_TokenIsRejected()
        {
            //Arrange
            string token =  ScrambleString(TestToken);

            //Act

            bool valid = auth.isTokenValid(token);

            //Assert
            Assert.IsFalse(valid);
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