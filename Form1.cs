using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MQTTclient
{
    public partial class Form1 : Form
    {        
        public Form1()
        {
            InitializeComponent();
            client = new MqttFactory().CreateManagedMqttClient();
            client.UseConnectedHandler(x => WriteTextSafe("Connected successfully with MQTT Brokers.\n"));
            client.UseDisconnectedHandler(x => WriteTextSafe("Disconnected from MQTT Brokers.\n"));               
           
            client.UseApplicationMessageReceivedHandler(x =>
            {
                try
                {
                    string topic = x.ApplicationMessage.Topic;
                    if (string.IsNullOrWhiteSpace(topic) == false)
                    {
                        string payload = Encoding.UTF8.GetString(x.ApplicationMessage.Payload);                       
                        WriteTextSafe($"Topic: {topic}. Message Received: {payload}");
                    }
                }
                catch (Exception ex)
                {
                    WriteTextSafe(ex.Message);
                }
            });            
        }

        public static IManagedMqttClient client; // { get; private set; }

        private  delegate void SafeCallDelegate(string text);
        private void WriteTextSafe(string text)
        {
            if (labelMessages.InvokeRequired)
            {
                var d = new SafeCallDelegate(WriteTextSafe);
                labelMessages.Invoke(d, new object[] { text });
            }
            else
            {
                labelMessages.Text += text + "\n";
            }
        }

        public static async Task ConnectAsync(string mqttURI = "lukan.sytes.net")
        {
            string clientId = Guid.NewGuid().ToString();
            string mqttUser = "";
            string mqttPassword = "";
            int mqttPort = 1883;
            bool mqttSecure = false;

            var messageBuilder = new MqttClientOptionsBuilder()
              .WithClientId(clientId)
              .WithCredentials(mqttUser, mqttPassword)
              .WithTcpServer(mqttURI, mqttPort)
              .WithCleanSession();
            var options = mqttSecure
              ? messageBuilder
                .WithTls()
                .Build()
              : messageBuilder
                .Build();
            ManagedMqttClientOptions managedOptions = new ManagedMqttClientOptionsBuilder()
              .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
              .WithClientOptions(options)
              .Build();
            
            await client.StartAsync(managedOptions);
        }
                     
        /// <summary>
        /// Publish Message.
        /// </summary>
        /// <param name="topic">Topic.</param>
        /// <param name="payload">Payload.</param>
        /// <param name="retainFlag">Retain flag.</param>
        /// <param name="qos">Quality of Service.</param>
        /// <returns>Task.</returns>
        public static async Task PublishAsync(string topic, string payload, bool retainFlag = true, int qos = 1) =>
          await client.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
            .WithRetainFlag(retainFlag)
            .Build());

        /// <summary>
        /// Subscribe topic.
        /// </summary>
        /// <param name="topic">Topic.</param>
        /// <param name="qos">Quality of Service.</param>
        /// <returns>Task.</returns>
        public static async Task SubscribeAsync(string topic, int qos = 1) =>
          await client.SubscribeAsync(new TopicFilterBuilder()
            .WithTopic(topic)
            .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
            .Build());

        private async void buttonOpen_Click(object sender, EventArgs e)
        {
            try
            {
                await ConnectAsync(textBoxUrl.Text);
            }
            catch (Exception ex)
            {
                labelMessages.Text = ex.Message;
            }            
        }

        private async void buttonPublish_Click(object sender, EventArgs e)
        {
            try
            {
                await PublishAsync(textBoxTopic.Text, textBoxMessage.Text);
            }
            catch (Exception ex)
            {

                labelMessages.Text = ex.Message + "\nOtwórz połączenie z brokerem\n";
            }
        }
     
        private async void buttonSubscribe_Click(object sender, EventArgs e)
        {
            string topic = textBoxTopic.Text;
            listBox1.Items.Add(topic);
            await SubscribeAsync(topic);
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            client.StopAsync();            
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            labelMessages.Text = "";
        }

        private async void buttonUnsubscribe_Click(object sender, EventArgs e)
        {
            string topic = textBoxTopic.Text;
            listBox1.Items.Remove(topic);
            await client.UnsubscribeAsync(topic);
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            textBoxTopic.Text = listBox1.SelectedItem.ToString();
        }
    }
}
