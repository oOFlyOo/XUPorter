using UnityEngine;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.XCodeEditor;
#endif
using System.IO;

public static class XCodePostProcess
{
	public static string SelectProjmods = "";

//	[MenuItem("Custom/XCodePostProcess")]
	public static void PostProcessBuild()
	{
		OnPostProcessBuild (BuildTarget.iOS, "/Users/cilu2/Workspaces/H1/xcode/appstore");
	}

	#if UNITY_EDITOR
	[PostProcessBuild(999)]
	public static void OnPostProcessBuild( BuildTarget target, string pathToBuiltProject )
	{
		if (target != BuildTarget.iOS) {
			Debug.LogWarning("Target is not iPhone. XCodePostProcess will not run");
			return;
		}

        //得到xcode工程的路径
        string path = Path.GetFullPath(pathToBuiltProject);

        XCFileChecker.InitModeDict();
		// Create a new project object from build target
		XCProject project = new XCProject( pathToBuiltProject );

		// Find and run through all projmods files to patch the project.
		// Please pay attention that ALL projmods files in your project folder will be excuted!
		string applicationPath = Application.dataPath.Replace ("/Assets", "");
		string[] files = Directory.GetFiles( applicationPath + "/Mods", "*.projmods", SearchOption.TopDirectoryOnly );

        XCFileChecker.SortMods(ref files);

		string selectProjmods = EditorPrefs.GetString("selectProjmods");
		Debug.Log("selectProjmods = " + selectProjmods);

		foreach( string file in files ) {
			if (string.IsNullOrEmpty(selectProjmods))
			{
				UnityEngine.Debug.Log("ProjMod File: "+file);
				project.ApplyMod( file );
			}
			else
			{
				if (XCFileChecker.CheckApplyMod(file))
				{
					UnityEngine.Debug.Log("ProjMod File: "+file);
					project.ApplyMod( file );
				}
			}
		}

		//modify for xcode7
		project.overwriteBuildSetting("ENABLE_BITCODE", "NO", "all");


        XCFileChecker.EditCode(path);

//		if (selectProjmods == "kuaiyongSDK.projmods")
//		{
//			//编辑代码文件
//			EditorCodeForKuaiyong(path);
//		}

		// Finally save the xcode project
		project.Save();
    }
#endif

#if UNITY_IOS || UNITY_IPHONE
    #region 信鸽 自动打开PushNotification、勾选BackgroundModel
    static void OpenNotification(string pPath)
    {
        var tXcodeFilePath = string.Empty;
        string[] tSearchResult = System.IO.Directory.GetDirectories(pPath, "*.xcodeproj");
        if (tSearchResult.Length != 0) tXcodeFilePath = tSearchResult[0];
        if (!string.IsNullOrEmpty(tXcodeFilePath))
        {
            var tPath1 = Path.Combine(tXcodeFilePath, "project.pbxproj");
            var tResult1 = SetPushNotificationState(ReadText(tPath1), true);
            WriterText(tPath1, tResult1);
        }

        var tPath2 = Path.Combine(pPath, "info.plist");
        var tResult2 = InsertNode(ReadText(tPath2), "dict", "<key>UIBackgroundModes</key>\n<array>\n<string>remote-notification</string>\n</array>");
        WriterText(tPath2, tResult2);
    }

    static string ReadText(string pPath)
    {
        StreamReader tReader = null;
        var tTxt = string.Empty;
        try
        {
            tReader = new StreamReader(pPath);
            tTxt = tReader.ReadToEnd();
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
        finally
        {
            if (tReader != null)
            {
                tReader.Close();
                tReader.Dispose();
            }
        }
        return tTxt;
    }
    static void WriterText(string pPath, string val)
    {
        StreamWriter tWriter = null;
        try
        {
            tWriter = new StreamWriter(pPath);
            tWriter.Write(val);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
        finally
        {
            if (tWriter != null)
            {
                tWriter.Close();
                tWriter.Dispose();
            }
        }
    }

    static string SetPushNotificationState(string pSourceTxt, bool pEnable)
    {
        try
        {
            var tPushEnable = pEnable ? "1" : "0";
            var tMatch = Regex.Match(pSourceTxt, @"targets.*?(?<targetId>\b\w{24}\b)", RegexOptions.Singleline);
            if (tMatch.Success)
            {
                var tTargetID = tMatch.Groups["targetId"].Value;
                var tPushInputStr = "\ncom.apple.Push = {\nenabled = " + tPushEnable + ";\n};\n";
                var tInsertTxt = "\n" + tTargetID + " = {\nSystemCapabilities = {" + tPushInputStr + "};\n};";
                var tRegex = new Regex(@"TargetAttributes\s*=\s*{", RegexOptions.Singleline);

                pSourceTxt = tRegex.Replace(pSourceTxt, match =>
                {
                    return match.Value + tInsertTxt;
                }, 1);
            }
        }
        catch (System.Exception ex) { Debug.LogError(ex.ToString()); }
        return pSourceTxt;
    }

    static string InsertNode(string pSourceTxt, string pParentNode, string pInsertTxt)
    {
        Regex tRegex = new Regex("(?<=>).*?(?=</" + pParentNode + ">)", RegexOptions.Singleline | RegexOptions.RightToLeft);
        pSourceTxt = tRegex.Replace(pSourceTxt, string.Format("\n{0}\n", pInsertTxt), 1);
        return pSourceTxt;
    }
    #endregion
#endif


	public static void Log(string message)
	{
		UnityEngine.Debug.Log("PostProcess: "+message);
	}
}
