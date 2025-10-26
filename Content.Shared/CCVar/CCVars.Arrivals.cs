// SPDX-FileCopyrightText: 2024 Simon <63975668+Simyon264@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 noirogen <raethertechnologies@gmail.com>
// SPDX-FileCopyrightText: 2025 slarticodefast <161409025+slarticodefast@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<int> ArrivalsMinHours =
        CVarDef.Create("transithub.arrivals_min_hours", 0, CVar.SERVER | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ArrivalsRoundStartSpawn =
        CVarDef.Create("transithub.arrivals_round_start_spawn", false, CVar.SERVER | CVar.ARCHIVE);


}
