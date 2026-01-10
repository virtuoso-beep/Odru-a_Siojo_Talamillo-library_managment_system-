using System;

namespace LMS_Library_Management_System.Models
{
    /// <summary>
    /// Data class for member information
    /// </summary>
    public class MemberData
    {
        public int MemberId { get; set; }
        public string MemberNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; }
        public string MemberType { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Department { get; set; }
        public int Status { get; set; } // 1 = Active, 2 = Suspended, 3 = Expired
        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case 0: return "Inactive";
                    case 1: return "Active";
                    case 2: return "Suspended";
                    case 3: return "Expired";
                    default: return "Unknown";
                }
            }
        }
        public DateTime RegistrationDate { get; set; }
    }
}

