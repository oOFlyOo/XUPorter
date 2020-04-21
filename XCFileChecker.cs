using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.XCodeEditor;


public static class XCFileChecker
{
    private static SPChannel _curChannel;

    private static readonly string[] _modSortArray =
    {
        "ShareSDK",
        "Bugly",
    };

    /// <summary>
    /// 初始化
    /// </summary>
    public static void InitModeDict()
    {
        _curChannel = SPSdkManager.SpChannelDic()[GameSetting.LoadGameSettingData().channel];
    }


    /// <summary>
    /// 某些mod排在后面比较方便
    /// 如果有需要排在前面的，再进行重构
    /// </summary>
    /// <param name="mods"></param>
    public static void SortMods(ref string[] mods)
    {
        if (mods != null && mods.Length > 1)
        {
            var modList = mods.ToList();
            modList.Sort(CompareMod);
            mods = modList.ToArray();
        }
    }

    private static int CompareMod(string mod1, string mod2)
    {
        return Array.IndexOf(_modSortArray, Path.GetFileNameWithoutExtension(mod1))
            .CompareTo(Array.IndexOf(_modSortArray, Path.GetFileNameWithoutExtension(mod2)));
    }


    /// <summary>
    /// 检查哪些需要应用
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static bool CheckApplyMod(string file)
    {
        if (file.Contains(_curChannel.projmods) ||
            file.Contains("Extra.projmods") ||
            file.Contains("ImagePicker.projmods") ||
            file.Contains("Bugly.projmods") ||
            file.Contains("GCloudVoice.projmods")||
            file.Contains("Jpush.projmods"))
        {
            return true;
        }

        if (file.Contains("iosNativeSDK.projmods") &&
            (_curChannel.symbol != null &&
             _curChannel.symbol.Contains("IOSNATIVE_ENABLED")))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 不可以使用宏，否则会跪
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool CheckAddFile(string path)
    {
        var ignoreFiles = new string[]
        {
            "extends/WeChatSDK/libWeChatSDK.a",
            "/Info.plist",
            "/InfoPlist.strings",
        };
        if (ignoreFiles.Any(path.Contains))
        {
            return false;
        }

        return true;
    }


    /// <summary>
    /// 编辑代码
    /// </summary>
    /// <param name="filePath"></param>
    public static void EditCode(string filePath)
    {
        string.Format("{0}", "ken");
        XClass UnityAppController = new XClass(filePath + "/Classes/UnityAppController.mm");

        if (_curChannel.symbol != null && _curChannel.symbol.Contains("ENABLE_XINGE"))
        {
            UnityAppController.WriteBelow("#include \"PluginBase/AppDelegateListener.h\"", "#import \"XinGeIOSSDK.h\"");
            UnityAppController.WriteBelow("[KeyboardDelegate Initialize];", @"
    [XinGeIOSSDK handleLaunching];
    [XinGeIOSSDK registerAPNS];
");
            UnityAppController.WriteBelow("UnitySendDeviceToken(deviceToken);", @"    
    [XinGeIOSSDK saveDeviceToken:deviceToken];
");
        }
    }
}