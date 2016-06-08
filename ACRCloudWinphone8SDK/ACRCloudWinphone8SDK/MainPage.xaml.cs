using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using ACRCloud;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=391641 上有介绍

namespace ACRCloudWinphone8SDK
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page, IACRCloudClientListener
    {
        ACRCloudClient client = null;
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// 在此页将要在 Frame 中显示时进行调用。
        /// </summary>
        /// <param name="e">描述如何访问此页的事件数据。
        /// 此参数通常用于配置页。</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: 准备此处显示的页面。

            // TODO: 如果您的应用程序包含多个页面，请确保
            // 通过注册以下事件来处理硬件“后退”按钮:
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed 事件。
            // 如果使用由某些模板提供的 NavigationHelper，
            // 则系统会为您处理该事件。
        }

        private void startbtn_Click(object sender, RoutedEventArgs e)
        {
            var config = new Dictionary<string, object>();
            config.Add("host", "XXXXXXXX");
            // Replace "XXXXXXXX" below with your project's access_key and access_secret
            config.Add("access_key", "XXXXXXXX");
            config.Add("access_secret", "XXXXXXXX");
            client = new ACRCloudClient(this, config);
            client.Start();

            resultTextBlock.Text = "recording";
        }

        private void stopbtn_Click(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                client.Cancel();
            }
        }
        void IACRCloudClientListener.OnResult(string result)
        {
            resultTextBlock.Text = result;
        }
    }
}
