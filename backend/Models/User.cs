namespace TokenyRefresh.Models
{
    // Modelo de usuario (para Entity Framework Core)
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
    }
}
