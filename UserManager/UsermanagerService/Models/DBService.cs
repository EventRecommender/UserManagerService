using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Text;
using UsermanagerService.Exceptions;
using MySql.Data.MySqlClient;
using System.Transactions;

namespace UsermanagerService.Models
{
    public class DBService
    {
        private string connectionString = "";
        public DBService(string uRL)
        {
            connectionString = uRL;
        }
        
        /// <summary>
        /// Fetches a user and password from the database
        /// </summary>
        /// <param name="username"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <exception cref="InstanceException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public void FetchUserAndPasswordFromUsername(string username, out User user, out string password)
        {
            var connection = new MySqlConnection(connectionString);

            List<Tuple<User, string>> FoundUsers = new List<Tuple<User, string>>();

            //Query
            string query = $"SELECT user.id, user.username, user.city, user.institute, user.role, password.password " +
                           $"FROM user JOIN password " +
                           $"ON user.id = password.userid " +
                           $"WHERE username = '{username}';";
            
            //Command
            MySqlCommand command = new MySqlCommand(query, connection);
            try
            {
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    User FoundUser = new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]); //Creates user from retrieved rows
                    string FoundPas = (string)reader[5]; //Sets retrieved password hash

                    FoundUsers.Add(new Tuple<User, string>(FoundUser, FoundPas));
                }

                reader.Close();
                connection.Close();
                if (FoundUsers.Count == 1)
                {
                    user = FoundUsers[0].Item1;
                    password = FoundUsers[0].Item2;
                    
                }
                else { throw new InstanceException($"{FoundUsers.Count}"); }
            }
            catch (MySqlException) {
                connection.Close();
                throw new DatabaseException("Database Error"); }
        }
        
       /// <summary>
       /// Return a user from database.
       /// </summary>
       /// <param name="userId"></param>
       /// <returns></returns>
       /// <exception cref="InstanceException"></exception>
       /// <exception cref="DatabaseException"></exception>
        public User FetchUserFromID (int userId)
        {
            var connection = new MySqlConnection(connectionString);

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
                connection.Close();

                if (users.Count == 1)
                {
                    return users[0]; 
                }
                else { throw new InstanceException($"{users.Count}"); }

            }
            catch (MySqlException ex) {
                connection.Close();
                throw new DatabaseException(ex.Message); }
        }

        /// <summary>
        /// Deletes a user from the database
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>True - the user is removed. False - No user removed</returns>
        /// <exception cref="DatabaseException"></exception>
        public bool DeleteUser(int userId)
        {
            var connection = new MySqlConnection(connectionString);

            //Queries (password is deleted in the cascade)
            string query1 = $"DELETE FROM user WHERE id = '{userId}';";

            //Command
            MySqlCommand delUsercommand = new MySqlCommand(query1, connection);
            try
            {
                connection.Open();
                int affected = delUsercommand.ExecuteNonQuery(); //delete the user from user table
                connection.Close();
                if (affected == 0)
                    return false;
           
                return true;
            }
            catch (MySqlException)
            {
                connection.Close();
                throw new DatabaseException("DATABASE ERROR");
            }
        }

        /// <summary>
        /// Adds a user to both user and password table.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="hashedPassword"></param>
        /// <returns>id of the user or -1 if no user was made</returns>
        /// <exception cref="DatabaseException"></exception>
        public int AddUser(UIntUser user)
        {
            var connection = new MySqlConnection(connectionString);

            int id = -1;
            //Queries

            string insertQuery = $"INSERT INTO user (username, city, institute, role) " +
                                $"SELECT * FROM (SELECT '{user.Username}' AS username, '{user.City}' AS city, '{user.Institute}' AS institute, '{user.Role}' AS role ) AS temp " +
                                $"WHERE NOT EXISTS( " +
                                $"SELECT username FROM user WHERE username = '{user.Username}' " +
                                $") LIMIT 1;";

            string getIdQuery = $"SELECT id FROM user " +
                                $"WHERE username = '{user.Username}' AND city = '{user.City}' AND institute = '{user.Institute}' AND role  = '{user.Role}'";


           
            //Sql Commands
            MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection); //The command to insert user into user table
            MySqlCommand IdCommand = new MySqlCommand(getIdQuery, connection); //The command to retrieve the id of the inserted user.W
            
            
            try
            {
                connection.Open();
                var tr = connection.BeginTransaction();
                insertCommand.Transaction = tr;
                IdCommand.Transaction = tr;
                
                int RowsEffected = insertCommand.ExecuteNonQuery(); //execute insertion in user table

                if (RowsEffected != 1) { throw new InsertionException("Insertion Failed"); }

                MySqlDataReader reader = IdCommand.ExecuteReader(); //read from the user table   

                while (reader.Read()) { id = (int)reader[0]; }

                reader.Close();
                IdCommand.Dispose();
                insertCommand.Dispose();
                // Execute insertion in password table
                MySqlCommand passwordCommand = new MySqlCommand($"INSERT INTO password VALUES ({id},'{user.Password}')", connection);
                passwordCommand.ExecuteNonQuery();
                passwordCommand.Dispose();

                tr.Commit();
                connection.Close();
                return id;
            }
            catch (InsertionException) 
            { 
                IdCommand.Dispose();
                insertCommand.Dispose();
                connection.Close();
                return -1; 
            }
            catch (MySqlException)
            {
                IdCommand.Dispose();
                insertCommand.Dispose();
                connection.Close();
                throw new DatabaseException("DATABASE ERROR");
            }
        }
        /// <summary>
        /// Retrieves all users from database.
        /// </summary>
        /// <returns>List of user objects</returns>
        /// <exception cref="DatabaseException"></exception>
        public List<User> FetchAllUsers()
        {
            var connection = new MySqlConnection(connectionString);

            List<User> users = new List<User>();
                    
            //Command
            MySqlCommand Fetch = new MySqlCommand("SELECT * FROM user", connection);
            try
            {
                connection.Open();
                MySqlDataReader reader = Fetch.ExecuteReader();

                while (reader.Read()) { 
                    users.Add(new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]));
                }

                connection.Close();
                return users;
            }catch(MySqlException ex)
            {
                connection.Close();
                throw new DatabaseException(ex.Message);
            }
        }
    }
}
