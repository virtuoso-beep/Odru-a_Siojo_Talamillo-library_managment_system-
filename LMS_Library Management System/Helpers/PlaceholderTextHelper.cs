using System;
using System.Drawing;
using System.Windows.Forms;

namespace LMS_Library_Management_System.Helper
{
    public static class PlaceholderTextHelper
    {
        private const string PLACEHOLDER_TAG = "PLACEHOLDER_TAG";

        /// <summary>
        /// Sets placeholder text for a TextBox control using Enter/Leave events
        /// </summary>
        /// <param name="textBox">The TextBox control to set placeholder text for</param>
        /// <param name="placeholderText">The placeholder text to display</param>
        /// <param name="placeholderColor">The color for placeholder text (default is gray)</param>
        public static void SetPlaceholder(this TextBox textBox, string placeholderText, Color? placeholderColor = null)
        {
            if (textBox == null)
                throw new ArgumentNullException(nameof(textBox));

            if (string.IsNullOrEmpty(placeholderText))
                return;

            Color color = placeholderColor ?? Color.Gray;

            // Store the placeholder text in the Tag property
            textBox.Tag = new PlaceholderData
            {
                PlaceholderText = placeholderText,
                PlaceholderColor = color,
                OriginalForeColor = textBox.ForeColor
            };

            // Set initial placeholder if textbox is empty
            if (string.IsNullOrEmpty(textBox.Text))
            {
                // For password fields, clear password char to show placeholder text clearly
                if (textBox.Name.Contains("Password") || textBox.Name.Contains("password"))
                {
                    textBox.PasswordChar = '\0';
                }
                textBox.Text = placeholderText;
                textBox.ForeColor = color;
            }

            // Handle Enter event (when user clicks in the textbox)
            textBox.Enter += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && tb.Tag is PlaceholderData data)
                {
                    if (tb.Text == data.PlaceholderText)
                    {
                        tb.Text = "";
                        tb.ForeColor = data.OriginalForeColor;
                        // Set password char if it's a password field (user will type actual password)
                        if (tb.Name.Contains("Password") || tb.Name.Contains("password"))
                        {
                            tb.PasswordChar = '●';
                        }
                    }
                }
            };
            
            // Handle TextChanged to ensure password char is set when user types
            textBox.TextChanged += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && tb.Tag is PlaceholderData data)
                {
                    // If user is typing (not placeholder), ensure password char is set for password fields
                    if (tb.Text != data.PlaceholderText && !string.IsNullOrEmpty(tb.Text))
                    {
                        if ((tb.Name.Contains("Password") || tb.Name.Contains("password")) && tb.PasswordChar == '\0')
                        {
                            tb.PasswordChar = '●';
                        }
                    }
                }
            };

            // Handle Leave event (when user clicks out of the textbox)
            textBox.Leave += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && tb.Tag is PlaceholderData data)
                {
                    if (string.IsNullOrWhiteSpace(tb.Text))
                    {
                        tb.Text = data.PlaceholderText;
                        tb.ForeColor = data.PlaceholderColor;
                        // Restore password char if it's a password field
                        if (tb.Name.Contains("Password") || tb.Name.Contains("password"))
                        {
                            tb.PasswordChar = '\0'; // Show placeholder text clearly
                        }
                    }
                    else
                    {
                        // If there's actual text, restore password char for password fields
                        if (tb.Name.Contains("Password") || tb.Name.Contains("password"))
                        {
                            tb.PasswordChar = '●';
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Gets the actual text from a TextBox, excluding placeholder text
        /// </summary>
        /// <param name="textBox">The TextBox control</param>
        /// <returns>The actual text, or empty string if showing placeholder</returns>
        public static string GetActualText(this TextBox textBox)
        {
            if (textBox == null)
                return string.Empty;

            if (textBox.Tag is PlaceholderData data && textBox.Text == data.PlaceholderText)
            {
                return string.Empty;
            }

            return textBox.Text;
        }

        /// <summary>
        /// Sets the actual text for a TextBox, handling placeholder logic
        /// </summary>
        /// <param name="textBox">The TextBox control</param>
        /// <param name="text">The text to set</param>
        public static void SetActualText(this TextBox textBox, string text)
        {
            if (textBox == null)
                return;

            if (textBox.Tag is PlaceholderData data)
            {
                if (string.IsNullOrEmpty(text))
                {
                    // For password fields, clear password char to show placeholder text clearly
                    if (textBox.Name.Contains("Password") || textBox.Name.Contains("password"))
                    {
                        textBox.PasswordChar = '\0';
                    }
                    textBox.Text = data.PlaceholderText;
                    textBox.ForeColor = data.PlaceholderColor;
                }
                else
                {
                    // For password fields, set password char when there's actual text
                    if (textBox.Name.Contains("Password") || textBox.Name.Contains("password"))
                    {
                        textBox.PasswordChar = '●';
                    }
                    textBox.Text = text;
                    textBox.ForeColor = data.OriginalForeColor;
                }
            }
            else
            {
                textBox.Text = text ?? "";
                // For password fields, set password char if there's text
                if (!string.IsNullOrEmpty(text) && (textBox.Name.Contains("Password") || textBox.Name.Contains("password")))
                {
                    textBox.PasswordChar = '●';
                }
            }
        }

        public class PlaceholderData
        {
            public string PlaceholderText { get; set; }
            public Color PlaceholderColor { get; set; }
            public Color OriginalForeColor { get; set; }
        }
    }
}

