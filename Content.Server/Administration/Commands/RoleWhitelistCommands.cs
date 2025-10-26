// SPDX-FileCopyrightText: 2024 Brandon Hu <103440971+Brandon-Huu@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 DrSmugleaf <10968691+DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Players.RoleWhitelist;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.Administration.Commands;

public abstract class BaseRoleWhitelistCommand : LocalizedCommands
{
    [Dependency] protected readonly IServerDbManager Db = default!;
    [Dependency] protected readonly IPlayerLocator PlayerLocator = default!;
    [Dependency] protected readonly IPlayerManager Players = default!;

    protected const string PlayerNotFoundError = "cmd-rolewhitelist-player-doesnt-exist-error";

    protected async Task<LocatedPlayerData?> FindPlayerAsync(string playerName)
    {
        return await PlayerLocator.LookupIdByNameAsync(playerName.Trim());
    }

    protected NetUserId GetAdminUserId(IConsoleShell shell)
    {
        return shell.Player?.UserId ?? new NetUserId(Guid.Empty);
    }

    protected CompletionResult GetPlayerCompletion(string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                Players.Sessions.Select(s => s.Name),
                Loc.GetString("cmd-jobwhitelist-hint-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.RoleWhitelist)]
public sealed class JobWhitelistAddCommand : BaseRoleWhitelistCommand
{
    [Dependency] private readonly RoleWhitelistManager _roleWhitelist = default!;

    public override string Command => "rw:add";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("cmd-need-at-least-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var playerName = args[0].Trim();
        var playerData = await FindPlayerAsync(playerName);

        if (playerData == null)
        {
            shell.WriteError(Loc.GetString(PlayerNotFoundError));
            return;
        }

        var adminData = await PlayerLocator.LookupIdAsync(GetAdminUserId(shell));
        var playerGuid = playerData.UserId;
        var adminGuid = adminData?.UserId ?? Guid.Empty;

        if (await Db.IsPlayerRoleWhitelisted(playerGuid))
        {
            shell.WriteLine(Loc.GetString("cmd-rolewhitelistadd-already-whitelisted",
                ("player", playerName)));
            return;
        }

        _roleWhitelist.AddWhitelist(playerGuid, adminGuid);
        shell.WriteLine(Loc.GetString("cmd-rolewhitelistadd-added",
            ("player", playerName)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return GetPlayerCompletion(args);
    }
}

[AdminCommand(AdminFlags.RoleWhitelist)]
public sealed class RemoveJobWhitelistCommand : BaseRoleWhitelistCommand
{
    [Dependency] private readonly RoleWhitelistManager _roleWhitelist = default!;

    public override string Command => "rw:remove";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("cmd-need-at-least-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var playerName = args[0].Trim();
        var playerData = await FindPlayerAsync(playerName);

        if (playerData == null)
        {
            shell.WriteError(Loc.GetString(PlayerNotFoundError));
            return;
        }

        var adminData = await PlayerLocator.LookupIdAsync(GetAdminUserId(shell));
        var playerGuid = playerData.UserId;
        var adminGuid = adminData?.UserId ?? Guid.Empty;

        if (!await Db.IsPlayerRoleWhitelisted(playerGuid))
        {
            shell.WriteLine(Loc.GetString("cmd-rolewhitelistget-not-whitelisted",
                ("player", playerName)));
            return;
        }

        _roleWhitelist.RemoveWhitelist(playerGuid, adminGuid);
        shell.WriteLine(Loc.GetString("cmd-rolewhitelistremove-removed",
            ("player", playerName)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return GetPlayerCompletion(args);
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class RoleWhitelistListCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IWhitelistDataService _whitelistService = default!;

    public override string Command => "rw:list";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        try
        {
            var whitelists = await _db.GetAllRoleWhitelists();
            var activeWhitelists = whitelists.Where(wl => wl.InWhitelist).ToList();

            if (!activeWhitelists.Any())
            {
                shell.WriteLine(Loc.GetString("cmd-rolewhitelistgetall-empty-list-error"));
                return;
            }

            var playerCache = await _whitelistService.BuildPlayerCacheAsync(activeWhitelists);
            var outputBuilder = new StringBuilder();

            for (int i = 0; i < activeWhitelists.Count; i++)
            {
                var whitelist = activeWhitelists[i];
                if (playerCache.TryGetValue(whitelist.PlayerId, out var playerData))
                {
                    outputBuilder.Append(_whitelistService.FormatWhitelistInfo(whitelist, playerData, playerCache, showAll: false, position: i + 1));
                }
            }

            shell.WriteLine(Loc.GetString("cmd-rolewhitelistgetall-active-count", ("count", activeWhitelists.Count)));
            shell.WriteLine(outputBuilder.ToString());
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-rolewhitelistgetall-error", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class GetAllRoleWhitelistCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IWhitelistDataService _whitelistService = default!;

    public override string Command => "rw:all";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        try
        {
            var whitelists = await _db.GetAllRoleWhitelists();
            var allWhitelists = whitelists.ToList();

            if (!allWhitelists.Any())
            {
                shell.WriteLine(Loc.GetString("cmd-rolewhitelistgetall-no-records"));
                return;
            }

            var playerCache = await _whitelistService.BuildPlayerCacheAsync(allWhitelists);
            var outputBuilder = new StringBuilder();

            for (int i = 0; i < allWhitelists.Count; i++)
            {
                var whitelist = allWhitelists[i];
                if (playerCache.TryGetValue(whitelist.PlayerId, out var playerData))
                {
                    outputBuilder.Append(_whitelistService.FormatWhitelistInfo(whitelist, playerData, playerCache, showAll: true, position: i + 1));
                }
            }

            var activeCount = allWhitelists.Count(wl => wl.InWhitelist);
            var totalCount = allWhitelists.Count;

            shell.WriteLine(Loc.GetString("cmd-rolewhitelistgetall-all-count",
                ("active", activeCount),
                ("total", totalCount)));
            shell.WriteLine(outputBuilder.ToString());
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-rolewhitelistgetall-error", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class GetRoleWhitelistCommand : BaseRoleWhitelistCommand
{
    [Dependency] private readonly IWhitelistDataService _whitelistService = default!;

    public override string Command => "rw:check";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("cmd-need-at-least-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var playerName = args[0].Trim();
        var playerData = await FindPlayerAsync(playerName);

        if (playerData == null)
        {
            shell.WriteLine(Loc.GetString("cmd-rolewhitelistget-not-whitelisted", ("player", playerName)));
            return;
        }

        var whitelists = await Db.GetAllRoleWhitelists();
        var playerWhitelist = whitelists.FirstOrDefault(wl => wl.PlayerId == playerData.UserId);

        if (playerWhitelist == null)
        {
            shell.WriteLine(Loc.GetString("cmd-rolewhitelistget-never-whitelisted", ("player", playerName)));
            return;
        }

        var playerCache = await _whitelistService.BuildPlayerCacheAsync(new[] { playerWhitelist });
        var output = _whitelistService.FormatWhitelistInfo(playerWhitelist, playerData, playerCache, showAll: true);
        shell.WriteLine(output);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return GetPlayerCompletion(args);
    }
}

public interface IWhitelistDataService
{
    Task<Dictionary<Guid, LocatedPlayerData>> BuildPlayerCacheAsync(IEnumerable<RoleWhitelist> whitelists);
    string FormatWhitelistInfo(RoleWhitelist whitelist, LocatedPlayerData playerData, Dictionary<Guid, LocatedPlayerData> playerCache, bool showAll, int? position = null);
}

public sealed class WhitelistDataService : IWhitelistDataService
{
    private readonly IPlayerLocator _playerLocator;
    private readonly TimeZoneInfo _moscowTime;

    public WhitelistDataService(IPlayerLocator playerLocator)
    {
        _playerLocator = playerLocator;
        _moscowTime = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
    }

    public async Task<Dictionary<Guid, LocatedPlayerData>> BuildPlayerCacheAsync(IEnumerable<RoleWhitelist> whitelists)
    {
        var cache = new Dictionary<Guid, LocatedPlayerData>();
        var uniquePlayerIds = CollectUniquePlayerIds(whitelists);

        var lookupTasks = uniquePlayerIds.Select(async playerId =>
        {
            var playerData = await _playerLocator.LookupIdAsync(new NetUserId(playerId));
            return (playerId, playerData);
        });

        var results = await Task.WhenAll(lookupTasks);

        foreach (var (playerId, playerData) in results)
        {
            if (playerData != null)
            {
                cache[playerId] = playerData;
            }
        }

        return cache;
    }

    public string FormatWhitelistInfo(RoleWhitelist whitelist, LocatedPlayerData playerData, Dictionary<Guid, LocatedPlayerData> playerCache, bool showAll, int? position = null)
    {
        var entryBuilder = new StringBuilder();

        if (position.HasValue)
        {
            if (showAll)
            {
                var status = whitelist.InWhitelist
                    ? Loc.GetString("whitelist-status-active")
                    : Loc.GetString("whitelist-status-inactive");
                entryBuilder.Append($"{status} ");
            }
            entryBuilder.AppendLine(Loc.GetString("cmd-rolewhitelistgetall-entry-one",
                ("pos", position.Value),
                ("usr", playerData.Username)));
        }
        else
        {
            entryBuilder.AppendLine(Loc.GetString("whitelist-player-info", ("player", playerData.Username)));
            var status = whitelist.InWhitelist
                ? Loc.GetString("whitelist-status-active-value")
                : Loc.GetString("whitelist-status-inactive-value");
            entryBuilder.AppendLine(Loc.GetString("whitelist-status-current", ("status", status)));
            entryBuilder.AppendLine();
        }

        if (!position.HasValue)
        {
            entryBuilder.AppendLine(Loc.GetString("whitelist-admins-title"));
        }

        entryBuilder.AppendLine(Loc.GetString("whitelist-admins-first-added",
            ("admin", GetAdminNameFromCache(whitelist.FirstTimeAddedBy, playerCache))));

        entryBuilder.AppendLine(Loc.GetString("whitelist-admins-last-added",
            ("admin", GetAdminNameFromCache(whitelist.LastTimeAddedBy, playerCache))));

        entryBuilder.AppendLine(Loc.GetString("whitelist-admins-last-removed",
            ("admin", GetAdminNameFromCache(whitelist.LastTimeRemovedBy, playerCache))));

        if (!position.HasValue)
        {
            entryBuilder.AppendLine();
            entryBuilder.AppendLine(Loc.GetString("whitelist-stats-title"));
        }

        entryBuilder.AppendLine(Loc.GetString("whitelist-stats-add-count",
            ("count", whitelist.HowManyTimesAdded)));

        entryBuilder.AppendLine(Loc.GetString("whitelist-stats-first-added",
            ("date", ConvertToMoscowTime(whitelist.FirstTimeAdded))));

        entryBuilder.AppendLine(Loc.GetString("whitelist-stats-last-added",
            ("date", ConvertToMoscowTime(whitelist.LastTimeAdded))));

        if (whitelist.LastTimeRemoved.HasValue)
        {
            entryBuilder.AppendLine(Loc.GetString("whitelist-stats-last-removed",
                ("date", ConvertToMoscowTime(whitelist.LastTimeRemoved.Value))));
        }
        else
        {
            entryBuilder.AppendLine(Loc.GetString("whitelist-stats-never-removed"));
        }

        entryBuilder.AppendLine();
        return entryBuilder.ToString();
    }

    private static HashSet<Guid> CollectUniquePlayerIds(IEnumerable<RoleWhitelist> whitelists)
    {
        var uniquePlayerIds = new HashSet<Guid>();

        foreach (var whitelist in whitelists)
        {
            uniquePlayerIds.Add(whitelist.PlayerId);
            AddAdminIdIfValid(uniquePlayerIds, whitelist.FirstTimeAddedBy);
            AddAdminIdIfValid(uniquePlayerIds, whitelist.LastTimeAddedBy);
            AddAdminIdIfValid(uniquePlayerIds, whitelist.LastTimeRemovedBy);
        }

        return uniquePlayerIds;
    }

    private static void AddAdminIdIfValid(HashSet<Guid> set, Guid? adminId)
    {
        if (adminId.HasValue && adminId.Value != Guid.Empty)
        {
            set.Add(adminId.Value);
        }
    }

    private string GetAdminNameFromCache(Guid? adminId, Dictionary<Guid, LocatedPlayerData> playerCache)
    {
        if (adminId == null || adminId.Value == Guid.Empty)
            return "Неизвестно";

        return playerCache.TryGetValue(adminId.Value, out var adminData)
            ? adminData.Username
            : "Неизвестно";
    }

    private string ConvertToMoscowTime(DateTime dateTime)
    {
        return TimeZoneInfo.ConvertTime(dateTime, _moscowTime).ToString("yyyy-M-d H:mm:ss");
    }
}
