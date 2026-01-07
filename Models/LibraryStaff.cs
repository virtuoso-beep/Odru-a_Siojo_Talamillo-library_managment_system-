namespace Library_Management_System.Models
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
            return "Library Staff - Process borrowing, returns, and member registration";
        }
        public override bool HasAccessToModule(string moduleName)
        {
            if (!IsActive) return false;
            switch (moduleName.ToLower())
            {
                case "circulation":
                case "member management":
                case "catalog":
                case "search":
                    return true;
                case "reports":
                case "system configuration":
                case "user management":
                    return false; 
                default:
                    return false;
            }
        }
    }
}
