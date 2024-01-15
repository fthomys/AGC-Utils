﻿using AGC_Management.Entities;
using AGC_Management.Services;

namespace AGC_Management.Utils;

public static class LevelUtils
{
    private static ulong levelguildid = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"]);
    /// <summary>
    /// Calculates the experience points required to reach a given level.
    /// </summary>
    /// <param name="lvl">The level to calculate the experience points for.</param>
    /// <returns>The experience points required to reach the given level.</returns>
    public static int XpForLevel(int lvl)
    {
        if (lvl <= 0)
        {
            return 0;
        }
        int alvl = lvl - 1;
        return (int)(5 / 6.0 * (151 * alvl + 33 * Math.Pow(alvl, 2) + 2 * Math.Pow(alvl, 3)) + 100);
    }
    
    public static int XpForNextLevel(int lvl)
    {
        lvl += 1;
        int alvl = lvl;
        return (int)(5 / 6.0 * (151 * alvl + 33 * Math.Pow(alvl, 2) + 2 * Math.Pow(alvl, 3)) + 100);
    }
    
    public static int XpToFinishLevel(int xp)
    {
        int level = LevelAtXp(xp);
        int xpForNextLevel = XpForLevel(level + 1);
        return xpForNextLevel - xp;
    }
    
    
    
    /// <summary>
    /// Calculates the minimum and maximum experience points (XP) required for a given level.
    /// </summary>
    /// <param name="lvl">The level.</param>
    /// <returns>A dictionary containing the minimum and maximum XP values.</returns>
    public static Dictionary<int, int> MinAndMaxXpForThisLevel(int lvl)
    {
        int min = 0;
        int max = 0;
        if (lvl <= 0)
        {
            if (lvl == 0)
            {
                return new Dictionary<int, int> { { 0, 100 } };
            }
            return new Dictionary<int, int> { { min, max } };
        }
        int alvl = lvl - 1;
        min = (int)(5 / 6.0 * (151 * alvl + 33 * Math.Pow(alvl, 2) + 2 * Math.Pow(alvl, 3)) + 100);
        max = (int)(5 / 6.0 * (151 * lvl + 33 * Math.Pow(lvl, 2) + 2 * Math.Pow(lvl, 3)) + 100);
        return new Dictionary<int, int> { { min, max } };
    }
    


    /// <summary>
    /// Calculates the level based on the total experience points.
    /// </summary>
    /// <param name="totalXp">The total experience points.</param>
    /// <returns>The level corresponding to the total experience points.</returns>
    public static int LevelAtXp(int totalXp)
    {
        int level = 0;
        int xpForNextLevel = 100;
        
        while (totalXp >= xpForNextLevel)
        {
            totalXp -= xpForNextLevel;
            level++;
            xpForNextLevel = 5 * (level * level) + (50 * level) + 100;
        }

        return level;
    }

    /// <summary>
    /// Calculates the amount of experience points needed to reach the next level.
    /// </summary>
    /// <param name="xp">The current amount of experience points.</param>
    /// <returns>The amount of experience points needed to reach the next level.</returns>
    public static int XpUntilNextLevel(int xp)
    {
        int currentLevel = LevelAtXp(xp);
        int xpForNextLevel = XpForLevel(currentLevel + 1);
        return xpForNextLevel - xp;
    }

