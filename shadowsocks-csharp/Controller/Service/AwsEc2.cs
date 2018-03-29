using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime.CredentialManagement;
using Shadowsocks.Model;

namespace Shadowsocks.Controller.Service
{
    class AwsEc2
    {
        AmazonEC2Client ec2Client;
        ShadowsocksController controller;

        public AwsEc2(ShadowsocksController controller, string accessKey, string secretKey)
        {
            // aws 配置用户名和密码
            AddOrUpdateAppSettings("AWSProfileName", "aws_profile");
            var options = new CredentialProfileOptions
            {
                AccessKey = accessKey,
                SecretKey = secretKey
            };
            var profile = new Amazon.Runtime.CredentialManagement.CredentialProfile("aws_profile", options);
            var netSDKFile = new NetSDKCredentialsFile();
            netSDKFile.RegisterProfile(profile);

            // 初始化客户端
            ec2Client = new AmazonEC2Client(RegionEndpoint.USWest2);
            this.controller = controller;
        }

        public List<Instance> ListInstance()
        {
            
            var instances = ec2Client.DescribeInstances();
            return instances.Reservations[0].Instances;
        }

        private void StartInstance(List<string> ids)
        {
            if (ids.Count <= 0) return;
            var sir = new StartInstancesRequest
            {
                InstanceIds = ids
            };

            ec2Client.StartInstances(sir);
        }

        private void StopInstance(List<string> ids)
        {
            if (ids.Count <= 0) return;
            var sir = new StopInstancesRequest
            {
                InstanceIds = ids
            };

            ec2Client.StopInstances(sir);
        }

        // 启动 Aws 服务器
        public List<string> Up()
        {
            List<Instance> instances; 
            while (true)
            {
                List<string> stoppedIds = new List<string>();
                List<string> pendingIds = new List<string>();
                List<string> stoppingIds = new List<string>();

                // 获取实例状态
                instances = ListInstance();

                // 找出没有启动的项目
                foreach (var instance in instances)
                {
                    // 过滤非 ss 主机
                    bool isSSEcs = false;
                    foreach (var tag in instance.Tags)
                    {
                        // 有标识的才当做 ss 虚拟机
                        if (tag.Key is "for" && tag.Value is "fuckwall")
                        {
                            isSSEcs = true;
                            break;
                        }
                    }
                    if (isSSEcs)
                    {
                        // 如果正在停止
                        if (instance.State.Name.Value.Equals("stopping"))
                        {
                            stoppingIds.Add(instance.InstanceId);
                        }

                        // 如果正在启动
                        if (instance.State.Name.Value.Equals("pending"))
                        {
                            pendingIds.Add(instance.InstanceId);
                        }

                        // 如果是停止状态
                        if (instance.State.Name.Value.Equals("stopped"))
                        {
                            stoppedIds.Add(instance.InstanceId);
                        }
                    }
                }
               
                // 确认虚拟机是有没有起来的项
                if (stoppedIds.Count == 0 && pendingIds.Count == 0 && stoppingIds.Count == 0)
                {
                    break;
                }
                else
                {
                    // 等待 3s 再次重试
                    StartInstance(stoppedIds);
                    Thread.Sleep(3000);
                }
            }
           
            // 获取虚拟机信息
            instances = ListInstance();
            List<String> instancesList = new List<string>();
            List<Server> servers = new List<Server>();
            foreach (var instance in instances)
            {
                foreach (var tag in instance.Tags)
                {
                    // 有标识的才当做 ss 虚拟机
                    if (tag.Key is "for" && tag.Value is "fuckwall")
                    {
                        instancesList.Add(instance.PublicDnsName);
                        var server = new Server();
                        server.server = instance.PublicIpAddress;
                        servers.Add(server);
                    }
                }
            }

            // 保存虚拟机信息到配置文件
            controller.SaveServers(servers, 1080);
            controller.Start();

            return instancesList;
        }

        // 关闭 Aws 服务器
        public void Down()
        {
            // 确认虚拟机状态
            List<string> runningIds = new List<string>();
            var instances = ListInstance();
            foreach (var instance in instances)
            {
                bool isSSEc2 = false;
                foreach (var tag in instance.Tags)
                {
                    // 有标识的才当做 ss 虚拟机
                    if (tag.Key is "for" && tag.Value is "fuckwall")
                    {
                        isSSEc2 = true;
                        break;
                    }
                }
                if (isSSEc2 && !instance.State.Name.Value.Equals("stopped"))
                {
                    runningIds.Add(instance.InstanceId);
                }
            }
            StopInstance(runningIds);
        }

        public static void AddOrUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }
    }
}
