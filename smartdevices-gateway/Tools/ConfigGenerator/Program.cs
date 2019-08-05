﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using ConfigGenerator.Configs.General;
using ConfigGenerator.Configs.Special;
using ConfigGenerator.Configs.UseCases;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SmartDevicesGateway.Model.Config.SDConfig;
using SmartDevicesGateway.Model.Ui;
using SmartDevicesGateway.Processing.Controller.SmartDevice;
using Formatting = Newtonsoft.Json.Formatting;

[assembly: XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace ConfigGenerator
{
    public class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string DefaultPath
            = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + 
              (Debugger.IsAttached ? @"\..\..\..\..\..\SmartDevicesGateway.Api\config" : "config");

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Converters = new JsonConverter[]
            {
                new IsoDateTimeConverter
                {
                    DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffzzz"
                    //DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss"
                }
            }
        };

        static void Main(string[] args)
        {
            Logger.Debug($"{nameof(Main)} called with {string.Join(" ", args)}.");

            var config = new SmartDeviceConfig();
            config = config.MergeWith(new DefaultConfig().GenerateConfig());
//            config = config.MergeWith(new Uc1ToolBreakage().GenerateConfig());
//            config = config.MergeWith(new Uc2MachineSetUp().GenerateConfig());
            config = config.MergeWith(new Uc5LiveData().GenerateConfig());
            
            Logger.Info($"Config generated.");
            Logger.Info($"Writing config files...");

            Write(new UserConfig
            {
                Users = config.Users
            }, "UserConfig");

            Write(new SmartDevicesGateway.Model.Config.SDConfig.SmartDeviceConfig
            {
                DeviceGroups = config.DeviceGroups
            }, "SmartDeviceConfig");

            Write(new UiConfig
            {
                ActionDefinitions = config.Actions.Select(x => new UiAction(x)),
                Uis = config.Uis,
                TabDefinitions = config.TabConfig,
                ValueDefinitions = config.ValueDefinitions,

                Info = config.AppInfo
            }, "UiConfig");

            Logger.Info("All files written.");
            Logger.Info("Press any key to exit.");
            Console.ReadLine();
        }

        private static void Write<T>(T t, string configName, string path = null)
        {
            if (path == null)
            {
                path = $@"{DefaultPath}\{configName}.json";
            }
            path = Path.GetFullPath(path);

            Logger.Info($"Write {configName} to {path}.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // use temp file to avoid corruption
            var tmpPath = $"{path}_{DateTime.Now:yyyyMMdd_hhmmss}.json";
            try
            {
                var json = JsonConvert.SerializeObject(t, Formatting.Indented, Settings);
                File.WriteAllText(tmpPath, json, Encoding.UTF8);
                File.Delete(path);
                File.Move(tmpPath, path);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
            finally
            {
                try
                {
                    File.Delete(tmpPath);
                }
                catch (Exception)
                {
                    // do nothing
                }
            }
        }
    }
}
