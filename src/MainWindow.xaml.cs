using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace M2Client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private MqttClient client = null;
        private ObservableCollection<string> subTopics = new ObservableCollection<string>();

        public ObservableCollection<string> Topics
        {
            get { return subTopics; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string proname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(proname));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrEmpty(txtServer.Text))
            {
                MessageBox.Show("请输入服务器地址！");
                return;
            }
            string host = txtServer.Text;

            int port = 0;

            if (txtServer.Text.IndexOf(':') > 0)
            {
                string[] items = txtServer.Text.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length == 2)
                {
                    host = items[0];
                    if (!int.TryParse(items[1], out port))
                    {
                        MessageBox.Show("请输入有效的端口号！");
                        return;
                    }
                }
            }
            if (string.Compare(host, "localhost", true) == 0)
                host = IPAddress.Loopback.ToString();

            string clientId = Guid.NewGuid().ToString();
            try
            {
                if (port == 0)
                {
                    client = new MqttClient(host);
                }
                else
                {
                    client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);
                }

                client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                client.Connect(clientId);

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
                return;
            }

            btnTopic.IsEnabled = true;
            btnConnect.IsEnabled = false;
            btnStop.IsEnabled = true;
            btnPubTopic.IsEnabled = true;
        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // handle message received
            this.Dispatcher.Invoke(() =>
            {
                txtResult.AppendText(string.Format("{0}\t", txtResult.LineCount));
                txtResult.AppendText(e.Topic);
                txtResult.AppendText("\t");
                txtResult.AppendText(Encoding.Default.GetString(e.Message));
                txtResult.AppendText(Environment.NewLine);
                txtResult.ScrollToEnd();
            });
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                client.Disconnect();
                btnConnect.IsEnabled = true;
                btnTopic.IsEnabled = false;
                btnStop.IsEnabled = false;
                btnPubTopic.IsEnabled = false;
            }
        }

        private void btnTopic_Click(object sender, RoutedEventArgs e)
        {
            string topic = txtTopic.Text;
            if (!string.IsNullOrEmpty(topic) && client != null)
            {   
                if (!subTopics.Contains(topic))
                {
                    client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                    subTopics.Add(topic);
                    OnPropertyChanged("Topics");
                }
                
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (client != null)
            {
                if (subTopics.Count > 0)
                {
                    client.Unsubscribe(subTopics.ToArray());
                    subTopics.Clear();
                }
                client.Disconnect();
            }
        }

        private void btnPubTopic_Click(object sender, RoutedEventArgs e)
        {
            string topic = txtPubTopic.Text;
            string content = txtPubContent.Text;
            if (!string.IsNullOrEmpty(topic) && !string.IsNullOrEmpty(content) && client != null)
            {
                //client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                try
                {
                    int r = client.Publish(topic, Encoding.Default.GetBytes(content));
                    if (r == 0)
                    {
                        MessageBox.Show("发送失败！");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
               
            }
        }

        private void btnClr_Click(object sender, RoutedEventArgs e)
        {
            txtResult.Clear();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (lstTopics.SelectedValue == null) return;
            string topic = lstTopics.SelectedValue.ToString();
            try
            {
                if (client != null && client.IsConnected)
                {
                    client.Unsubscribe(new string[] { topic });
                    subTopics.Remove(topic);
                    OnPropertyChanged("Topics");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }
    }
}