using System;
using System.Text.RegularExpressions;

namespace LMS_Library_Management_System.Models
{
    public class Member : User
    {
        public int MemberId { get; set; }
        public string MemberNumber { get; set; }
        public string MemberType { get; set; }

        // Override Email property to enforce educational email validation for Members
        public new string Email
        {
            get { return base.Email; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Email cannot be empty.");
                
                // Members must have educational email format
                if (!IsValidEducationalEmail(value))
                {
                    throw new ArgumentException("Email must be a valid educational email in the format: firstname.lastname.IDnumber.tc@umindanao.edu.ph");
                }
                
                // Set the base email property
                base.Email = value;
            }
        }

        public Member() : base()
        {
            Role = UserRole.Member;
        }

        public Member(string email, string firstName, string lastName) 
            : base()
        {
            Role = UserRole.Member;
            // Set email with validation
            Email = email;
            FirstName = firstName;
            LastName = lastName;
        }

        public override string GetRoleDescription()
        {
            return "Library Member - Patron access";
        }

        private bool IsValidEducationalEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim().ToLower();
            if (!email.EndsWith("@umindanao.edu.ph"))
                return false;

            string[] emailParts = email.Split('@');
            if (emailParts.Length != 2)
                return false;
                
            string localPart = emailParts[0];

            string[] parts = localPart.Split('.');
            
            if (parts.Length != 4)
                return false;
            
            if (string.IsNullOrWhiteSpace(parts[0]) || !Regex.IsMatch(parts[0], @"^[a-z]+$"))
                return false;
            
            if (string.IsNullOrWhiteSpace(parts[1]) || !Regex.IsMatch(parts[1], @"^[a-z]+$"))
                return false;
            
            if (string.IsNullOrWhiteSpace(parts[2]) || !Regex.IsMatch(parts[2], @"^[0-9]+$"))
                return false;
            
            if (parts[3] != "tc")
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

