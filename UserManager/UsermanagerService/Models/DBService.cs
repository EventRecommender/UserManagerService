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

            //Query
            string getIdQeury = $"SELECT user.id, user.username, user.city, user.institute, user.role " +
                          $"FROM user " +
                           $"WHERE username = '{username}';";
            
            MySqlCommand command = new MySqlCommand(getIdQeury, connection);
            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            try
            {
                
                if (!reader.Read()){
                    command.Dispose();
                    reader.Dispose();
                    connection.Close();
                    throw new InstanceException("no users found");
                }
                user = new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]); //Creates user from retrieved rows

                command.Dispose();
                reader.Close();
                connection.Close();

                connection.Open();
                string getPassQuery = $"SELECT password FROM password WHERE userid = '{user.ID}';";
                command = new MySqlCommand(getPassQuery, connection);
                reader = command.ExecuteReader();
                if (!reader.Read()){
                    reader.Dispose();
                    command.Dispose();
                    connection.Close();
                    throw new InstanceException("no password found for the given user");
                }
                password = (string)reader[0];

                connection.Close();
            }
            catch (MySqlException) {
                command.Dispose();
                reader.Close();
                connection.Close();
                throw; }
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
            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            try
            {
                
                

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
                command.Dispose();
                reader.Close();
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
                if (affected == 0){
                    delUsercommand.Dispose();
                    return false;
                }

                delUsercommand.Dispose();
                return true;
            }
            catch (MySqlException)
            {
                delUsercommand.Dispose();
                connection.Close();
                throw;
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
            string findUserQuery = $"SELECT username FROM user WHERE username = '{user.Username}' LIMIT 1;";
            connection.Open();
            
            MySqlCommand findUserCommand = new MySqlCommand(findUserQuery,connection);
            MySqlDataReader reader2 = findUserCommand.ExecuteReader();
            if (reader2.Read()){
                findUserCommand.Dispose();
                reader2.Dispose();
                connection.Close();
                return -1;
            }

            findUserCommand.Dispose();
            reader2.Dispose();
            connection.Close();

            string insertQuery = $"INSERT INTO user (username, city, institute, role) " +
                                $"VALUES ('{user.Username}','{user.City}','{user.Institute}','{user.Role}');";

            string getIdQuery = $"SELECT id FROM user " +
                                $"WHERE username = '{user.Username}' AND city = '{user.City}' AND institute = '{user.Institute}' AND role  = '{user.Role}'";


           
            //Sql Commands
            MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection); //The command to insert user into user table
            MySqlCommand IdCommand = new MySqlCommand(getIdQuery, connection); //The command to retrieve the id of the inserted user.W
            MySqlDataAdapter adapter = new MySqlDataAdapter();

            adapter.InsertCommand = insertCommand;
            try
            {
                connection.Open();
                
                int RowsEffected = adapter.InsertCommand.ExecuteNonQuery(); //execute insertion in user table
                connection.Close();
                if (RowsEffected != 1) 
                {
                    adapter.Dispose();
                    insertCommand.Dispose();
                    IdCommand.Dispose(); 
                    throw new InsertionException("Insertion Failed"); 
                }
                connection.Open();
                MySqlDataReader reader = IdCommand.ExecuteReader(); //read from the user table   

                while (reader.Read()) { id = (int)reader[0]; }

                reader.Close();
                IdCommand.Dispose();
                insertCommand.Dispose();
                adapter.Dispose();
                // Execute insertion in password table
                connection.Close();
                connection.Open();
                MySqlCommand passwordCommand = new MySqlCommand($"INSERT INTO password VALUES ({id},'{user.Password}')", connection);
                passwordCommand.ExecuteNonQuery();
                passwordCommand.Dispose();
                connection.Close();
                return id;
            }
            catch (InsertionException) 
            { 
                IdCommand.Dispose();
                insertCommand.Dispose();
                connection.Close();
                adapter.Dispose();
                return -1; 
            }
            catch (MySqlException)
            {
                IdCommand.Dispose();
                insertCommand.Dispose();
                connection.Close();
                adapter.Dispose();
                throw;
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
            MySqlDataReader reader = Fetch.ExecuteReader();
            try
            {
                connection.Open();
                

                while (reader.Read()) { 
                    users.Add(new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]));
                }

                Fetch.Dispose();
                reader.Dispose();
                connection.Close();
                return users;
            }catch(MySqlException)
            {
                Fetch.Dispose();
                reader.Dispose();
                connection.Close();
                throw;
            }
        }

        public void CreateUsersForTesting(int amount){
    
			MySqlConnection connection = new(connectionString);

			StringBuilder sb = new StringBuilder($"INSERT INTO user (username, city, institute, role) VALUES ");
			List<string> rows = new List<string>();


			List<string> newUsers = new();
			for(int i = 1;i <= amount; i++){
			    newUsers.Add($"{i}");
			}
			foreach(var id in newUsers)
			{
				rows.Add(string.Format("('{0}', '{1}', '{2}', '{3}')", MySqlHelper.EscapeString("username"+id), MySqlHelper.EscapeString("aalborg"), MySqlHelper.EscapeString("steve"), MySqlHelper.EscapeString("student")));
			}
			sb.Append(string.Join(",", rows));
			sb.Append(";");

			string SQLstatement = sb.ToString();



			connection.Open();
			MySqlCommand command = new MySqlCommand(SQLstatement, connection);
			MySqlDataAdapter adapter = new MySqlDataAdapter();


			command.CommandType = CommandType.Text;
			adapter.InsertCommand = command;
			adapter.InsertCommand.ExecuteNonQuery();

			command.Dispose();
			adapter.Dispose();

			connection.Close();

            connection.Open();


            StringBuilder sb2 = new StringBuilder($"INSERT INTO password (userid, password) VALUES ");
            List<string> rows2 = new List<string>();
            foreach(var id in newUsers)
			{
				rows2.Add(string.Format("('{0}', '{1}')", MySqlHelper.EscapeString($"{id}"), MySqlHelper.EscapeString("password")));
			}
            sb2.Append(string.Join(",", rows2));
			sb2.Append(";");
            string query = sb2.ToString();
            MySqlCommand passwordCommand = new MySqlCommand(query, connection);
            passwordCommand.ExecuteNonQuery();
            passwordCommand.Dispose();
            connection.Close();
		}
    }
}
