using System;
using System.Drawing;
using System.Windows.Forms;

namespace Library_Management_System.Helper
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
                    textBox.Text = data.PlaceholderText;
                    textBox.ForeColor = data.PlaceholderColor;
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

        private class PlaceholderData
        {
            public string PlaceholderText { get; set; }
            public Color PlaceholderColor { get; set; }
            public Color OriginalForeColor { get; set; }
        }
    }
}
