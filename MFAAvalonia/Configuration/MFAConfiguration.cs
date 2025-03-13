using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAAvalonia.Helper;
using MFAAvalonia.Helper.Converters;
using MFAWPF.Helper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MFAAvalonia.Configuration;

public partial class MFAConfiguration : ObservableObject
{
    [ObservableProperty] private string _name;

    [ObservableProperty] private string _fileName;

    [ObservableProperty] private Dictionary<string, object> _config = new();

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


    public void SetConfig(string key, object? value)
    {
        if (Config == null || value == null) return;
        Config[key] = value;
        JsonHelper.SaveJson(FileName, Config, new MaaInterfaceSelectOptionConverter(false));
    }

    public T GetConfig<T>(string key, T defaultValue)
    {
        if (Config.TryGetValue(key, out var data) == true)
        {
            try
            {
                if (data is long longValue && typeof(T) == typeof(int))
                {
                    return (T)(object)Convert.ToInt32(longValue);
                }

                if (data is JArray jArray)
                {
                    return jArray.ToObject<T>();
                }

                if (data is T t)
                {
                    return t;
                }
            }
            catch (Exception e)
            {
                LoggerHelper.Error("在进行类型转换时发生错误!", e);
            }
        }

        return defaultValue;
    }
    public override string ToString() => Name;
}
