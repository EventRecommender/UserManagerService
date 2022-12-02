using System.Reflection;

namespace UsermanagerService.Models
{
    public class DBService
    {
        private string URL = "";

        public bool ContainsCredentials(string username, string password)
        {
            return false;
        }

        public User? FetchUser (int userId)
        {
            return null;
            //return new User(1, "test", "Aalborg", "AAU", "Student");
        }

        public bool DeleteUser(int userId)
        {
            return false;
        }

        public bool AddNewUser(User user)
        {
            return false;
        }
       

        



    }
}
