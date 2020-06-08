using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MaxMix.Framework.Mvvm;
using MaxMix.ViewModels;

namespace MaxMix
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += OnMainWindowClosing;
        }

        #region Fields
        private ICommand _closeCommand;
        #endregion

        #region Attached Properties
        // Command to be called on window closing
        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.RegisterAttached("CloseCommand", typeof(ICommand), typeof(MainWindow), new FrameworkPropertyMetadata(OnCloseCommandChanged));

        public static void SetCloseCommand(UIElement element, ICommand value)
        {
            element.SetValue(CloseCommandProperty, value);
        }

        public static ICommand GetCloseCommand(UIElement element)
        {
            return (ICommand)element.GetValue(CloseCommandProperty);
        }

        private static void OnCloseCommandChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs args)
        {
            var window = dpo as MainWindow;
            var command = (ICommand)args.NewValue;
            window._closeCommand = command;
        }
        #endregion

        #region Event Handlers
        private void OnMainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_closeCommand != null)
            {
                _closeCommand.Execute(null);
                e.Cancel = true;
            }
        }
        #endregion
    }
}