    /// <summary>
    /// Method to retrieve the ranking of a user based on their XP in the leveling data table. </summary>
    /// <param name="userid">The unique identifier of the user.</param>
    /// <returns>The rank of the user. Returns 0 if the user is not found.</returns>
    public static async Task<int> GetUserRankAsync(ulong userid)
    {
        int rank = 0;
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT userid, current_xp FROM levelingdata ORDER BY current_xp DESC", db);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                rank++;
                if (reader.GetInt64(0) == (long)userid)
                {
                    break;
                }
            }
        }

        await db.CloseAsync();
        return rank;
    }

    /// <summary>
    /// Recalculates the user level based on their experience points (xp) and updates the database.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    public static async Task RecalculateUserLevel(ulong userId)
    {
        var rank = await GetRank(userId);
        var xp = rank[userId].Xp;
        var level = LevelAtXp(xp);
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("UPDATE levelingdata SET current_level = @level WHERE userid = @id", db);
        cmd.Parameters.AddWithValue("@level", level);
        cmd.Parameters.AddWithValue("@id", (long)userId);
        await cmd.ExecuteNonQueryAsync();
        await db.CloseAsync();
    }
    
    public static async Task RecalculateAllUserLevels()
    {
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT userid, current_xp FROM levelingdata", db);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            CurrentApplication.Logger.Information("Recalculating all user levels...");
            while (await reader.ReadAsync())
            {
                var userId = reader.GetInt64(0);
                var xp = reader.GetInt32(1);
                var level = LevelAtXp(xp);
                await using var db2 = new NpgsqlConnection(DatabaseService.GetConnectionString());
                await db2.OpenAsync();
                await using var cmd2 = new NpgsqlCommand("UPDATE levelingdata SET current_level = @level WHERE userid = @id", db2);
                cmd2.Parameters.AddWithValue("@level", level);
                cmd2.Parameters.AddWithValue("@id", userId);
                await cmd2.ExecuteNonQueryAsync();
                await db2.CloseAsync();
            }
            // set timestamp for last recalculation unix timestamp
            await using var db3 = new NpgsqlConnection(DatabaseService.GetConnectionString());
            await db3.OpenAsync();
            await using var cmd3 = new NpgsqlCommand("UPDATE levelingsettings SET lastrecalc = @timestamp", db3);
            cmd3.Parameters.AddWithValue("@timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await cmd3.ExecuteNonQueryAsync();
            await db3.CloseAsync();
            CurrentApplication.Logger.Information("Recalculated all user levels.");
        }
        await db.CloseAsync();
    }

    /// <summary>
    /// Retrieves the rank data for a specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A dictionary containing the rank data, where the key is the user ID and the value is the RankData object.</returns>
    public static async Task<Dictionary<ulong, RankData>> GetRank(ulong userId)
    {
        var rank = new Dictionary<ulong, RankData>();
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT current_xp, current_level FROM levelingdata WHERE userid = @id", db);
        cmd.Parameters.AddWithValue("@id", (long)userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var xp = reader.GetInt32(0);
                var level = reader.GetInt32(1);
                rank[userId] = new RankData { Level = level, Xp = xp };
            }
        }
        else
        {
            rank[userId] = new RankData { Level = 0, Xp = 0 };
        }

        await reader.CloseAsync();
        await db.CloseAsync();
        return rank;
    }

    public static async Task<int> GetUserLevel(ulong userId)
    {
        var rank = await GetRank(userId);
        return rank[userId].Level;
    }
    
    
    // leaderboard data for the leaderboard command
    public static async Task<List<LeaderboardData>> FetchLeaderboardData()
    {
        var leaderboardData = new List<LeaderboardData>();
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        
        var cmd = new NpgsqlCommand("SELECT userid, current_xp, current_level FROM levelingdata ORDER BY current_xp DESC", db);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var userId = reader.GetInt64(0);
                var xp = reader.GetInt32(1);
                var level = reader.GetInt32(2);
                leaderboardData.Add(new LeaderboardData { UserId = (ulong)userId, XP = xp, Level = level });
            }
        }
        else
        {
            leaderboardData.Add(new LeaderboardData { UserId = 0, XP = 0, Level = 0 });
        }
        
        await reader.CloseAsync();
        await db.CloseAsync();
        return leaderboardData;
    }

    public static int GetUserRank(ulong invokingUserId, List<LeaderboardData> leaderboardData)
    {
        for (int i = 0; i < leaderboardData.Count; i++)
        {
            if (leaderboardData[i].UserId == invokingUserId)
            {
                return i + 1;
            }
        }
        
        return -1;
    }
    
    public static int GetBaseXp(XpRewardType type)
    {
        var rng = new Random();
        switch (type)
        {
            case XpRewardType.Message:
                return rng.Next(15, 25);
            case XpRewardType.Voice:
                return rng.Next(3, 5);
            default:
                return 0;
        }
    }

    public static async Task<bool> isVcLevelingEnabled()
    {
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT vc_active FROM levelingsettings WHERE guildid = @guildid", db);
        cmd.Parameters.AddWithValue("@guildid", (long)levelguildid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var vcLevelingEnabled = reader.GetBoolean(0);
                return vcLevelingEnabled;
            }
        }
        else
        {
            return false;
        }
        await db.CloseAsync();
        return false;
    }
    
    public static async Task<bool> isMessageLevelingEnabled()
    {
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT text_active FROM levelingsettings WHERE guildid = @guildid", db);
        cmd.Parameters.AddWithValue("@guildid", (long)levelguildid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var messageLevelingEnabled = reader.GetBoolean(0);
                return messageLevelingEnabled;
            }
        }
        else
        {
            return false;
        }
        await db.CloseAsync();
        return false;
    }
    
    public static async Task<float> GetVcXpMultiplier()
    {
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT vc_multi FROM levelingsettings WHERE guildid = @guildid", db);
        cmd.Parameters.AddWithValue("@guildid", (long)levelguildid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var vcXpMultiplier = reader.GetFloat(0);
                return vcXpMultiplier;
            }
        }
        else
        {
            return 1;
        }
        await db.CloseAsync();
        return 1;
    } 
    
    public static async Task<float> GetMessageXpMultiplier()
    {
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT text_multi FROM levelingsettings WHERE guildid = @guildid", db);
        cmd.Parameters.AddWithValue("@guildid", (long)levelguildid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var messageXpMultiplier = reader.GetFloat(0);
                return messageXpMultiplier;
            }
        }
        else
        {
            return 1;
        }
        await db.CloseAsync();
        return 1;
    }
    
    public static async Task<string> GetLevelUpMessage()
    {
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT levelupmessage FROM levelingsettings WHERE guildid = @guildid", db);
        cmd.Parameters.AddWithValue("@guildid", (long)levelguildid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var levelUpMessage = reader.GetString(0);
                return levelUpMessage;
            }
        }
        else
        {
            return "Congratulations {user}! You just advanced to level {level}!";
        }
        await db.CloseAsync();
        return "Congratulations {user}! You just advanced to level {level}!";
    }
    
    public static async Task<string> GetLevelUpRewardMessage()
    {
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT levelupmessagereward FROM levelingsettings WHERE guildid = @guildid", db);
        cmd.Parameters.AddWithValue("@guildid", (long)levelguildid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var levelUpRewardMessage = reader.GetString(0);
                return levelUpRewardMessage;
            }
        }
        else
        {
            return "Congratulations {username}! You just advanced to level {level} and received {rolename}!";
        }
        await db.CloseAsync();
        return "Congratulations {username}! You just advanced to level {level} and received {rolename}!";
    }
    
    public static async Task<ulong> GetLevelUpChannelId()
    {
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT levelupchannelid FROM levelingsettings WHERE guildid = @guildid", db);
        cmd.Parameters.AddWithValue("@guildid", (long)levelguildid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var levelUpChannelId = reader.GetInt64(0);
                return (ulong)levelUpChannelId;
            }
        }
        else
        {
            return 0;
        }
        await db.CloseAsync();
        return 0;
    }
    
    public static async Task<List<Reward>> GetLevelRewards()
    {
        var rewards = new List<Reward>();
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        
        var cmd = new NpgsqlCommand("SELECT level, roleid FROM level_rewards ORDER BY level ASC", db);
        cmd.Parameters.AddWithValue("@guildid", (long)levelguildid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var level = reader.GetInt32(0);
                var roleId = reader.GetInt64(1);
                rewards.Add(new Reward { Level = level, RoleId = (ulong)roleId });
            }
        }
        await reader.CloseAsync();
        await db.CloseAsync();
        return rewards;
    }
    
    public static async Task<List<MultiplicatorOverrides>> GetMultiplicatorOverrides()
    {
        var overrides = new List<MultiplicatorOverrides>();
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        
        var cmd = new NpgsqlCommand("SELECT roleid, multiplicator FROM level_multiplicatoroverrideroles", db);
        cmd.Parameters.AddWithValue("@guildid", (long)levelguildid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var roleId = reader.GetInt64(0);
                var multiplier = reader.GetFloat(1);
                overrides.Add(new MultiplicatorOverrides { RoleId = (ulong)roleId , Multiplicator = multiplier });
            }
        }
        await reader.CloseAsync();
        await db.CloseAsync();
        return overrides;
    }
    
    public static async Task<bool> IsLevelingActive(XpRewardType type)
    {
        if (type == XpRewardType.Message)
        {
            return await isMessageLevelingEnabled();
        }
        else if (type == XpRewardType.Voice)
        {
            return await isVcLevelingEnabled();
        }
        return false;
    }
    
    
    public static async Task GiveXP(DiscordUser user, float xp, XpRewardType type)
    {
        _ = Task.Run(async () =>
        { 
            if (!await IsLevelingActive(type))
            {
                return;
            }
            await AddUserToDbIfNot(user);
            var typeString = type == XpRewardType.Message ? "last_text_reward" : "last_vc_reward";
            await using var cooldowndb = new NpgsqlConnection(DatabaseService.GetConnectionString());
            await cooldowndb.OpenAsync();
            await using var cooldowncmd = new NpgsqlCommand("SELECT " + typeString + " FROM levelingdata WHERE userid = @id", cooldowndb);
            cooldowncmd.Parameters.AddWithValue("@id", (long)user.Id);
            await using var cooldownreader = await cooldowncmd.ExecuteReaderAsync();
            // cooldown is 60 seconds
            if (cooldownreader.HasRows)
            {
                while (await cooldownreader.ReadAsync())
                {
                    var lastReward = cooldownreader.GetInt64(0);
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (now - lastReward < 60)
                    {
                        Console.WriteLine("Cooldown not over yet.");
                        await cooldownreader.CloseAsync();
                        await cooldowndb.CloseAsync();
                        return;
                    }
                }
            }
            await cooldownreader.CloseAsync();
            await cooldowndb.CloseAsync();
            
            var rank = await GetRank(user.Id);
            var currentXp = rank[user.Id].Xp;
            var currentLevel = rank[user.Id].Level;
            var xpMultiplier = 1f;
            if (type == XpRewardType.Message)
            {
                xpMultiplier = await GetMessageXpMultiplier();
            }
            else if (type == XpRewardType.Voice)
            {
                xpMultiplier = await GetVcXpMultiplier();
            }
            var xpToGive = (int)(xpMultiplier * xp);
            var multiplicatorOverrides = await GetMultiplicatorOverrides();
            var member = await user.ConvertToMember(CurrentApplication.TargetGuild);
            foreach (var multiplicatorOverride in multiplicatorOverrides)
            {
                if (member.Roles.Any(role => role.Id == multiplicatorOverride.RoleId))
                {
                    xpToGive = (int)(xpToGive * multiplicatorOverride.Multiplicator);
                }
            }
            var newXp = currentXp + xpToGive;
            var newLevel = LevelAtXp(newXp);
            var current_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var rewardTypeString = type == XpRewardType.Message ? "last_text_reward" : "last_vc_reward";
            await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
            await db.OpenAsync();
            await using var cmd = new NpgsqlCommand("UPDATE levelingdata SET current_xp = @xp, current_level = @level, " + rewardTypeString + " = @timestamp WHERE userid = @id", db);
            cmd.Parameters.AddWithValue("@xp", newXp);
            cmd.Parameters.AddWithValue("@level", newLevel);
            cmd.Parameters.AddWithValue("@id", (long)user.Id);
            cmd.Parameters.AddWithValue("@timestamp", current_timestamp);
            await cmd.ExecuteNonQueryAsync();
            await db.CloseAsync();
            Console.WriteLine("Gave " + xpToGive + " xp to " + user.Username);
            if (newLevel > currentLevel)
            {
                await SendLevelUpMessageAndReward(user, newLevel);
            }
        });
        await Task.CompletedTask;
    }

    public static async Task AddUserToDbIfNot(DiscordUser user)
    {
        // check if user is in the database
        await using var checkdb = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await checkdb.OpenAsync();
        await using var checkcmd = new NpgsqlCommand("SELECT userid FROM levelingdata WHERE userid = @id", checkdb);
        checkcmd.Parameters.AddWithValue("@id", (long)user.Id);
        await using var checkreader = await checkcmd.ExecuteReaderAsync();

        if (!checkreader.HasRows)
        {
            await using var __db = new NpgsqlConnection(DatabaseService.GetConnectionString());
            await __db.OpenAsync();
            await using var __cmd = new NpgsqlCommand("INSERT INTO levelingdata (userid, current_xp, current_level) VALUES (@id, @xp, @level)", __db);
            __cmd.Parameters.AddWithValue("@id", (long)user.Id);
            __cmd.Parameters.AddWithValue("@xp", 0);
            __cmd.Parameters.AddWithValue("@level", 0);
            await __cmd.ExecuteNonQueryAsync();
            await __db.CloseAsync();
            Console.WriteLine("Added user to database.");
        }
        await checkreader.CloseAsync();
        await checkdb.CloseAsync();
    }

    private static async Task SendLevelUpMessageAndReward(DiscordUser user, int level)
    {
        Console.WriteLine("Sending level up message and reward...");
        var levelUpMessage = await GetLevelUpMessage();
        var isReward = await IsLevelRewarded(level);
        var rewardMessage = await GetLevelUpRewardMessage();
        var reward = await GetRewardForaLevel(level);
        Console.WriteLine("Is reward: " + isReward);
        if (isReward)
        {
            var member = await user.ConvertToMember(CurrentApplication.TargetGuild);
            var role = CurrentApplication.TargetGuild.GetRole(reward[true]);
            try
            {
                await member.GrantRoleAsync(role);
                var formattedMessage = await MessageFormatter.FormatLevelUpMessage(rewardMessage, true, user, role);
                var channel = CurrentApplication.TargetGuild.GetChannel(await GetLevelUpChannelId());
                await channel.SendMessageAsync(formattedMessage);
            }
            catch (Exception e)
            {
                CurrentApplication.Logger.Error(e.Message);
            }

        }
        else
        {
            var formattedMessage = await MessageFormatter.FormatLevelUpMessage(levelUpMessage, false, user);
            var channel = CurrentApplication.TargetGuild.GetChannel(await GetLevelUpChannelId());
            await channel.SendMessageAsync(formattedMessage);
        }
        
    }
    
    private static async Task<bool> IsLevelRewarded(int level)
    {
        var rewards = await GetLevelRewards();
        var reward = rewards.FirstOrDefault(r => r.Level == level);
        if (reward != null)
        {
            return true;
        }
        return false;
    }
    
    private static async Task<Dictionary<bool, ulong>> GetRewardForaLevel(int level)
    {
        var rewards = await GetLevelRewards();
        var reward = rewards.FirstOrDefault(r => r.Level == level);
        if (reward != null)
        {
            return new Dictionary<bool, ulong> { { true, reward.RoleId } };
        }
        return new Dictionary<bool, ulong> { { false, 0 } };
    }
    
    
    // restore all roles for a user that match the level or below
    public static async Task RestoreRoles(DiscordMember user)
    {
        var rank = await GetRank(user.Id);
        var level = rank[user.Id].Level;
        var rewards = await GetLevelRewards();
        var rolesToRestore = rewards.Where(r => r.Level <= level).ToList();
        foreach (var roleToRestore in rolesToRestore)
        {
            var role = CurrentApplication.TargetGuild.GetRole(roleToRestore.RoleId);
            try
            {
                await user.GrantRoleAsync(role);
            }
            catch (Exception e)
            {
                CurrentApplication.Logger.Error(e.Message);
            }
        }
    }
    
    
}