using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Reflection.PortableExecutable;
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

        /// <summary>
        /// Checks if the username and password is a match in the database.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>The user</returns>
        public User? ContainsCredentials(string username, string password)
        {
            SqlConnection connection = new SqlConnection(URL);
            List<User> users = new List<User>();

            //Query
            string query = $"SELECT id username city institute role FROM user JOIN password WHERE username = {username} AND password ={password};";
            
            //Command
            SqlCommand command = new SqlCommand(query, connection);
            try
            {
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]));
                }

                reader.Close();
                command.Dispose();
                connection.Close();

                if (users.Count == 1){ return users[0];}
                else{return null;}
            }
            catch (Exception)
            {
                command.Dispose();
                connection.Close();
                return null;
            }
        }

        /// <summary>
        /// Fetches a user from the database
        /// </summary>
        /// <param name="userId"></param>
        /// <returns> A user object </returns>
        /// <exception cref="Exception"></exception>
        public User FetchUser (int userId)
        {
            SqlConnection connection = new SqlConnection(this.URL);
            List<User> users = new List<User>();
            //Query 
            string query = $"SELECT * from user WHERE id = {userId}";

            //SQL Command
            SqlCommand command = new SqlCommand(query, connection);

            try
            {
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]));
                }

                reader.Close();
                command.Dispose();
                connection.Close();

                if (users != null)
                {
                    if (users.Count > 1) { throw new Exception("USERID not unique"); }
                    else
                    {
                        return users[0];
                    }
                }
                else
                {
                    throw new Exception("User not found in database");
                }
            }catch(Exception ex)
            {
                if (ex is SqlException)
                {
                    throw (SqlException)ex;
                }
                else
                { throw new Exception("Fail"); }
            }
        }

        /// <summary>
        /// Removes a user from the database
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>True is user successfully removed</returns>
        public bool DeleteUser(int userId)
        {
            SqlConnection connection = new SqlConnection(URL);
            //Queries
            string query1 = $"DELETE FROM user WHERE id = {userId};";
            string query2 = $"DELETE FROM password WHERE id = {userId};";

            //Commands
            
            SqlCommand delUsercommand = new SqlCommand(query1, connection);
            SqlCommand delpasswordcommand = new SqlCommand(query2, connection);
            try
            {
                connection.Open();
                delUsercommand.ExecuteNonQuery(); //delete the user from user table
                delpasswordcommand.ExecuteNonQuery(); //delete the user from password table
                return true;
            }
            catch {

                //Implement SQL ROLLBACK
                return false;
            }
            

        }

        /// <summary>
        /// Adds a new user to the database.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns>True if user added correctly</returns>
        public bool AddNewUser(User user, string password)
        {
            SqlConnection connection = new SqlConnection(URL);
            List<int> ids = new List<int>();
            //Queries
            string insertQuery = $"INSERT INTO user ({user.username}, {user.city}, {user.institute}, {user.role})";
            string getIdQuery = $"SELECT id FROM user WHERE username = {user.username} city = {user.city} institute = {user.institute} role  = {user.role}";

            //SQL Commands
            SqlCommand insertCommand = new SqlCommand(insertQuery, connection); //The command to insert user into user table
            SqlCommand IdCommand = new SqlCommand(getIdQuery, connection); //The command to retrieve the id of the inserted user.
            SqlCommand passwordCommand = new SqlCommand($"INSERT INTO password ({ids[0]},{password}", connection);

            try
            {
                connection.Open();

                insertCommand.ExecuteNonQuery(); //execute insertion in user table
               
                SqlDataReader reader = IdCommand.ExecuteReader(); //read from the user table   

                while (reader.Read()){ ids.Add((int)reader[0]);}

                reader.Close();

                if (ids.Count != 1) { throw new Exception("Too many id"); }
                else{ passwordCommand.ExecuteNonQuery(); } // Execute insertion in password table

                IdCommand.Dispose();
                insertCommand.Dispose();
                passwordCommand.Dispose();
                connection.Close();
                
                return true;
            }catch
            {
                //Implement SQL ROLLBACK
                connection.Close();
                return false;
            }
            
        }
        
        /// <summary>
        /// Generates a JWT token
        /// </summary>
        /// <param name="builder"></param>
        /// <returns>Token string</returns>
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
