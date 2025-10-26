// SPDX-FileCopyrightText: 2024 DrSmugleaf <10968691+DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Players.JobWhitelist;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Serilog;

namespace Content.Server.Players.RoleWhitelist;

public sealed class RoleWhitelistManager : IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<NetUserId, Database.RoleWhitelist?> _whitelists = new();

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgRoleWhitelist>();
    }

    private async Task LoadData(ICommonSession session, CancellationToken cancel)
    {
        var whitelist = await _db.GetRoleWhitelist(session.UserId, cancel);
        cancel.ThrowIfCancellationRequested();
        _whitelists[session.UserId] = whitelist;
    }

    private void FinishLoad(ICommonSession session)
    {
        SendRoleWhitelist(session);
    }

    private void ClientDisconnected(ICommonSession session)
    {
        _whitelists.Remove(session.UserId);
    }

    public async void AddWhitelist(Guid player, Guid admin)
    {
        await _db.AddToRoleWhitelist(player, admin);
        await _db.AddRoleWhitelistLog(player, admin, "Added to the role whitelist.");
    }

    public bool IsAllowed(ICommonSession session, ProtoId<JobPrototype> job)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        if (!_prototypes.TryIndex(job, out var jobPrototype) ||
            !jobPrototype.Whitelisted)
            return true;

        return IsWhitelisted(session.UserId.UserId);
    }

    public bool IsWhitelisted(Guid player)
    {
        if (!_whitelists.TryGetValue(new NetUserId(player), out var whitelist))
        {
            Log.Error("Unable to check if player {Player} is whitelisted for roles. Stack trace:\\n{StackTrace}",
                player,
                Environment.StackTrace);
            return false;
        }

        return whitelist?.InWhitelist ?? false;
    }

    public async void RemoveWhitelist(Guid player, Guid admin)
    {
        await _db.RemoveFromRoleWhitelist(player, admin);
        await _db.AddRoleWhitelistLog(player, admin, "Removed from the role whitelist.");

        if (_player.TryGetSessionById(new NetUserId(player), out var session))
            SendRoleWhitelist(session);
    }

    public void SendRoleWhitelist(ICommonSession player)
    {
        var msg = new MsgRoleWhitelist
        {
            Whitelist = _whitelists.TryGetValue(player.UserId, out var roleWhitelist) && roleWhitelist != null && roleWhitelist.InWhitelist
        };

        _net.ServerSendMessage(msg, player.Channel);
    }

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
