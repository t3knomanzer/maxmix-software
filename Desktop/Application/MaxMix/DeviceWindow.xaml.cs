using MaxMix.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MaxMix
{
    /// <summary>
    /// Interaction logic for DeviceWindow.xaml
    /// </summary>
    public partial class DeviceWindow : Window
    {
        public DeviceWindow()
        {
            InitializeComponent();
        }
        
        protected override void OnContentRendered(EventArgs e)
        {
            RenderTargetBitmap renderTarget = new RenderTargetBitmap(128, 32, 96d, 96d, PixelFormats.Pbgra32);
            renderTarget.Render(UISourceElement);
            renderTarget.Freeze();

            FormatConvertedBitmap renderTargetConverted = new FormatConvertedBitmap(renderTarget, PixelFormats.BlackWhite, null, 0.1d);            
            UITargetImage.Source = renderTargetConverted;

            var length = renderTargetConverted.PixelWidth * renderTargetConverted.PixelHeight;
            var stride = renderTargetConverted.PixelWidth;
            byte[] pixels = new byte[length];
            renderTargetConverted.CopyPixels(pixels, stride, 0);

            var dataContext = DataContext as MainViewModel;
            dataContext.SetPixels(pixels);
        }
    }
}
