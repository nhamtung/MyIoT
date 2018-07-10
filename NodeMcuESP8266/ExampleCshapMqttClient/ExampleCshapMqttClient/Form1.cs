
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;


// including the M2Mqtt Library
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace ExampleCshapMqttClient
{
    public partial class Form1 : Form
    {
        MqttClient client;
        string clientId;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            DisconnectMqttBroker();
            try
            {
                string BrokerAddress = tbEndPoint.Text;
                UpdateStateConnect($"Connecting to {BrokerAddress}...");
                client = new MqttClient(BrokerAddress);

                // register a callback-function (we have to implement, see below) which is called by the library when a message was received
                client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

                // use a unique id as client id, each time we start the application
                clientId = Guid.NewGuid().ToString();
                //clientId = tbAccessKeyId.Text;

                client.Connect(clientId);
                UpdateStateConnect("Connected!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connect to AWS: " + ex.Message);
            }
        }

        private void UpdateStateConnect(string content)
        {
            this.Invoke((MethodInvoker)delegate
            {
                txtStateConnect.Text = content;
            });
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            DisconnectMqttBroker();
            UpdateStateConnect("Not Connect!");
        }
        private void DisconnectMqttBroker()
        {
            if (client != null)
            { 
                client.Disconnect();
                client = null;
            }
        }

        // this code runs when a message was received
        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);
            this.Invoke((MethodInvoker)delegate
            {
                tbReceived.Text = ReceivedMessage;
            });
        }

        private void btnSubscribe_Click(object sender, EventArgs e)
        {
            if (tbTopicSub.Text != "")
            {
                // whole topic
                string Topic = tbTopicSub.Text;

                // subscribe to the topic with QoS 2
                client.Subscribe(new string[] { Topic }, new byte[] { 2 });   // we need arrays as parameters because we can subscribe to different topics with one call
                tbReceived.Text = "";
            }
            else
            {
                MessageBox.Show("You have to enter a topic to subscribe!");
            }
        }

        private void btnPublish_Click(object sender, EventArgs e)
        {
            if (tbTopicPub.Text != "")
            {
                // whole topic
                string Topic = tbTopicPub.Text;

                // publish a message with QoS 2
                client.Publish(Topic, Encoding.UTF8.GetBytes(tbPublish.Text), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
            }
            else
            {
                MessageBox.Show("You have to enter a topic to publish!");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectMqttBroker();
        }
    }
}
