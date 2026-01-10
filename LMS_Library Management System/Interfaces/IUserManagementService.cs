using System.Collections.Generic;
using LMS_Library_Management_System.Models;

namespace LMS_Library_Management_System.Interfaces
{
    /// <summary>
    /// Interface for user management operations
    /// </summary>
    public interface IUserManagementService
    {
        List<UserInfo> GetUsersByRole(UserRole role);
        
        UserInfo GetUserById(int userId);
        
        UserInfo GetUserByEmail(string email);
        
        bool CreateUser(string email, string password, string firstName, string lastName, 
            UserRole role, string department = null, string phone = null);
        
        bool UpdateUser(int userId, string email, string firstName, string lastName, 
            string department = null, string phone = null);
        
        bool UpdateUserPassword(string email, string newPassword);
        
        bool UpdateUserRole(int userId, UserRole newRole);
        
        bool DeactivateUser(int userId);
        
        bool UpdateLastLogin(string email);
        
        bool UserExists(string email);
        
        bool UpdateUserStatus(int userId, bool isActive);
        
        int AutoDeactivateInactiveUsers(int daysInactive = 90);
    }
}

