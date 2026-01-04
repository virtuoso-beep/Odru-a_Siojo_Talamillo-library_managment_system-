using System;
using System.Drawing;
using System.Windows.Forms;
namespace Library_Management_System.Helper
{
    public static class PlaceholderTextHelper
    {
        private const string PLACEHOLDER_TAG = "PLACEHOLDER_TAG";
        public static void SetPlaceholder(this TextBox textBox, string placeholderText, Color? placeholderColor = null)
        {
            if (textBox == null)
                throw new ArgumentNullException(nameof(textBox));
            if (string.IsNullOrEmpty(placeholderText))
                return;
            Color color = placeholderColor ?? Color.Gray;
            textBox.Tag = new PlaceholderData
            {
                PlaceholderText = placeholderText,
                PlaceholderColor = color,
                OriginalForeColor = textBox.ForeColor
            };
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = placeholderText;
                textBox.ForeColor = color;
            }
            textBox.Enter += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && tb.Tag is PlaceholderData data)
                {
                    if (tb.Text == data.PlaceholderText)
                    {
                        tb.Text = "";
                        tb.ForeColor = data.OriginalForeColor;
                        if (tb.Name.Contains("Password") || tb.Name.Contains("password"))
                        {
                            tb.PasswordChar = '●';
                        }
                    }
                }
            };
            textBox.TextChanged += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && tb.Tag is PlaceholderData data)
                {
                    if (tb.Text != data.PlaceholderText && !string.IsNullOrEmpty(tb.Text))
                    {
                        if ((tb.Name.Contains("Password") || tb.Name.Contains("password")) && tb.PasswordChar == '\0')
                        {
                            tb.PasswordChar = '●';
                        }
                    }
                }
            };
            textBox.Leave += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && tb.Tag is PlaceholderData data)
                {
                    if (string.IsNullOrWhiteSpace(tb.Text))
                    {
                        tb.Text = data.PlaceholderText;
                        tb.ForeColor = data.PlaceholderColor;
                        if (tb.Name.Contains("Password") || tb.Name.Contains("password"))
                        {
                            tb.PasswordChar = '\0';
                        }
                    }
                    else
                    {
                        if (tb.Name.Contains("Password") || tb.Name.Contains("password"))
                        {
                            tb.PasswordChar = '●';
                        }
                    }
                }
            };
        }
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
        public static void SetActualText(this TextBox textBox, string text)
        {
            if (textBox == null)
                return;
            if (textBox.Tag is PlaceholderData data)
            {
                if (string.IsNullOrEmpty(text))
                {
                    textBox.Text = data.PlaceholderText;
                    textBox.ForeColor = data.PlaceholderColor;
                    if (textBox.Name.Contains("Password") || textBox.Name.Contains("password"))
                    {
                        textBox.PasswordChar = '\0';
                    }
                }
                else
                {
                    textBox.Text = text;
                    textBox.ForeColor = data.OriginalForeColor;
                }
            }
            else
            {
                textBox.Text = text ?? "";
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
