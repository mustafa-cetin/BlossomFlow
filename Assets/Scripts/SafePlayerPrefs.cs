using System;
using CrazyGames;
using UnityEngine;

public static class SafePlayerPrefs
{
    static bool UseSDK
    {
        get
        {
            if (CrazySDK.IsAvailable && !CrazySDK.IsInitialized)
                throw new InvalidOperationException("CrazySDK is available but not initialized.");
            return CrazySDK.IsAvailable;
        }
    }

    public static void SetInt(string key, int value)
    {
        if (UseSDK)
            CrazySDK.Data.SetInt(key, value);
        else
            PlayerPrefs.SetInt(key, value);
    }

    public static int GetInt(string key) => GetInt(key, 0);

    public static int GetInt(string key, int defaultValue)
    {
        return UseSDK ? CrazySDK.Data.GetInt(key, defaultValue) : PlayerPrefs.GetInt(key, defaultValue);
    }

    public static void SetFloat(string key, float value)
    {
        if (UseSDK)
            CrazySDK.Data.SetFloat(key, value);
        else
            PlayerPrefs.SetFloat(key, value);
    }

    public static float GetFloat(string key) => GetFloat(key, 0f);

    public static float GetFloat(string key, float defaultValue)
    {
        return UseSDK ? CrazySDK.Data.GetFloat(key, defaultValue) : PlayerPrefs.GetFloat(key, defaultValue);
    }

    public static void SetString(string key, string value)
    {
        if (UseSDK)
            CrazySDK.Data.SetString(key, value);
        else
            PlayerPrefs.SetString(key, value);
    }

    public static string GetString(string key) => GetString(key, "");

    public static string GetString(string key, string defaultValue)
    {
        return UseSDK ? CrazySDK.Data.GetString(key, defaultValue) : PlayerPrefs.GetString(key, defaultValue);
    }

    public static bool HasKey(string key)
    {
        return UseSDK ? CrazySDK.Data.HasKey(key) : PlayerPrefs.HasKey(key);
    }

    public static void DeleteKey(string key)
    {
        if (UseSDK)
            CrazySDK.Data.DeleteKey(key);
        else
            PlayerPrefs.DeleteKey(key);
    }

    public static void DeleteAll()
    {
        if (UseSDK)
            CrazySDK.Data.DeleteAll();
        else
            PlayerPrefs.DeleteAll();
    }

    public static void Save()
    {
        if (!UseSDK || Application.isEditor)
            PlayerPrefs.Save();
    }
}
