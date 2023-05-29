﻿using AGC_Management.Services.DatabaseHandler;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using System.Runtime.CompilerServices;

namespace AGC_Management.Helpers.TempVoice;

public class TempVoiceHelper : BaseCommandModule
{
    protected static string GetVCConfig(string key)
    {
        return BotConfig.GetConfig()["TempVC"][$"{key}"];
    }


    protected static async Task NoChannel(CommandContext ctx)
    {
        string errorMessage = $"<:attention:1085333468688433232> **Fehler!** " +
                              $"Du besitzt keinen eigenen Kanal oder der Kanal gehört dir nicht. " +
                              $"Wenn du keinen Kanal hast, kannst du einen unter <#{GetVCConfig("Creation_Channel_ID")}> erstellen.";

        await ctx.RespondAsync(errorMessage);
    }

    protected static async Task<bool> CheckTeam(CommandContext ctx, DiscordMember user)
    {
        DiscordRole staffRole = ctx.Guild.GetRole(ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["StaffRoleId"]));
        if (staffRole.Members.Any(x => x.Key == user.Id))
        {
            ctx.RespondAsync(
                $"<:attention:1085333468688433232> **Fehler!** Du kannst den Befehl ``{ctx.Command.Name}`` nicht auf Teammitglieder anwenden!");
            return true;
        }

        return false;
    }

    protected static ulong? GetUserChannel(CommandContext ctx)
    {
        ulong? userchannel;
        try
        {
            userchannel = ctx.Member.VoiceState?.Channel.Id;
        }
        catch
        {
            userchannel = null;
        }

        return userchannel;
    }

    protected static ulong? GetUserChannel(DiscordMember user)
    {
        ulong? userchannel;
        try
        {
            userchannel = user.VoiceState?.Channel.Id;
        }
        catch
        {
            userchannel = null;
        }

        return userchannel;
    }

    protected static DiscordChannel? GetUserChannelObj(CommandContext ctx)
    {
        DiscordChannel? channel;
        try
        {
            channel = ctx.Member.VoiceState?.Channel;
        }
        catch
        {
            channel = null;
        }

        return channel;
    }

    protected static DiscordChannel? GetUserChannelObj(DiscordMember user)
    {
        DiscordChannel? channel;
        try
        {
            channel = user.VoiceState?.Channel;
        }
        catch
        {
            channel = null;
        }

        return channel;
    }

    protected static async Task<List<ulong>> GetChannelIDFromDB(CommandContext ctx)
    {
        List<ulong> dbChannels = new List<ulong>();

        List<string> Query = new List<string>()
        {
            "channelid"
        };
        Dictionary<string, object> QueryConditions = new Dictionary<string, object>()
        {
            { "ownerid", ctx.User.Id }
        };
        List<Dictionary<string, object>> QueryResult = await DatabaseService.SelectDataFromTable("tempvoice", Query, QueryConditions);
        foreach (var result in QueryResult)
        {
            if (result.TryGetValue("channelid", out object channelIdObj) && channelIdObj is ulong channelId)
            {
                dbChannels.Add(channelId);
            }
        }
        return dbChannels;
    }

    protected static async Task<List<ulong>> GetChannelIDFromDB(DiscordMember member)
    {
        List<ulong> dbChannels = new List<ulong>();

        List<string> Query = new List<string>()
        {
            "channelid"
        };
        Dictionary<string, object> QueryConditions = new Dictionary<string, object>()
        {
            { "ownerid", member.Id }
        };
        List<Dictionary<string, object>> QueryResult = await DatabaseService.SelectDataFromTable("tempvoice", Query, QueryConditions);
        foreach (var result in QueryResult)
        {
            if (result.TryGetValue("channelid", out object channelIdObj) && channelIdObj is ulong channelId)
            {
                dbChannels.Add(channelId);
            }
        }
        return dbChannels;
    }

    protected static async Task<ulong?> GetChannelOwnerID(CommandContext ctx)
    {
        ulong? channelownerid = null;
        try
        {
            List<string> query = new List<string>()
            {
                "ownerid"
            };

            Dictionary<string, object> queryConditions = new Dictionary<string, object>()
            {
                { "channelid", ctx.Member.VoiceState?.Channel.Id }
            };

            List<Dictionary<string, object>> queryResult =
                await DatabaseService.SelectDataFromTable("tempvoice", query, queryConditions);

            foreach (var result in queryResult)
            {
                if (result.TryGetValue("ownerid", out object ownerIdValue) && ownerIdValue is ulong ownerId)
                {
                    channelownerid = ownerId;
                    break;
                }
            }
        }
        catch
        {
            // Handle exception
        }

        return channelownerid;
    }

    protected static async Task<ulong?> GetChannelOwnerID(DiscordMember user)
    {
        ulong? channelownerid = null;
        try
        {
            List<string> query = new List<string>()
            {
                "ownerid"
            };

            Dictionary<string, object> queryConditions = new Dictionary<string, object>()
            {
                { "channelid", user.VoiceState?.Channel.Id }
            };

            List<Dictionary<string, object>> queryResult =
                await DatabaseService.SelectDataFromTable("tempvoice", query, queryConditions);

            foreach (var result in queryResult)
            {
                if (result.TryGetValue("ownerid", out object ownerIdValue) && ownerIdValue is ulong ownerId)
                {
                    channelownerid = ownerId;
                    break; 
                }
            }
        }
        catch
        {
            // Handle exception
        }

        return channelownerid;
    }

    protected static async Task<ulong?> GetChannelOwnerID(DiscordChannel channel)
    {
        ulong? channelownerid = null;
        try
        {
            List<string> query = new List<string>()
            {
                "ownerid"
            };

            Dictionary<string, object> queryConditions = new Dictionary<string, object>()
            {
                { "channelid", channel.Id }
            };

            List<Dictionary<string, object>> queryResult =
                await DatabaseService.SelectDataFromTable("tempvoice", query, queryConditions);

            foreach (var result in queryResult)
            {
                if (result.TryGetValue("ownerid", out object ownerIdValue) && ownerIdValue is ulong ownerId)
                {
                    channelownerid = ownerId;
                    break; 
                }
            }
        }
        catch
        {
            // Handle exception
        }

        return channelownerid;
    }

    protected static bool SelfCheck(CommandContext ctx, DiscordMember member)
    {
        if (ctx.User.Id == member.Id)
        {
            return true;
        }
        return false;
    }

    protected static async Task<List<long>> GetAllTempChannels()
    {
        var list = new List<long>();
        List<string> query = new List<string>()
        {
            "channelid"
        };
        List<Dictionary<string, object>> result = await DatabaseService.SelectDataFromTable("tempvoice", query, null);
        foreach (var item in result)
        {
            long ChannelId = (long)item["channelid"];
            list.Add(ChannelId);
        }
        return list;

    }
    
}