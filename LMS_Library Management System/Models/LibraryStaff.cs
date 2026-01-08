using System;

namespace LMS_Library_Management_System.Models
{
    public class LibraryStaff : User
    {
        public LibraryStaff() : base()
        {
            Role = UserRole.Staff;
        }

        public LibraryStaff(string email, string firstName, string lastName) 
            : base(email, firstName, lastName)
        {
            Role = UserRole.Staff;
        }

        public override string GetRoleDescription()
        {
            return "Library Staff - Operational access";
        }
    }
}

