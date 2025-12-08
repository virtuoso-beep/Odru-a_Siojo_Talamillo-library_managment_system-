using Library_Management_System.Models;

namespace Library_Management_System.Interface
{
    public interface IAuthenticationService
    {
        User Authenticate(string email, string password, UserRole expectedRole);

        bool VerifyPassword(string password, string passwordHash);

        string HashPassword(string password);

        bool UserExists(string email);
    }
}

