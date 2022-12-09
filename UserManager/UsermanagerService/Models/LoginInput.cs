namespace UsermanagerService.Models
{
    public class LoginInput
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public LoginInput(string username, string password)
        {
            Username = username;
            Password= password;
        }
    }
}
