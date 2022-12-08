namespace UsermanagerService.Models
{
    public class User
    {
        public int ID { get;  }
        public string username { get; }
        public string city { get; }
        public string institute { get; }
        public string role { get; }
       

        public User(int Id, string Username, string City, string Institute, string Role)
        {
            ID = Id;
            username = Username;
            city = City;
            institute = Institute;
            role= Role;
        }
    }
}
