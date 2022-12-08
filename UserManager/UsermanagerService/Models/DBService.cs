using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Text;
using UsermanagerService.Exceptions;

namespace UsermanagerService.Models
{
    public class DBService
    {
        private SqlConnection connection;

        public DBService(string uRL)
        {
            connection = new SqlConnection(uRL);
        }

        
        /// <summary>
        /// Fetches a user and password from the database
        /// </summary>
        /// <param name="username"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <exception cref="InstanceException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public void FetchUserAndPasswordFromUsername(string username, out User user, out byte[] password)
        {
            List<Tuple<User, byte[]>> FoundUsers = new List<Tuple<User, byte[]>>();

            //Query
            string query = $"SELECT * " +
                           $"FROM user" +
                           $" FULL OUTER JOIN password" +
                           $"ON user.id = password.id" +
                           $" WHERE username = {username};";
            //Command
            SqlCommand command = new SqlCommand(query, this.connection);

            try
            {
                connection.Open();
                
                SqlDataReader reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    User FoundUser = new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]); //Creates user from retrieved rows
                    byte[] FoundPas = (byte[])reader[5]; //Sets retrieved password hash

                    FoundUsers.Add(new Tuple<User, byte[]>(FoundUser, FoundPas));
                }

                reader.Close();

                if (FoundUsers.Count == 1)
                {
                    user = FoundUsers[0].Item1;
                    password = FoundUsers[0].Item2;
                    
                }
                else { throw new InstanceException($"{FoundUsers.Count}"); }
            }
            catch (SqlException) { throw new DatabaseException("Database Error"); }
            finally { if (this.connection.State == ConnectionState.Open) { this.connection.Close(); } }

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
            List<User> users = new List<User>();
            //Query 
            string query = $"SELECT * from user WHERE id = {userId}";

            //SQL Command
            SqlCommand command = new SqlCommand(query, this.connection);

            try
            {
                this.connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]));
                }

                reader.Close();
                command.Dispose();
               

                if (users.Count == 1)
                {
                    return users[0]; 
                }
                else { throw new InstanceException($"{users.Count}"); }

            }
            catch (SqlException ex) { throw new DatabaseException(ex.Message); }
            finally { if (this.connection.State == ConnectionState.Open) { this.connection.Close(); } }
        }

        
        public bool DeleteUser(int userId)
        {
            //Queries
            string query1 = $"DELETE FROM user WHERE id = {userId};";
            string query2 = $"DELETE FROM password WHERE id = {userId};";

            //Commands
            SqlCommand delUsercommand = new SqlCommand(query1, this.connection);
            SqlCommand delpasswordcommand = new SqlCommand(query2, this.connection);
            try
            {
                this.connection.Open();
                delUsercommand.ExecuteNonQuery(); //delete the user from user table
                delpasswordcommand.ExecuteNonQuery(); //delete the user from password table
                return true;
            }
            catch {

                //Implement SQL ROLLBACK
                return false;
            }
            finally { if (this.connection.State == ConnectionState.Open) { this.connection.Close(); } }


        }

       /// <summary>
       /// Adds a user to both user and password table.
       /// </summary>
       /// <param name="user"></param>
       /// <param name="hashedPassword"></param>
       /// <returns></returns>
       /// <exception cref="DatabaseException"></exception>
        public bool AddUser(UIntUser user, byte[] hashedPassword)
        {
            int id = -1;

            //Queries
            string insertQuery = $"IF NOT EXIST " +
                       $"SELECT * FROM user " +
                       $"WHERE username = {user.Username} " +
                       $"BEGIN " +
                       $"INSERT INTO user ({user.Username}, {user.City}, {user.Institute}, {user.Role}) " +
                       $"END";

            string getIdQuery = $"SELECT id FROM user " +
                                $"WHERE username = {user.Username} city = {user.City} institute = {user.Institute} role  = {user.Role}";

            //SQL Commands
            SqlCommand insertCommand = new SqlCommand(insertQuery, this.connection); //The command to insert user into user table
            SqlCommand IdCommand = new SqlCommand(getIdQuery, this.connection); //The command to retrieve the id of the inserted user.
            SqlCommand passwordCommand = new SqlCommand($"INSERT INTO password ({id},{hashedPassword}", this.connection);

            try
            {
                this.connection.Open();
                int RowsEffected = insertCommand.ExecuteNonQuery(); //execute insertion in user table

                if (RowsEffected != 1) { throw new InsertionException("Insertion Failed"); }

                SqlDataReader reader = IdCommand.ExecuteReader(); //read from the user table   

                while (reader.Read()) { id = (int)reader[0];  }
                reader.Close();
                passwordCommand.ExecuteNonQuery(); // Execute insertion in password table

                return true;
            }
            catch (InsertionException) { return false; }
            catch (SqlException) {

                //ROLLBACK
                throw new DatabaseException("DATABASE ERROR");
            }
            finally { 

                if (this.connection.State == ConnectionState.Open) { this.connection.Close(); }
                IdCommand.Dispose();
                insertCommand.Dispose();
                passwordCommand.Dispose();
            }

        }

        
        
        


    }
}
