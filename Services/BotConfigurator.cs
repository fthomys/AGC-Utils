﻿#region

using AGC_Management.Utils;
using IniParser;
using IniParser.Model;

#endregion

namespace AGC_Management;

public static class BotConfig
{
    public static IniData GetConfig()
    {
        IniData ConfigIni;
        FileIniDataParser parser = new();
        try
        {
            ConfigIni = parser.ReadFile("config.ini");
        }
        catch
        {
            Console.WriteLine("Die Konfigurationsdatei konnte nicht geladen werden. Bitte überprüfe die config.");
            Console.WriteLine("Drücke eine beliebige Taste um das Programm zu beenden.");
            Console.WriteLine(Directory.GetCurrentDirectory());
            throw new ApplicationException();
            return null;
        }

        return ConfigIni;
    }


    public static void SetConfig(string key, string value, string data)
    {
        IniData ConfigIni;
        FileIniDataParser parser = new();
        ConfigIni = parser.ReadFile("config.ini");
        ConfigIni[key][value] = data;
        parser.WriteFile("config.ini", ConfigIni);
    }


    public static DiscordColor GetEmbedColor()
    {
        var fallbackColor = "000000";
        string colorString;

        try
        {
            var colorConfig = GetConfig()["EmbedConfig"]["DefaultEmbedColor"];
            if (colorConfig.StartsWith("#")) colorConfig = colorConfig.Remove(0, 1);

            if (string.IsNullOrEmpty(colorConfig) || !HexCheck.IsHexColor(colorConfig))
            {
                colorString = fallbackColor;
                return new DiscordColor(colorString);
            }

            colorString = colorConfig;
        }
        catch
        {
            colorString = fallbackColor;
        }

        return new DiscordColor(colorString);
    }
}