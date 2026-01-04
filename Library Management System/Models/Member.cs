namespace Library_Management_System.Models
{
    public class Member : User
    {
        public int MemberId { get; set; }
        public string MemberNumber { get; set; }
        public Member() : base()
        {
            Role = UserRole.Member;
        }
        public Member(string email, string firstName, string lastName)
            : base(email, firstName, lastName)
        {
            Role = UserRole.Member;
        }
        public override string GetRoleDescription()
        {
            return "Library Member - Search catalog, view borrowed books, reserve books";
        }
        public override bool HasAccessToModule(string moduleName)
        {
            if (!IsActive) return false;
            switch (moduleName.ToLower())
            {
                case "search":
                case "my books":
                case "reservations":
                    return true;
                default:
                    return false;
            }
        }
    }
}
