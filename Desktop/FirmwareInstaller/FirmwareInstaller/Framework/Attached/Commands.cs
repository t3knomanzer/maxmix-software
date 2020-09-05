using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FirmwareInstaller.Framework.Attached
{
    public class Commands
    {
        public static readonly DependencyProperty FileOpenCommandProperty =
            DependencyProperty.RegisterAttached("FileOpenCommand", typeof(ICommand), typeof(Commands), 
                new FrameworkPropertyMetadata(null, OnFileOpenCommandChanged));

        [AttachedPropertyBrowsableForChildren]
        public static ICommand GetFileOpenCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(FileOpenCommandProperty);
        }

        public static void SetFileOpenCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(FileOpenCommandProperty, value);
        }

        protected static void OnFileOpenCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var control = obj as MenuItem;

            if (control != null)
            {
                if ((e.NewValue != null) && (e.OldValue == null))
                    control.Click += OnMenuItemClick;

                else if ((e.NewValue == null) && (e.OldValue != null))
                    control.Click -= OnMenuItemClick;
            }
        }

        private static void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Firmware (*.hex)|*.hex";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                GetFileOpenCommand(menuItem).Execute(openFileDialog.FileName);
            }
        }
    }
}
