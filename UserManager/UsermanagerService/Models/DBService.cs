using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Text;
using UsermanagerService.Exceptions;
using MySql.Data.MySqlClient;

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
            MySqlConnection connection = new MySqlConnection(URL);
            List<User> users = new List<User>();

            //Query
            string query = $"SELECT user.id, user.username, user.city, user.institute, user.role " +
                           $"FROM user JOIN password " +
                           $"ON user.id = password.userid " +
                           $"WHERE username = '{username}' AND password = '{password}';";
            
            //Command
            MySqlCommand command = new MySqlCommand(query, connection);
            try
            {
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]));
                }

                reader.Close();
                command.Dispose();

                if (users.Count != 0) {
                    if (users.Count == 1) { return users[0]; }
                    else { throw new MultipleInstanceException($"{users.Count}"); }
                }
                else { throw new NoInstanceException(""); }
            }
            catch (MySqlException ex) { throw ex; }// newDatabaseException("Database Error"); }
            finally { if (connection.State == ConnectionState.Open) { connection.Close(); } }
        }

        /// <summary>
        /// Fetches a user from the database
        /// </summary>
        /// <param name="userId"></param>
        /// <returns> A user object </returns>
        /// <exception cref="Exception"></exception>
        public User FetchUser (int userId)
        {
            MySqlConnection connection = new MySqlConnection(this.URL);
            List<User> users = new List<User>();
            //Query 
            string query = $"SELECT * FROM user WHERE id = {userId}";

            //SQL Command
            MySqlCommand command = new MySqlCommand(query, connection);

            try
            {
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]));
                }

                reader.Close();
                command.Dispose();
               

                if (users.Count != 0)
                {
                    if (users.Count > 1) { throw new MultipleInstanceException($"Found {users.Count} ID"); }
                    else { return users[0]; }
                }
                else { throw new NoInstanceException("No User found"); }

            }
            catch (MySqlException ex) { throw new DatabaseException(ex.Message); }
            finally { if (connection.State == ConnectionState.Open) { connection.Close(); } }
        }

        /// <summary>
        /// Removes a user from the database
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>True is user successfully removed</returns>
        public bool DeleteUser(int userId)
        {
            MySqlConnection connection = new MySqlConnection(URL);
            //Queries (password is deleted in the cascade)
            string query1 = $"DELETE FROM user WHERE id = '{userId}';";

            //Command
            MySqlCommand delUsercommand = new MySqlCommand(query1, connection);
            try
            {
                connection.Open();
                int affected = delUsercommand.ExecuteNonQuery(); //delete the user from user table
                if (affected == 0)
                    return false;
           
                return true;
            }
            catch (MySqlException){

                //Implement SQL ROLLBACK
                throw new DatabaseException("DATABASE ERROR");
            }
            finally { if (connection.State == ConnectionState.Open) { connection.Close(); } }


        }

        /// <summary>
        /// Adds a new user to the database.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns>True if user added correctly</returns>
        public bool AddNewUser(User user, string password)
        {
            bool result;
            MySqlConnection connection = new MySqlConnection(URL);
            List<int> ids = new List<int>();
            //Queries


            string insertQuery =$"INSERT INTO user (username, city, institute, role) " +
                                $"SELECT * FROM (SELECT '{user.username}' AS username, '{user.city}' AS city, '{user.institute}' AS institute, '{user.role}' AS role ) AS temp " +
                                $"WHERE NOT EXISTS( " +
                                $"SELECT username FROM user WHERE username = '{user.username}' " +      
                                $") LIMIT 1;";  

            string getIdQuery = $"SELECT id FROM user " +
                                $"WHERE username = '{user.username}' AND city = '{user.city}' AND institute = '{user.institute}' AND role  = '{user.role}'";

            //Sql Commands
            MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection); //The command to insert user into user table
            MySqlCommand IdCommand = new MySqlCommand(getIdQuery, connection); //The command to retrieve the id of the inserted user.W

            try
            {
                connection.Open();
                if (insertCommand.ExecuteNonQuery() != 1)//execute insertion in user table
                {
                    throw new InsertionException("Insertion Failed");
                }

                MySqlDataReader reader = IdCommand.ExecuteReader(); //read from the user table   

                while (reader.Read()) { ids.Add((int)reader[0]); }
                


                reader.Close();
                IdCommand.Dispose();
                insertCommand.Dispose();
                

                if (ids.Count != 1) { throw new MultipleInstanceException($"{ids.Count}"); }
                else 
                {
                    // Execute insertion in password table
                    MySqlCommand passwordCommand = new MySqlCommand($"INSERT INTO password VALUES ({ids[0]},'{password}')", connection);
                    passwordCommand.ExecuteNonQuery();
                    passwordCommand.Dispose();
                } 

                return true;
            }
            catch (InsertionException ex) { return false; }
            catch (MySqlException ex) {

                //ROLLBACK
                throw new DatabaseException("DATABASE ERROR");
            
            }
            finally { if (connection.State == ConnectionState.Open) { connection.Close(); } }

        }
        
        /// <summary>
        /// Generates a JWT token
        /// </summary>
        /// <param name="builder"></param>
        /// <returns>Token string</returns>
        public string GenerateToken()
        {
            return "Temp Authentication token";

        }



    }
}
