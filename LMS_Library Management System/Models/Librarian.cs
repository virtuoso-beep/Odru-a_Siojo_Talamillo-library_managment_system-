using System;

namespace LMS_Library_Management_System.Models
{
    public class Librarian : User
    {
        public Librarian() : base()
        {
            Role = UserRole.Administrator;
        }

        public Librarian(string email, string firstName, string lastName) 
            : base(email, firstName, lastName)
        {
            Role = UserRole.Administrator;
        }

        public override string GetRoleDescription()
        {
            return "Administrator - Full system access";
        }
    }
}

