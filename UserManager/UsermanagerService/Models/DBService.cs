﻿using Microsoft.IdentityModel.Tokens;
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
        private MySqlConnection connection;

        public DBService(string uRL)
        {
            connection = new MySqlConnection(uRL);
            
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
            List<Tuple<User, string>> FoundUsers = new List<Tuple<User, string>>();

            //Query
            string query = $"SELECT user.id, user.username, user.city, user.institute, user.role, password.password " +
                           $"FROM user JOIN password " +
                           $"ON user.id = password.userid " +
                           $"WHERE username = '{username}';";
            
            //Command
            MySqlCommand command = new MySqlCommand(query, this.connection);
            try
            {
                this.connection.Open();
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    User FoundUser = new User((int)reader[0], (string)reader[1], (string)reader[2], (string)reader[3], (string)reader[4]); //Creates user from retrieved rows
                    string FoundPas = (string)reader[5]; //Sets retrieved password hash

                    FoundUsers.Add(new Tuple<User, string>(FoundUser, FoundPas));
                }

                reader.Close();

                if (FoundUsers.Count == 1)
                {
                    user = FoundUsers[0].Item1;
                    password = FoundUsers[0].Item2;
                    
                }
                else { throw new InstanceException($"{FoundUsers.Count}"); }
            }
            catch (MySqlException) { throw new DatabaseException("Database Error"); }
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
            string query = $"SELECT * FROM user WHERE id = {userId}";

            //SQL Command
            MySqlCommand command = new MySqlCommand(query, this.connection);

            try
            {
                this.connection.Open();
                MySqlDataReader reader = command.ExecuteReader();

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
            catch (MySqlException ex) { throw new DatabaseException(ex.Message); }
            finally { if (this.connection.State == ConnectionState.Open) { this.connection.Close(); } }
        }

        public bool DeleteUser(int userId)
        {

            //Queries (password is deleted in the cascade)
            string query1 = $"DELETE FROM user WHERE id = '{userId}';";

            //Command
            MySqlCommand delUsercommand = new MySqlCommand(query1, this.connection);
            try
            {
                this.connection.Open();
                int affected = delUsercommand.ExecuteNonQuery(); //delete the user from user table
                if (affected == 0)
                    return false;
           
                return true;
            }
            catch (MySqlException){

                throw new DatabaseException("DATABASE ERROR");
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
        public bool AddUser(UIntUser user)
        {
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
            MySqlCommand insertCommand = new MySqlCommand(insertQuery, this.connection); //The command to insert user into user table
            MySqlCommand IdCommand = new MySqlCommand(getIdQuery, this.connection); //The command to retrieve the id of the inserted user.W
            
            
            try
            {
                this.connection.Open();
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
                MySqlCommand passwordCommand = new MySqlCommand($"INSERT INTO password VALUES ({id},'{user.Password}')", this.connection);
                passwordCommand.ExecuteNonQuery();
                passwordCommand.Dispose();

                tr.Commit();
                return true;
            }
            catch (InsertionException) { return false; }
            catch (MySqlException)
            {
                throw new DatabaseException("DATABASE ERROR");
            }
            finally
            {
                if (this.connection.State == ConnectionState.Open) { this.connection.Close(); }
                IdCommand.Dispose();
                insertCommand.Dispose();
                             
            }
        }
    }
}
