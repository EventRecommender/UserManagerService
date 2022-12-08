namespace UsermanagerService.Models
{
    public class temp
    {

        public string name { get; set; }
        public string pass { get; set; }

        public temp(string username, string password)
        {
            name= username;
            pass = password;
        }
    }
}
