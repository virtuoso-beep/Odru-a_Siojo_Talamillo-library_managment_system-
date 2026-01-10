using System;

namespace LMS_Library_Management_System.Models
{
    /// <summary>
    /// Data class for user information
    /// </summary>
    public class UserInfo
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Department { get; set; } // Optional field for display
        public string LastLogin { get; set; } // Optional field for display
        public string Phone { get; set; } // Optional field for phone number
    }
}

