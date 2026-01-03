using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Library_Management_System.Exceptions;
using Library_Management_System.Models;

namespace Library_Management_System.Validators
{
    /// <summary>
    /// Validator for User-related operations
    /// </summary>
    public static class UserValidator
    {
        public static void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException("Email cannot be empty.");

            email = email.Trim().ToLower();

            // Check if it's an educational email
            if (email.EndsWith("@umindanao.edu.ph"))
            {
                if (!IsValidEducationalEmail(email))
                {
                    throw new ValidationException("Email must be in the format: firstname.lastname.IDnumber.tc@umindanao.edu.ph");
                }
            }
            else
            {
                // Regular email validation
                if (!IsValidRegularEmail(email))
                {
                    throw new ValidationException("Email format is invalid.");
                }
            }
        }

        public static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ValidationException("Password cannot be empty.");

            if (password.Length < 8)
                throw new ValidationException("Password must be at least 8 characters long.");

            if (!Regex.IsMatch(password, @"[A-Z]"))
                throw new ValidationException("Password must contain at least one uppercase letter.");

            if (!Regex.IsMatch(password, @"[a-z]"))
                throw new ValidationException("Password must contain at least one lowercase letter.");

            if (!Regex.IsMatch(password, @"[0-9]"))
                throw new ValidationException("Password must contain at least one number.");
        }

        public static void ValidateName(string name, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException($"{fieldName} cannot be empty.");

            name = name.Trim();

            if (name.Length < 2 || name.Length > 50)
                throw new ValidationException($"{fieldName} must be between 2 and 50 characters.");

            if (!Regex.IsMatch(name, @"^[a-zA-Z\s\-']+$"))
                throw new ValidationException($"{fieldName} can only contain letters, spaces, hyphens, and apostrophes.");
        }

        private static bool IsValidEducationalEmail(string email)
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

        private static bool IsValidRegularEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim().ToLower();

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email && email.Contains("@");
            }
            catch
            {
                return false;
            }
        }
    }
}

