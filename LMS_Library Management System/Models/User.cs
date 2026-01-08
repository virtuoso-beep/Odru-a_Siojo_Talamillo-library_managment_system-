using System;

namespace LMS_Library_Management_System.Models
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
                
                // Validate email format (basic validation)
                try
                {
                    var addr = new System.Net.Mail.MailAddress(value);
                    if (addr.Address != value.Trim())
                        throw new ArgumentException("Invalid email format.");
                }
                catch
                {
                    throw new ArgumentException("Invalid email format.");
                }
                
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

