using NETCore.Encrypt;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;


namespace MFAAvalonia.Helper;

public static class SimpleEncryptionHelper
{
    public static string Generate()
    {
        // 跨平台系统特征参数
        var osDescription = RuntimeInformation.OSDescription;
        var osArchitecture = RuntimeInformation.OSArchitecture.ToString();
        var plainTextSpecificId = GetPlatformSpecificId();
        var machineName = Environment.MachineName;

        // 混合参数生成哈希
        var combinedString = $"{osDescription}_{osArchitecture}_{plainTextSpecificId}_{machineName}";
        return EncryptProvider.Sha256(combinedString);
    }

    public static string GetPlatformSpecificId()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // 使用WMI获取主板UUID
            using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
            foreach (ManagementObject obj in searcher.Get())
                return obj["UUID"].ToString();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // 读取DMI产品UUID
            return File.ReadAllText("/sys/class/dmi/id/product_uuid").Trim();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ioreg",
                    Arguments = "-rd1 -c IOPlatformExpertDevice",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var match = Regex.Match(output, @"IOPlatformUUID"" = ""(.+?)""");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
        return string.Empty;
    }
    private static string GetDeviceKeys()
    {
        var fingerprint = Generate();
        var key = fingerprint.Substring(0, 32);
        return key;
    }

    // 加密（自动绑定设备）
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;
        var key = GetDeviceKeys();
        var encryptedData = EncryptProvider.AESEncrypt(plainText, key);
        return encryptedData;
    }

    // 解密（仅当前设备可用）
    public static string Decrypt(string encryptedBase64)
    {
        try
        {
            var key = GetDeviceKeys();
            var decryptedData = EncryptProvider.AESDecrypt(encryptedBase64, key);
            return decryptedData;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
