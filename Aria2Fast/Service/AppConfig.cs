﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Diagnostics;
using PropertyChanged;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Aria2Fast.Service.Model.SubscriptionModel;

namespace Aria2Fast.Service
{
    [AddINotifyPropertyChangedInterface]
    public class AppConfigData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        /// <summary>
        /// 分区路径->存储路径
        /// 【RPC URL->存储路径】
        /// </summary>
        public Dictionary<string, string> AddTaskSavePathDict { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 设备默认选择订阅的分区对应表 
        /// 【设备ID->分区路径】
        /// </summary>
        public Dictionary<string, string> AddSubscriptionSavePartitionDict { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 分区路径->存储路径
        /// 【分区路径->存储路径】
        /// </summary>
        public Dictionary<string, string> AddSubscriptionSavePathDict { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 常用过滤器名称（不区分RPC了）
        /// </summary>
        public List<SubscriptionFilterModel> AddSubscriptionFilterList { get; set; } = new List<SubscriptionFilterModel>();

        //OSS相关设置

        public bool OSSSynchronizeOpen { get; set; } = false;


        public string OSSEndpoint { get; set; } = string.Empty;

        public string OSSBucket { get; set; } = string.Empty;

        public string OSSAccessKeyId { get; set; } = string.Empty;

        public string OSSAccessKeySecret { get; set; } = string.Empty;


        public bool PushDeerOpen { get; set; } = false;

        public string PushDeerKey { get; set; } = string.Empty;


        public bool SubscriptionProxyOpen { get; set; } = false;

        public string SubscriptionProxy { get; set; } = string.Empty;

        /// <summary>
        /// OpenAIKey 用于RSS为集中订阅时，使用OpenAI提取连接中的作品名称
        /// </summary>
        public string OpenAIKey { get; set; } = string.Empty;

        public string OpenAIProxy { get; set; } = string.Empty;

        public bool OpenAIOpen { get; set; } = false;


        /// <summary>
        /// 用于第三方转发服务的实现
        /// </summary>
        public string OpenAIHost { get; set; } = string.Empty;

        //当前客户端ID
        public string ClientId { get; set; } = string.Empty;


        public string Aria2Rpc { get; set; } = string.Empty;

        public string Aria2Token { get; set; } = string.Empty;


        public string Aria2RpcHost
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Aria2Rpc) && Uri.TryCreate(Aria2Rpc, new UriCreationOptions(), out Uri? result))
                {
                    return result.Host;
                }
                return "";
            }
        }
    }

    /// <summary>
    /// 配置项读取、写入、存储逻辑
    /// </summary>
    public class AppConfig
    {
        private static readonly Lazy<AppConfig> lazy = new Lazy<AppConfig>(() => new AppConfig());
        
        public static AppConfig Instance => lazy.Value;

        public AppConfigData ConfigData { set; get; } = new AppConfigData();

        private string _configPath = AppContext.BaseDirectory + @"Config.json";

        private object _lock = new object();

        private AppConfig()
        {
            Init();
            Debug.WriteLine(_configPath);
        }

        public void InitDefault() //载入默认配置
        {
            ConfigData.ClientId = Guid.NewGuid().ToString();
        }


        public bool Init()
        {
            try
            {

                Debug.WriteLine($"初始化配置" + Thread.CurrentThread.ManagedThreadId);
                if (File.Exists(_configPath) == false)
                {
                    Debug.WriteLine($"默认初始化");
                    InitDefault();
                    Save();
                }



                lock (_lock)
                {
                    var fileContent = File.ReadAllText(_configPath);
                    var appData = JsonConvert.DeserializeObject<AppConfigData>(fileContent);
                    ConfigData = appData;
                    ConfigData.PropertyChanged += AppConfigData_PropertyChanged;
                }

                if (string.IsNullOrWhiteSpace(ConfigData.ClientId))
                {
                    ConfigData.ClientId = Guid.NewGuid().ToString();
                    Save();
                }

                return true;
            }
            catch (Exception ex)
            {
                InitDefault();
                Save();
                Debug.WriteLine(ex);
                return false;
            }
            finally
            {
                if (ConfigData.AddSubscriptionFilterList.Count == 0)
                {
                    ConfigData.AddSubscriptionFilterList.Add(new SubscriptionFilterModel() { Filter = "简体"});
                    ConfigData.AddSubscriptionFilterList.Add(new SubscriptionFilterModel() { Filter = "简中" });
                    ConfigData.AddSubscriptionFilterList.Add(new SubscriptionFilterModel() { Filter = "简繁" });
                    ConfigData.AddSubscriptionFilterList.Add(new SubscriptionFilterModel() { Filter = "简日" });
                }
            }
        }

        private void AppConfigData_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Aria2Rpc" || e.PropertyName == "Aria2Token")
            {
                Aria2ApiManager.Instance.UpdateRpc();
            }

            Save();
        }

        public void Save()
        {
            try
            {
                lock (_lock)
                {
                    var data = JsonConvert.SerializeObject(ConfigData);
                    Debug.WriteLine($"存储配置{Thread.CurrentThread.ManagedThreadId} {data}");
                    File.WriteAllText(_configPath, data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }


    }
}
