    using Microsoft.AspNetCore.Builder;
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using UsermanagerService.Models;

    namespace UserManager
    {
        public class GenerateTokenTest
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
            public void GeneratesWithIssuerCorrect()
            {
                //Arrange
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                string Issuer = builder.Configuration["JWT:Issuer"];

                //Act
                string genToken = auth.GenerateToken("test");
                var token = handler.ReadToken(genToken);

                //Assert
                Assert.That(Issuer, Is.EqualTo(token.Issuer));

            }


           
        }

    }

