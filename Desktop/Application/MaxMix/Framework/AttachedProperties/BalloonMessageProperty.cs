using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MaxMix.Framework.AttachedProperties
{
    internal class BalloonMessageProperty
    {
        public static readonly DependencyProperty BallonMessageProperty = DependencyProperty.RegisterAttached(
        "BallonMessage",
        typeof(string),
        typeof(BalloonMessageProperty),
        new PropertyMetadata(string.Empty, BallonMessageChanged));

        private static void BallonMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TaskbarIcon)
            {
                var obj = (TaskbarIcon)d;
                var oldArg = e.OldValue.ToString();
                var newArg = e.NewValue.ToString();

                // Check if the previous value was null, meaning this is the first
                // time this is called (application startup).
                // Or if the new value is null, meaning and invalid value was received.
                if (string.IsNullOrEmpty(oldArg) ||
                    string.IsNullOrEmpty(newArg))
                    return;

                obj.ShowBalloonTip("MaxMix", e.NewValue.ToString(), BalloonIcon.Info);
            }
        }

        public static string GetBallonMessage(DependencyObject obj)
        {
            return (string)obj.GetValue(BallonMessageProperty);
        }

        public static void SetBallonMessage(DependencyObject obj, string value)
        {
            obj.SetValue(BallonMessageProperty, value);
        }
    }
}
