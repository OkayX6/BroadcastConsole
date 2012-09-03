using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using BroadcastConsole.Common.WinRT;
using BroadcastConsole.MetroClient.Common;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace BroadcastConsole.MetroClient
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class ConsolePage
    {
        private TcpConnection tcpClient;
        private readonly ObservableCollection<String> messageHistory = new ObservableCollection<String>();
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public ConsolePage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            DefaultViewModel["ChannelName"] = navigationParameter as string;

            DefaultViewModel["MessageHistory"] = messageHistory;

            this.tcpClient = new TcpConnection("127.0.0.1", 2009);
            this.tcpClient.Send("Cloud");

            Task.Factory.StartNew(() => BackgroundUpdater(tokenSource.Token));
        }

        private void BackgroundUpdater(CancellationToken token)
        {
            while (! token.IsCancellationRequested)
            {
                var message = this.tcpClient.Receive();

                Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () => messageHistory.Insert(0, message));
            }

            tcpClient.Dispose();
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            tokenSource.Cancel();
            tokenSource = new CancellationTokenSource();
        }

        private void OnAddMessage(object sender, RoutedEventArgs e)
        {
            messageHistory.Insert(0, "Hello " + Guid.NewGuid());
        }
    }
}
