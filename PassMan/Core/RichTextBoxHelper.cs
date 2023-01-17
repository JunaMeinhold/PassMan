namespace PassMan.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;

    public class RichTextBoxHelper
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached("Text",
            typeof(string), typeof(RichTextBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach",
            typeof(bool), typeof(RichTextBoxHelper), new PropertyMetadata(false, Attach));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached("IsUpdating", typeof(bool),
            typeof(RichTextBoxHelper));

        public static void SetAttach(DependencyObject dp, bool value)
        {
            dp.SetValue(AttachProperty, value);
        }

        public static bool GetAttach(DependencyObject dp)
        {
            return (bool)dp.GetValue(AttachProperty);
        }

        public static string GetText(DependencyObject dependencyObject)
        {
            return (string)dependencyObject.GetValue(TextProperty);
        }

        public static void SetText(DependencyObject dependencyObject, string text)
        {
            dependencyObject.SetValue(TextProperty, text);
        }

        private static bool GetIsUpdating(DependencyObject dp)
        {
            return (bool)dp.GetValue(IsUpdatingProperty);
        }

        private static void SetIsUpdating(DependencyObject dp, bool value)
        {
            dp.SetValue(IsUpdatingProperty, value);
        }

        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not RichTextBox richTextBox)
                return;
            richTextBox.TextChanged -= TextChanged;

            if (!GetIsUpdating(richTextBox))
            {
                new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text = (string?)e.NewValue ?? string.Empty;
            }
            richTextBox.TextChanged += TextChanged;
        }

        private static void Attach(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not RichTextBox richTextBox)
                return;

            if ((bool)e.OldValue)
            {
                richTextBox.TextChanged -= TextChanged;
            }

            if ((bool)e.NewValue)
            {
                richTextBox.TextChanged += TextChanged;
            }
        }

        private static void TextChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not RichTextBox richTextBox)
                return;
            SetIsUpdating(richTextBox, true);
            SetText(richTextBox, new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text);
            SetIsUpdating(richTextBox, false);
        }
    }
}