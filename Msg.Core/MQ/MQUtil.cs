using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Msg.Core.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Msg.Core.MQ
{
    public class MQUtil
    {
        private readonly static string url = string.Empty;
        public static void Init()
        {

        }
        static MQUtil()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("msgCoreConfig.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
            url = configuration["MQ:Url"];
        }
        public static async Task Produce(string topic, string message)
        {
            var config = new ProducerConfig { BootstrapServers = url };
            using (var p = new ProducerBuilder<Null, string>(config).Build())
            {
                try
                {
                    var dr = await p.ProduceAsync(topic, new Message<Null, string> { Value = message });
                    Console.WriteLine($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
                }
                catch (ProduceException<Null, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                }
            }
        }
        public static async Task Produce(ProduceEntity entity)
        {

        }
        public static void Consume<T>(string topic, Action<string> action)
        {
            var conf = new ConsumerConfig
            {
                GroupId = "consumer-group",
                BootstrapServers = url,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            using (var c = new ConsumerBuilder<Ignore, string>(conf).Build())
            {
                c.Subscribe(topic);

                CancellationTokenSource cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true; // prevent the process from terminating.
                    cts.Cancel();
                };

                try
                {
                    while (true)
                    {
                        try
                        {
                            var cr = c.Consume(cts.Token);
                            Console.WriteLine($"Consumed message '{cr.Message.Value}' at: '{cr.TopicPartitionOffset}'.");
                            string msg = cr.Message.Value;
                            if (!string.IsNullOrEmpty(msg))
                            {
                                //var msgObj = JsonConvert.DeserializeObject<T>(msg);
                                action(msg);
                            }
                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Error occured: {e.Error.Reason}");
                            //action(default(T));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    c.Close();
                }
            }
        }

        public static void Consume1(string topic, Action<string> action)
        {
            var conf = new ConsumerConfig
            {
                GroupId = "consumer-group",
                BootstrapServers = url,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            using (var c = new ConsumerBuilder<Ignore, string>(conf).Build())
            {
                c.Subscribe(topic);

                CancellationTokenSource cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true; // prevent the process from terminating.
                    cts.Cancel();
                };

                try
                {
                    while (true)
                    {
                        try
                        {
                            var cr = c.Consume(cts.Token);
                            Console.WriteLine($"Consumed message '{cr.Message.Value}' at: '{cr.TopicPartitionOffset}'.");
                            action(cr.Message.Value);
                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Error occured: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    c.Close();
                }
            }
        }
    }
}
