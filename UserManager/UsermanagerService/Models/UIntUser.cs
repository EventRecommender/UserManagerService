namespace UsermanagerService.Models
{
    
    public class UIntUser
    {
        public string Username { get; private set; }
        public string City { get; private set; }
        public string Institute { get; private set; }
        public string Role { get; private set; }

        public string Password { get; private set; }
        public UIntUser(string username, string city, string institute, string role, string password) 
        {
            Username = username;
            City = city;
            Institute= institute;
            Role = role;
            Password = password;

        }
    }
}
