//https://gist.github.com/adrenalinehit
//https://www.sslshopper.com/ssl-converter.html

using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;

// including the M2Mqtt Library
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace ExampleCshapMqttClientAWS
{
    public partial class Form1 : Form
    {
        MqttClient client;
        string clientId;
        X509Certificate2 clientCert;
        X509Certificate caCert;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            DisconnectMqttBroker();

            string brokerAddress = tbEndPoint.Text;
            int brokerPort = Int32.Parse(tbPort.Text);
            UpdateStateConnect($"Connecting to {brokerAddress}...");
            try
            {
                //convert to pfx using openssl
                //you'll need to add these two files to the project and copy them to the output
                clientCert = new X509Certificate2("C:/Users/tungn/Downloads/ExampleCshapMqttClientAWS/8a03a801dd-certificate.pem.pfx", "Tung12051994");
                
                //this is the AWS caroot.pem file that you get as part of the download
                caCert = X509Certificate.CreateFromCertFile("C:/Users/tungn/Downloads/ExampleCshapMqttClientAWS/8a03a801dd-certificate.pem.crt");// this doesn't have to be a new X509 type...
                //caCert = X509Certificate.CreateFromSignedFile("C:/Users/tungn/Downloads/ExampleCshapMqttClientAWS/rootCA.pem");

                client = new MqttClient(brokerAddress, brokerPort, true, caCert, clientCert, MqttSslProtocols.TLSv1_2);
                //client = new MqttClient(brokerAddress);

                // register a callback-function (we have to implement, see below) which is called by the library when a message was received
                client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

                // use a unique id as client id, each time we start the application
                clientId = Guid.NewGuid().ToString();

                client.Connect(clientId);
                if(client.IsConnected)
                    UpdateStateConnect("Connected!");
            }
            catch (Exception ex)
            {
                UpdateStateConnect($"Failue connection: {ex.Message}");
                //MessageBox.Show("Error connect: " + ex.Message);
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
            if(client == null)
                UpdateStateConnect("Not Connect!");
        }

        private void DisconnectMqttBroker()
        {
            if ((client != null)&&(client.IsConnected))
            {
                client.Disconnect();
                client = null;
            }
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

        // this code runs when a message was received
        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);
            this.Invoke((MethodInvoker)delegate
            {
                tbReceived.Text = ReceivedMessage;
            });
        }
    }
}
