namespace UsermanagerService.Models
{
    public class UserAuth
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string Token { get; set; } = string.Empty;
        public UserAuth(int id, string username, string role, string token)
        {
            Id = id;
            Username = username;
            Role = role;
            Token = token;

        }
    }
}
