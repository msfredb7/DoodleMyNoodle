﻿#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorLaunchData
{
    public static bool developmentBuild
    {
        get => EditorPrefs.GetBool("EditorLaunch-devBuild", true);
        set => EditorPrefs.SetBool("EditorLaunch-devBuild", value);
    }

    public static string symbolsProfile
    {
        get => EditorPrefs.GetString("EditorLaunch-symbolsProfile", "");
        set => EditorPrefs.SetString("EditorLaunch-symbolsProfile", value);
    }

    public static bool allowDebugging
    {
        get => EditorPrefs.GetBool("EditorLaunch-allowDebugging", true);
        set => EditorPrefs.SetBool("EditorLaunch-allowDebugging", value);
    }

    public static int profileLocalId
    {
        get => EditorPrefs.GetInt("EditorLaunch-profile", 0);
        set => EditorPrefs.SetInt("EditorLaunch-profile", value);
    }

    public static bool playFromScratch
    {
        get => EditorPrefs.GetBool("EditorLaunch-fromScratch", true);
        set => EditorPrefs.SetBool("EditorLaunch-fromScratch", value);
    }

    public static string level
    {
        get => EditorPrefs.GetString("EditorLaunch-level");
        set => EditorPrefs.SetString("EditorLaunch-level", value);
    }

    public static string serverName
    {
        get => EditorPrefs.GetString("EditorLaunch-serverName");
        set => EditorPrefs.SetString("EditorLaunch-serverName", value);
    }

    public static string extraArguments
    {
        get => EditorPrefs.GetString("EditorLaunch-extraArguments");
        set => EditorPrefs.SetString("EditorLaunch-extraArguments", value);
    }

    public static bool serverIsHeadless
    {
        get => EditorPrefs.GetBool("EditorLaunch-serverIsHeadless", false);
        set => EditorPrefs.SetBool("EditorLaunch-serverIsHeadless", value);
    }

    public static bool playOnline
    {
        get => EditorPrefs.GetBool("EditorLaunch-playOnline", true);
        set => EditorPrefs.SetBool("EditorLaunch-playOnline", value);
    }

    public static int whoIsServerId
    {
        get => EditorPrefs.GetInt("EditorLaunch-whoIsServerId", 0);
        set => EditorPrefs.SetInt("EditorLaunch-whoIsServerId", value);
    }

    public static int whoIsEditorId
    {
        get => EditorPrefs.GetInt("EditorLaunch-whoIsEditorId", 0);
        set => EditorPrefs.SetInt("EditorLaunch-whoIsEditorId", value);
    }

    public static bool launchOverrideScreen
    {
        get => EditorPrefs.GetBool("EditorLaunch-launchOverrideScreen", true);
        set => EditorPrefs.SetBool("EditorLaunch-launchOverrideScreen", value);
    }

    public static bool launchFullscreen
    {
        get => EditorPrefs.GetBool("EditorLaunch-launchFullscreen", false);
        set => EditorPrefs.SetBool("EditorLaunch-launchFullscreen", value);
    }

    public static int launchScreenWidth
    {
        get => EditorPrefs.GetInt("EditorLaunch-launchScreenWidth", 1280);
        set => EditorPrefs.SetInt("EditorLaunch-launchScreenWidth", value);
    }

    public static int launchScreenHeight
    {
        get => EditorPrefs.GetInt("EditorLaunch-launchScreenHeight", 720);
        set => EditorPrefs.SetInt("EditorLaunch-launchScreenHeight", value);
    }
}
#endif
