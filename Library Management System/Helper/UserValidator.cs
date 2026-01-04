using System;
using System.Text.RegularExpressions;
using Library_Management_System.Models;
namespace Library_Management_System.Helper
{
    public static class UserValidator
    {
        public static void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ValidationException("Email cannot be empty.");
            }
            email = email.Trim().ToLower();
            if (!email.EndsWith("@umindanao.edu.ph"))
            {
                throw new ValidationException("Email must be a valid educational email ending with @umindanao.edu.ph");
            }
            string localPart = email.Split('@')[0];
            string pattern = @"^[a-zA-Z]+\.[a-zA-Z]+\.[0-9]+\.tc$";
            if (!Regex.IsMatch(localPart, pattern))
            {
                throw new ValidationException("Email must be in the format: firstname.lastname.IDnumber.tc@umindanao.edu.ph");
            }
        }
        public static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ValidationException("Password cannot be empty.");
            }
            if (password.Length < 8)
            {
                throw new ValidationException("Password must be at least 8 characters long.");
            }
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                throw new ValidationException("Password must contain at least one uppercase letter.");
            }
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                throw new ValidationException("Password must contain at least one lowercase letter.");
            }
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                throw new ValidationException("Password must contain at least one number.");
            }
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?\"":{}|<>]"))
            {
                throw new ValidationException("Password must contain at least one special character.");
            }
        }
        public static void ValidateName(string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new ValidationException("First name cannot be empty.");
            }
            if (string.IsNullOrWhiteSpace(lastName))
            {
                throw new ValidationException("Last name cannot be empty.");
            }
        }
    }
}
