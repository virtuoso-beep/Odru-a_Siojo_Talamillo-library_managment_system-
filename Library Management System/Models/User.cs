using System;
using System.Text.RegularExpressions;
namespace Library_Management_System.Models
{
    public abstract class User
    {
        protected int _userId;
        protected string _email;
        protected string _passwordHash;
        protected string _firstName;
        protected string _lastName;
        protected UserRole _role;
        protected bool _isActive;
        protected DateTime _createdDate;
        public int UserId
        {
            get { return _userId; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("User ID must be greater than zero.");
                _userId = value;
            }
        }
        public string Email
        {
            get { return _email; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Email cannot be empty.");
                if (!IsValidEducationalEmail(value))
                    throw new ArgumentException("Email must be a valid educational email in the format: firstname.lastname.IDnumber.tc@umindanao.edu.ph");
                _email = value.Trim().ToLower();
            }
        }
        public string PasswordHash
        {
            get { return _passwordHash; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Password hash cannot be empty.");
                _passwordHash = value;
            }
        }
        public string FirstName
        {
            get { return _firstName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("First name cannot be empty.");
                _firstName = value.Trim();
            }
        }
        public string LastName
        {
            get { return _lastName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Last name cannot be empty.");
                _lastName = value.Trim();
            }
        }
        public string FullName => $"{FirstName} {LastName}";
        public UserRole Role
        {
            get { return _role; }
            protected set { _role = value; }
        }
        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; }
        }
        public DateTime CreatedDate
        {
            get { return _createdDate; }
            set { _createdDate = value; }
        }
        public abstract string GetRoleDescription();
        public virtual bool HasAccessToModule(string moduleName)
        {
            return IsActive;
        }
        private bool IsValidEducationalEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            email = email.Trim().ToLower();
            if (!email.EndsWith("@umindanao.edu.ph"))
                return false;
            string localPart = email.Split('@')[0];
            string pattern = @"^[a-zA-Z]+\.[a-zA-Z]+\.[0-9]+\.tc$";
            if (!Regex.IsMatch(localPart, pattern))
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
        protected User()
        {
            _createdDate = DateTime.Now;
            _isActive = true;   
        }
        protected User(string email, string firstName, string lastName)
        {
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            _createdDate = DateTime.Now;
            _isActive = true;
        }
    }
}
