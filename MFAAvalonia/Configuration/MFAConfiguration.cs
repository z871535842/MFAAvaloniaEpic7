using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAAvalonia.Helper;
using MFAAvalonia.Helper.Converters;
using MFAWPF.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MFAAvalonia.Configuration;

public partial class MFAConfiguration(string name, string fileName, Dictionary<string, object> config) : ObservableObject
{
    [ObservableProperty] private string _name = name;

    [ObservableProperty] private string _fileName = fileName;

    [ObservableProperty] private Dictionary<string, object> _config = config;

    [RelayCommand]
    private void DeleteConfiguration()
    {
        if (ConfigurationManager.Configs.All(c => c.Name != Name))
        {
            LoggerHelper.Error($"配置 {Name} 不存在");
            return;
        }

        if (ConfigurationManager.ConfigName == Name)
        {
            LoggerHelper.Error($"不能删除当前使用的配置 {Name}");
            return;
        }

        ConfigurationManager.Configs.Remove(this);
        File.Delete(GetConfigFilePath());
    }

    private string GetConfigFilePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            $"YourAppName/config/{FileName}.json");


    public void SetValue(string key, object? value)
    {
        if (Config == null || value == null) return;
        Config[key] = value;
        JsonHelper.SaveConfig(FileName, Config, new MaaInterfaceSelectOptionConverter(false));
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        if (Config.TryGetValue(key, out var data))
        {
            try
            {
                if (data is long longValue && typeof(T) == typeof(int))
                {
                    return (T)(object)Convert.ToInt32(longValue);
                }

                if (data is T t)
                {
                    return t;
                }

                if (data is JArray jArray)
                {
                    return jArray.ToObject<T>();
                }
                
                if (data is JObject jObject)
                {
                    return jObject.ToObject<T>();
                }
            }
            catch (Exception e)
            {
                LoggerHelper.Error("在进行类型转换时发生错误!", e);
            }
        }

        return defaultValue;
    }
    public T GetValue<T>(string key, T defaultValue, Dictionary<object, T> options)
    {

        if (Config.TryGetValue(key, out var data))
        {
            if (options != null && options.TryGetValue(data, out var result))
            {
                return result;
            }
            try
            {
                if (data is long longValue && typeof(T) == typeof(int))
                {
                    return (T)(object)Convert.ToInt32(longValue);
                }

                if (data is T t)
                {
                    return t;
                }

                if (data is JArray jArray)
                {
                    return jArray.ToObject<T>();
                }

                if (data is JObject jObject)
                {
                    return jObject.ToObject<T>();
                }
            }
            catch (Exception e)
            {
                LoggerHelper.Error("在进行类型转换时发生错误!", e);
            }
        }

        return defaultValue;
    }

    public T GetValue<T>(string key, T defaultValue, T? noValue = default, params JsonConverter[] valueConverters)
    {

        if (Config.TryGetValue(key, out var data))
        {
            try
            {
                var settings = new JsonSerializerSettings();
                foreach (var converter in valueConverters)
                {
                    settings.Converters.Add(converter);
                }
                var result = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(data), settings) ?? defaultValue;
                if (result.Equals(noValue))
                    return defaultValue;
                return result;
            }
            catch (Exception e)
            {
                LoggerHelper.Error($"类型转换失败: {e.Message}");
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public bool TryGetValue<T>(string key, out T output, params JsonConverter[] valueConverters)
    {
        if (Config.TryGetValue(key, out var data))
        {
            try
            {
                var settings = new JsonSerializerSettings();
                foreach (var converter in valueConverters)
                {
                    settings.Converters.Add(converter);
                }
                output = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(data), settings) ?? default;
                return true;
            }
            catch (Exception e)
            {
                LoggerHelper.Error($"类型转换失败: {e.Message}");
            }
        }
        output = default;
        return false;
    }

    public override string ToString() => Name;

    public MFAConfiguration SetName(string name)
    {
        Name = name;
        return this;
    }
    public MFAConfiguration SetFileName(string name)
    {
        FileName = name;
        return this;
    }

    public MFAConfiguration SetConfig(Dictionary<string, object> config)
    {
        Config = config;
        return this;
    }
}
