using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace UsermanagerService.Models
{
    public class DBService
    {
        private string URL;

        public DBService(string uRL)
        {
            URL = uRL;
        }

        public User? ContainsCredentials(string username, string password)
        {   
            List<User> users = new List<User>();
            string query = $"SELECT id username city institute role FROM user JOIN password WHERE username = {username} AND password ={password};";
            SqlConnection con = new SqlConnection(URL);
            SqlCommand command = new SqlCommand(query, con);
            con.Open();
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]));
            }

            reader.Close();
            command.Dispose();
            con.Close();

            if(users.Count == 1) {
                return users[0];
            }
            else
            {
                return null;
            }

            
        }

        public User FetchUser (int userId)
        {
            List<User> users = new List<User>();
            string query = $"SELECT * from user WHERE id = {userId}";

            SqlConnection con = new SqlConnection(this.URL);
            SqlCommand command = new SqlCommand(query, con);
            con.Open();
            SqlDataReader reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                users.Add(new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]));
            }
            reader.Close();
            command.Dispose();
            con.Close();

            if(users != null)
            {
                if(users.Count > 1) { throw new Exception("USERID not unique"); }
                else
                {
                    return users[0];
                }
            }
            else
            {
                throw new Exception("User not found in database");
            }

        }

        public bool DeleteUser(int userId)
        {
            try
            {
                string query1 = $"DELETE FROM user WHERE id = {userId};";
                string query2 = $"DELETE FROM password WHERE id = {userId};";
                SqlConnection con = new SqlConnection(URL);
                SqlCommand delUsercommand = new SqlCommand(query1, con);
                SqlCommand delpasswordcommand = new SqlCommand(query2, con);
                con.Open();

                delUsercommand.ExecuteNonQuery();
                delpasswordcommand.ExecuteNonQuery();
                return true;
            }
            catch {

                //Implement SQL ROLLBACK
                return false;
            }
            

        }

        public bool AddNewUser(User user, string password)
        {
            try
            {
                List<int> ids = new List<int>();



                string insertQuery = $"INSERT INTO user ({user.username}, {user.city}, {user.institute}, {user.role})";
                string getIDQuery = $"SELECT id FROM user WHERE username = {user.username} city = {user.city} institute = {user.institute} role  = {user.role}";
                string passwordQuery = $"INSERT INTO password ({ids[0]},{password}";
                SqlConnection con = new SqlConnection(URL);
                SqlCommand insertCommand = new SqlCommand(insertQuery, con);
                SqlCommand IdCommand = new SqlCommand(getIDQuery, con);
                SqlCommand passwordCommand = new SqlCommand(passwordQuery, con);
                
                insertCommand.ExecuteNonQuery();
                SqlDataReader reader = IdCommand.ExecuteReader();

                while (reader.Read())
                {
                    ids.Add((int)reader[0]);
                }

                if (ids.Count != 1) { throw new Exception("Too many id"); }
                else
                {
                    passwordCommand.ExecuteNonQuery();
                }

                reader.Close();
                insertCommand.Dispose();
                IdCommand.Dispose();
                passwordCommand.Dispose();
                con.Close();

                return true;
            }catch
            {
                //Implement SQL ROLLBACK
                return false;
            }
            
        }
       

        public string GenerateToken(WebApplicationBuilder builder)
        {
            var issuer = builder.Configuration["Jwt:Issuer"];
            var key = Encoding.ASCII.GetBytes
            (builder.Configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim("Id", Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
             }),
                Expires = DateTime.UtcNow.AddMinutes(30),
                Issuer = issuer,
                SigningCredentials = new SigningCredentials
                (new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha512Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);
            var stringToken = tokenHandler.WriteToken(token);
            return stringToken;

        }



    }
}
