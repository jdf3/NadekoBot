using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;

namespace NadekoBot.Modules.SSP
{
    internal class SSPModule : DiscordModule
    {
        private SSP _ssp;

        public SSPModule()
        {
            _ssp = new SSP();
        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.SSP;

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands(Prefix, cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(com => com.Init(cgb));

                cgb.CreateCommand("maxchannels")
                    .Alias("mc")
                    .Description($"Sets the maximum number of channels. Default is 6.\n**Usage**: `{Prefix} maxchannels <number of channels>`\n**Example**: `{Prefix} maxchannels 7`")
                    .Parameter("channels", ParameterType.Required)
                    .Do(async e =>
                    {
                        string channelsArg = e.GetArg("channels");
                        int channelMax;
                        if (!int.TryParse(channelsArg, out channelMax))
                        {
                            await e.Channel.SendMessage($"💢 {channelMax} isn't a number.");
                            return;
                        }
                        if (channelMax < 1 || channelMax > 15)
                        {
                            await e.Channel.SendMessage($"💢 {channelMax} must be between 1 and 15.");
                            return;
                        }
                        Message botMessage = await e.Channel.SendMessage($"Set channel max to {channelMax}.");
                        await Task.Delay(10000).ConfigureAwait(false);
                        await botMessage.Delete().ConfigureAwait(false);
                        if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                        {
                            await e.Message.Delete().ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand("unknowncap")
                   .Alias("uc")
                   .Description($"Sets the time that capture is assumed to take if a channel is not updated. Default is {SSP.DefaultUnknownCaptureLength.ToString(@"mm\:ss")}.**Usage**: `{Prefix} unknowncap <time>")
                   .Parameter("time", ParameterType.Required)
                   .Do(async e =>
                   {
                       string timeArg = e.GetArg("time");

                       TimeSpan timeRemaining;
                       if (!PoopUtilities.TryParseTimeSpan(timeArg, out timeRemaining))
                       {
                           await e.Channel.SendMessage($"💢 {timeArg} isn't a valid amount of time.");
                           return;
                       }

                       if (!SSP.SetUnknownCaptureLength(timeRemaining))
                       {
                           await e.Channel.SendMessage($"💢 {timeArg} isn't a valid amount of time for the Capture phase.");
                           return;
                       }
                       Message botMessage = await e.Channel.SendMessage($"Default capture time set to {SSP.UnknownCaptureLength.ToString(@"mm\:ss")}.");
                       await Task.Delay(10000).ConfigureAwait(false);
                       await botMessage.Delete().ConfigureAwait(false);
                       if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                       {
                           await e.Message.Delete().ConfigureAwait(false);
                       }
                   });

                cgb.CreateCommand("updatefaction")
                    .Alias("uf")
                    .Description(
                        $"Updates the faction that owns a channel. Only applies to mining phases and waiting to mine phases.\n**Usage**: `{Prefix} updatefaction <channel> <faction>`\n**Example**: `{Prefix} updatefaction 1 cerulean")
                    .Parameter("channel")
                    .Parameter("faction")
                    .Do(async e =>
                    {
                        string channelArg = e.GetArg("channel");
                        string factionArg = e.GetArg("faction");

                        int channelNumber;
                        if (!int.TryParse(channelArg, out channelNumber))
                        {
                            await e.Channel.SendMessage($"💢 {channelArg} isn't a number.");
                            return;
                        }
                        if (channelNumber < 1 || channelNumber > SSP.ChannelMax)
                        {
                            await e.Channel.SendMessage($"💢 {channelArg} isn't a channel.");
                            return;
                        }

                        Faction? faction = null;
                        if (factionArg.StartsWith("ceru", StringComparison.InvariantCultureIgnoreCase) ||
                            string.Equals(factionArg, "co", StringComparison.InvariantCultureIgnoreCase) ||
                            string.Equals(factionArg, "blue", StringComparison.InvariantCultureIgnoreCase))
                        {
                            faction = Faction.Cerulean;
                        }
                        else if (factionArg.StartsWith("crim", StringComparison.InvariantCultureIgnoreCase) ||
                                 string.Equals(factionArg, "cl", StringComparison.InvariantCultureIgnoreCase) ||
                                 string.Equals(factionArg, "red", StringComparison.InvariantCultureIgnoreCase))
                        {
                            faction = Faction.Crimson;
                        }
                        else
                        {
                            await e.Channel.SendMessage($"💢 {factionArg} isn't a faction.");
                            return;
                        }

                        if (!_ssp.SetChannelFaction(channelNumber, faction.Value))
                        {
                            await e.Channel.SendMessage($"💢 I need to know something about channel {channelNumber} first.");
                            return;
                        }

                        string factionName = faction == Faction.Cerulean ? "Cerulean" : "Crimson";

                        Message botMessage = await e.Channel.SendMessage($"Successfully set owner of channel {channelNumber} to {faction}.");
                        await Task.Delay(10000).ConfigureAwait(false);
                        await botMessage.Delete().ConfigureAwait(false);
                        if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                        {
                            await e.Message.Delete().ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand("unknownbattle")
                   .Alias("ub")
                   .Description($"Sets the time that battle is assumed to take if a channel is not updated. Default is {SSP.DefaultUnknownBattleLength.ToString(@"mm\:ss")}.**Usage**: `{Prefix} unknownbattle <time>")
                   .Parameter("time", ParameterType.Required)
                   .Do(async e =>
                   {
                       string timeArg = e.GetArg("time");

                       TimeSpan timeRemaining;
                       if (!PoopUtilities.TryParseTimeSpan(timeArg, out timeRemaining))
                       {
                           await e.Channel.SendMessage($"💢 {timeArg} isn't a valid amount of time.");
                           return;
                       }

                       if (!SSP.SetUnknownBattleLength(timeRemaining))
                       {
                           await e.Channel.SendMessage($"💢 {timeArg} isn't a valid amount of time for the Battle phase.");
                           return;
                       }
                       Message botMessage = await e.Channel.SendMessage($"Default capture time set to {SSP.UnknownBattleLength.ToString(@"mm\:ss")}.");
                       await Task.Delay(10000).ConfigureAwait(false);
                       await botMessage.Delete().ConfigureAwait(false);
                       if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                       {
                           await e.Message.Delete().ConfigureAwait(false);
                       }
                   });

                cgb.CreateCommand("list")
                    .Alias("l")
                    .Description("Lists the status of all known channels")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(_ssp.PrintSummary());
                    });

                cgb.CreateCommand("setstatus")
                    .Alias("ss")
                    .Description($"Sets the phase, time remaining, and (optionally) faction controlling for a Soulstone Plains channel.\n**Usage**: `{Prefix} setstatus <channel number> <phase name> <time remaining> <controlling faction (optional)>`\n**Example**: `{Prefix} setstatus 2 mining 23:00 crimson`")
                    .Parameter("channel", ParameterType.Required)
                    .Parameter("phase", ParameterType.Required)
                    .Parameter("time remaining", ParameterType.Required)
                    .Parameter("faction", ParameterType.Optional)
                    .Do(async e =>
                    {
                        string channelArg = e.GetArg("channel");
                        string phaseArg = e.GetArg("phase");
                        string timeRemainingArg = e.GetArg("time remaining");
                        string factionArg = e.GetArg("faction");

                        int channelNumber;
                        if (!int.TryParse(channelArg, out channelNumber))
                        {
                            await e.Channel.SendMessage($"💢 {channelArg} isn't a number.");
                            return;
                        }
                        if (channelNumber < 1 || channelNumber > SSP.ChannelMax)
                        {
                            await e.Channel.SendMessage($"💢 {channelArg} isn't a channel.");
                            return;
                        }

                        SSPPhase phase;
                        if (phaseArg.StartsWith("waiting", StringComparison.InvariantCultureIgnoreCase) &&
                            phaseArg.EndsWith("mining", StringComparison.InvariantCultureIgnoreCase))
                        {
                            phase = SSPPhase.WaitingForMining;
                        }
                        else if (phaseArg.StartsWith("waiting", StringComparison.InvariantCultureIgnoreCase) &&
                                 (phaseArg.EndsWith("capture", StringComparison.InvariantCultureIgnoreCase)
                                  || phaseArg.EndsWith("cap", StringComparison.InvariantCultureIgnoreCase)))
                        {
                            phase = SSPPhase.WaitingForCapture;
                        }
                        else if (phaseArg.StartsWith("cap"))
                        {
                            phase = SSPPhase.Capture;
                        }
                        else if (phaseArg.StartsWith("mining"))
                        {
                            phase = SSPPhase.Mining;
                        }
                        else if (phaseArg.StartsWith("battle"))
                        {
                            phase = SSPPhase.Battle;
                        }
                        else
                        {
                            await e.Channel.SendMessage($"💢 {phaseArg} isn't a valid soulstone plains phase. Try these:\n**waitingforcapture**\n**Capture**\n**Waitingformining**\n**mining**\n**battle**");
                            return;
                        }

                        TimeSpan timeRemaining;
                        if (!PoopUtilities.TryParseTimeSpan(timeRemainingArg, out timeRemaining))
                        {
                            await e.Channel.SendMessage($"💢 {timeRemainingArg} isn't a valid amount of time.");
                            return;
                        }

                        Faction? faction = null;
                        if (factionArg.StartsWith("ceru", StringComparison.InvariantCultureIgnoreCase) ||
                            string.Equals(factionArg, "co", StringComparison.InvariantCultureIgnoreCase) ||
                            string.Equals(factionArg, "blue", StringComparison.InvariantCultureIgnoreCase))
                        {
                            faction = Faction.Cerulean;
                        }
                        else if (factionArg.StartsWith("crim", StringComparison.InvariantCultureIgnoreCase) ||
                                 string.Equals(factionArg, "cl", StringComparison.InvariantCultureIgnoreCase) ||
                                 string.Equals(factionArg, "red", StringComparison.InvariantCultureIgnoreCase))
                        {
                            faction = Faction.Crimson;
                        }

                        if (!_ssp.SetChannel(channelNumber, phase, timeRemaining, faction))
                        {
                            await e.Channel.SendMessage($"💢 Couldn't save channel information. Does the time remaining of `{timeRemaining}` for the `{phaseArg}` phase make sense?");
                            return;
                        }
                        Message botMessage = await e.Channel.SendMessage($"Successfully saved channel `{channelNumber}`'s information:\n" + _ssp.PrintChannelSummary(channelNumber));
                        await Task.Delay(10000).ConfigureAwait(false);
                        await botMessage.Delete().ConfigureAwait(false);
                        if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                        {
                            await e.Message.Delete().ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand("delete")
                   .Alias("d")
                   .Description("Deletes all information about a channel")
                   .Parameter("channel", ParameterType.Required)
                   .Do(async e =>
                   {
                       int channelNumber;
                       var channel = e.GetArg("channel");
                       if (string.IsNullOrWhiteSpace("channel"))
                       {
                           await e.Channel.SendMessage("💢 You need to tell me which channel to delete.");
                           return;
                       }
                       if (!int.TryParse(channel, out channelNumber))
                       {
                           await e.Channel.SendMessage($"💢 `{channel}` isn't a number.");
                           return;
                       }
                       if (!_ssp.DeleteChannel(channelNumber))
                       {
                           await e.Channel.SendMessage($"💢 Could not delete channel `{channelNumber}`. Are you sure it's a channel?");
                           return;
                       }
                       Message botMessage = await e.Channel.SendMessage($"Successfully deleted all information about channel `{channelNumber}`.");
                       await Task.Delay(10000).ConfigureAwait(false);
                       await botMessage.Delete().ConfigureAwait(false);
                       if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                       {
                           await e.Message.Delete().ConfigureAwait(false);
                       }
                   });

                cgb.CreateCommand("update")
                    .Alias("u")
                    .Description(
                        $"Updates the status of a channel entity: Konta, Chosu, Suljun, Kuchon, and the Drills.\n**Usage**: `{Prefix} update <channel> <entity> <dead/alive>`\n**Examples**: `{Prefix} update 1 Konta dead` `{Prefix} update 2 southdrill dead`")
                    .Parameter("channel", ParameterType.Required)
                    .Parameter("entity", ParameterType.Required)
                    .Parameter("status", ParameterType.Required)
                    .Do(async e =>
                    {
                        var channelArg = e.GetArg("channel");
                        var entityArg = e.GetArg("entity");
                        var statusArg = e.GetArg("status");

                        int channelNumber;
                        if (!int.TryParse(channelArg, out channelNumber))
                        {
                            await e.Channel.SendMessage($"💢 {channelArg} isn't a number.");
                            return;
                        }
                        if (!_ssp.IsChannelDataEntered(channelNumber))
                        {
                            await e.Channel.SendMessage($"💢 You need to enter channel data first.");
                            return;
                        }

                        SSPEntity entity;
                        if (entityArg.StartsWith("sulj", StringComparison.InvariantCultureIgnoreCase) ||
                            entityArg.StartsWith("kon", StringComparison.InvariantCultureIgnoreCase))
                        {
                            entity = SSPEntity.NorthMiner;
                        }
                        else if (entityArg.StartsWith("kuch", StringComparison.InvariantCultureIgnoreCase) ||
                                 entityArg.StartsWith("cho", StringComparison.InvariantCultureIgnoreCase))
                        {
                            entity = SSPEntity.SouthMiner;
                        }
                        else if (entityArg.StartsWith("nor", StringComparison.CurrentCultureIgnoreCase) &&
                                 entityArg.EndsWith("drill", StringComparison.CurrentCultureIgnoreCase))
                        {
                            entity = SSPEntity.NorthDrill;
                        }
                        else if (entityArg.StartsWith("sou", StringComparison.CurrentCultureIgnoreCase) &&
                                 entityArg.EndsWith("drill", StringComparison.InvariantCultureIgnoreCase))
                        {
                            entity = SSPEntity.SouthDrill;
                        }
                        else
                        {
                            await e.Channel.SendMessage($"💢 I don't understand who or what `{entityArg}` is.");
                            return;
                        }

                        bool? isAlive = null;
                        if (string.Equals(statusArg, "dead", StringComparison.InvariantCultureIgnoreCase))
                        {
                            isAlive = false;
                        }
                        else if (string.Equals(statusArg, "alive", StringComparison.InvariantCultureIgnoreCase))
                        {
                            isAlive = true;
                        }
                        else
                        {
                            await e.Channel.SendMessage($"💢 Entity must be set to either `dead` or `alive`.");
                            return;
                        }

                        string readableStatus = isAlive.Value ? "alive" : "dead";

                        if (!_ssp.UpdateChannelEntity(channelNumber, entity, isAlive.Value))
                        {
                            await e.Channel.SendMessage($"💢 Could not set {entity} as {readableStatus} on channel {channelNumber}.");
                            return;
                        }
                        Message botMessage = await e.Channel.SendMessage($"Successfully marked {entity} as {readableStatus} on channel {channelNumber}.");
                        await Task.Delay(10000).ConfigureAwait(false);
                        await botMessage.Delete().ConfigureAwait(false);
                        if (e.Server.CurrentUser.GetPermissions(e.Channel).ManageMessages)
                        {
                            await e.Message.Delete().ConfigureAwait(false);
                        }
                    });
            });
        }
    }

    internal static class PoopUtilities
    {
        internal static bool TryParseTimeSpan(string timeInput, out TimeSpan timeSpan)
        {
            string timeToParse;
            string[] pieces = timeInput.Split(':');
            switch (pieces.Length)
            {
                case 1:
                    // TODO: make this better
                    timeToParse = "00:" + timeInput + ":00";
                    break;
                case 2:
                    timeToParse = "00:" + timeInput;
                    break;
                default:
                    timeToParse = timeInput;
                    break;
            }
            return TimeSpan.TryParse(timeToParse, out timeSpan);
        }
    }

    internal class SSP
    {
        public const int DefaultChannelMax = 6;
        public static readonly TimeSpan DefaultUnknownBattleLength = TimeSpan.FromMinutes(16);
        public static readonly TimeSpan DefaultUnknownCaptureLength = TimeSpan.FromMinutes(5);

        public static int ChannelMax = DefaultChannelMax;

        // TODO: should this go here?
        public static TimeSpan UnknownBattleLength { get; private set; } = DefaultUnknownBattleLength;
        public static TimeSpan UnknownCaptureLength { get; private set; } = DefaultUnknownCaptureLength;

        public static bool SetUnknownBattleLength(TimeSpan length)
        {
            if (length < TimeSpan.Zero || length > SSPPhase.Battle.MaximumLength)
            {
                return false;
            }
            UnknownBattleLength = length;
            return true;
        }

        public static bool SetUnknownCaptureLength(TimeSpan length)
        {
            if (length < TimeSpan.Zero || length > SSPPhase.Capture.MaximumLength)
            {
                return false;
            }
            UnknownCaptureLength = length;
            return true;
        }

        private ConcurrentDictionary<int, SSPChannel> SSPChannels { get; } = new ConcurrentDictionary<int, SSPChannel>();

        public string PrintSummary()
        {
            if (!SSPChannels.Any())
            {
                return "No SSP data loaded...";
            }

            var sb = new StringBuilder();

            for (var channelNumber = 1; channelNumber <= SSPChannels.Keys.Max(); channelNumber++)
            {
                sb.Append($"**Channel {channelNumber}**: ");

                sb.Append(PrintChannelSummary(channelNumber)).Append("\n");
            }

            return sb.ToString();
        }

        public string PrintChannelSummary(int channelNumber)
        {
            SSPChannel channel;
            SSPChannels.TryGetValue(channelNumber, out channel);
            return channel == null ? "No data loaded." : channel.PrettyPrint();
        }

        public bool IsChannelDataEntered(int channelNumber)
        {
            return SSPChannels.ContainsKey(channelNumber);
        }

        public bool DeleteChannel(int channelNumber)
        {
            SSPChannel channel;
            return SSPChannels.ContainsKey(channelNumber) && SSPChannels.TryRemove(channelNumber, out channel);
        }

        public bool SetChannel(int channelNumber, SSPPhase phase, TimeSpan timeRemaining, Faction? faction)
        {
            SSPChannel channel;
            SSPChannels.TryGetValue(channelNumber, out channel);
            if (channel != null)
            {
                return channel.SetInfo(phase, timeRemaining, faction);
            }

            channel = new SSPChannel();
            return channel.SetInfo(phase, timeRemaining, faction) && SSPChannels.TryAdd(channelNumber, channel);
        }

        public bool UpdateChannelEntity(int channelNumber, SSPEntity entity, bool isAlive)
        {
            SSPChannel channel;
            SSPChannels.TryGetValue(channelNumber, out channel);
            return channel != null && channel.UpdateEntity(entity, isAlive);
        }

        public bool SetChannelFaction(int channelNumber, Faction faction)
        {
            SSPChannel channel;
            SSPChannels.TryGetValue(channelNumber, out channel);
            if (channel == null) return false;

            channel.SetFaction(faction);
            return true;
        }
    }

    internal class SSPChannel
    {
        private readonly ConcurrentStack<CancellationTokenSource> tokenSources =
            new ConcurrentStack<CancellationTokenSource>();

        public bool SetInfo(SSPPhase phase, TimeSpan timeRemaining, Faction? faction)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (timeRemaining <= TimeSpan.Zero) return false;

            if (timeRemaining > phase.MaximumLength) return false;

            PhaseEndTime = DateTimeOffset.UtcNow + timeRemaining;
            IsTimeRemainingAGuess = false;

            if (CurrentPhase != SSPPhase.Mining || phase != SSPPhase.Mining || DateTimeOffset.UtcNow - TimePhaseAndTimeSet > SSPPhase.Mining.MaximumLength)
            {
                NorthDrillIsDead = false;
                NorthMinerIsDead = false;
                SouthDrillIsDead = false;
                SouthMinerIsDead = false;
            }

            CurrentPhase = phase;

            if ((CurrentPhase == SSPPhase.WaitingForMining || CurrentPhase == SSPPhase.Mining) && faction.HasValue)
            {
                ControllingFaction = faction.Value;
            }
            else
            {
                ControllingFaction = null;
            }

            TimePhaseAndTimeSet = now;

            foreach (CancellationTokenSource cts in tokenSources)
            {
                cts.Cancel();
            }

            tokenSources.Clear();

            var newSource = new CancellationTokenSource();

            tokenSources.Push(newSource);

            // TODO: DRY
            TimeSpan timeToWait;
            if (CurrentPhase == SSPPhase.Capture)
            {
                timeToWait = new List<TimeSpan> { SSP.UnknownCaptureLength, TimeRemaining }.Min();
            }
            else if (CurrentPhase == SSPPhase.Battle)
            {
                timeToWait = new List<TimeSpan> { SSP.UnknownBattleLength, TimeRemaining }.Min();
            }
            else
            {
                timeToWait = TimeRemaining;
            }

#pragma warning disable 4014
            AdvanceToNextPhaseAsync(newSource.Token, timeToWait);
#pragma warning restore 4014

            return true;
        }

        public bool UpdateEntity(SSPEntity entity, bool isAlive)
        {
            switch (entity)
            {
                case SSPEntity.NorthMiner:
                    if (!isAlive)
                    {
                        NorthDrillIsDead = true;
                        NorthMinerIsDead = true;
                    }
                    else
                    {
                        NorthMinerIsDead = false;
                    }
                    return true;
                case SSPEntity.SouthMiner:
                    if (!isAlive)
                    {
                        SouthDrillIsDead = true;
                        SouthMinerIsDead = true;
                    }
                    else
                    {
                        SouthMinerIsDead = false;
                    }
                    return true;
                case SSPEntity.NorthDrill:
                    NorthDrillIsDead = !isAlive;
                    return true;
                case SSPEntity.SouthDrill:
                    SouthDrillIsDead = !isAlive;
                    return true;
                default:
                    return false;
            }
        }

        public string PrettyPrint()
        {
            if (TimeRemaining <= TimeSpan.Zero)
            {
                return "*Unknown*";
            }

            var sb = new StringBuilder($"**{CurrentPhase.Name}** phase, with **{TimeRemaining.ToString(@"mm\:ss")}** remaining");

            if (IsTimeRemainingAGuess)
            {
                sb.Append(", *but this is just a guess*");
            }
            sb.Append(".");

            if ((CurrentPhase == SSPPhase.WaitingForMining || CurrentPhase == SSPPhase.Mining) &&
                ControllingFaction.HasValue)
            {
                string factionName = ControllingFaction.Value == Faction.Cerulean ? "**Cerulean**" : "**Crimson**";
                sb.Append($" Controlled by {factionName}.");
            }

            TimeSpan lastUpdated = DateTimeOffset.UtcNow - TimePhaseAndTimeSet;
            if (lastUpdated > TimeSpan.FromMinutes(5))
            {
                sb.Append($" Last updated {Math.Round(lastUpdated.TotalMinutes)} minute(s) ago.");
            }

            if (CurrentPhase != SSPPhase.Mining) return sb.ToString();

            sb.Append("\n*North*:\n");
            switch (ControllingFaction)
            {
                case Faction.Cerulean:
                    sb.Append("- Konta: ");
                    break;
                case Faction.Crimson:
                    sb.Append("- Suljun: ");
                    break;
                case null:
                default:
                    sb.Append("- Miner: ");
                    break;
            }
            sb.Append(NorthMinerIsDead ? ":x:" : ":white_check_mark:");

            sb.Append("\n- Drill: ");
            sb.Append(NorthDrillIsDead ? ":x:" : ":white_check_mark:");


            sb.Append("\n*South*:\n");
            switch (ControllingFaction)
            {
                case Faction.Cerulean:
                    sb.Append("- Chosu: ");
                    break;
                case Faction.Crimson:
                    sb.Append("- Kuchon: ");
                    break;
                case null:
                default:
                    sb.Append("- Miner: ");
                    break;
            }
            sb.Append(SouthMinerIsDead ? ":x:" : ":white_check_mark:");

            sb.Append("\n- Drill: ");
            sb.Append(SouthDrillIsDead ? ":x:" : ":white_check_mark:");

            return sb.ToString();
        }

        public void SetFaction(Faction faction)
        {
            ControllingFaction = faction;
        }

        public Faction? ControllingFaction { get; private set; }

        private SSPPhase CurrentPhase { get; set; }

        private TimeSpan TimeRemaining
        {
            get
            {
                TimeSpan time = PhaseEndTime - DateTimeOffset.UtcNow;
                return time > TimeSpan.Zero ? time : TimeSpan.Zero;
            }
        }

        private DateTimeOffset PhaseEndTime { get; set; }

        private async Task AdvanceToNextPhaseAsync(CancellationToken token, TimeSpan? initialDelay = null)
        {
            if (token.IsCancellationRequested) return;

            if (initialDelay != null)
            {
                await Task.Delay(initialDelay.Value).ConfigureAwait(false);
            }

            if (token.IsCancellationRequested) return;

            if (DateTimeOffset.UtcNow - TimePhaseAndTimeSet > TimeSpan.FromMinutes(90))
            {
                return;
            }

            CurrentPhase = CurrentPhase.GetNextPhase();
            if (CurrentPhase == SSPPhase.WaitingForMining || CurrentPhase == SSPPhase.WaitingForCapture)
            {
                IsTimeRemainingAGuess = true;
            }
            if (CurrentPhase != SSPPhase.Mining && CurrentPhase != SSPPhase.WaitingForMining)
            {
                ControllingFaction = null;
            }
            PhaseEndTime = DateTimeOffset.UtcNow + CurrentPhase.MaximumLength;

            // TODO: dry
            TimeSpan timeToWait;
            if (CurrentPhase == SSPPhase.Capture)
            {
                timeToWait = SSP.UnknownCaptureLength;
            }
            else if (CurrentPhase == SSPPhase.Battle)
            {
                timeToWait = SSP.UnknownBattleLength;
            }
            else
            {
                timeToWait = CurrentPhase.MaximumLength;
            }

            await Task.Delay(timeToWait).ConfigureAwait(false);
            await AdvanceToNextPhaseAsync(token).ConfigureAwait(false);
        }

        private bool IsTimeRemainingAGuess { get; set; }

        private DateTimeOffset TimePhaseAndTimeSet { get; set; }

        private bool NorthMinerIsDead { get; set; }

        private bool SouthMinerIsDead { get; set; }

        private bool NorthDrillIsDead { get; set; }

        private bool SouthDrillIsDead { get; set; }
    }

    internal class SSPPhase
    {
        public static SSPPhase WaitingForCapture = new SSPPhase
        {
            MaximumLength = TimeSpan.FromMinutes(11),
            HasControllingFaction = false,
            Name = "Waiting for capture"
        };

        public static SSPPhase Capture = new SSPPhase
        {
            MaximumLength = TimeSpan.FromMinutes(12),
            HasControllingFaction = false,
            Name = "Capture"
        };
        public static SSPPhase WaitingForMining = new SSPPhase
        {
            MaximumLength = TimeSpan.FromMinutes(1),
            HasControllingFaction = true,
            Name = "Waiting for mining"
        };
        public static SSPPhase Mining = new SSPPhase
        {
            MaximumLength = TimeSpan.FromMinutes(33),
            HasControllingFaction = true,
            Name = "Mining"
        };
        public static SSPPhase Battle = new SSPPhase
        {
            MaximumLength = TimeSpan.FromMinutes(16),
            HasControllingFaction = true,
            Name = "Battle"
        };

        private SSPPhase() { }

        public TimeSpan MaximumLength { get; private set; }

        public bool HasControllingFaction { get; private set; }

        public string Name { get; private set; }

        public SSPPhase GetNextPhase()
        {
            if (this == WaitingForCapture)
            {
                return Capture;
            }
            if (this == Capture)
            {
                return WaitingForMining;
            }
            if (this == WaitingForMining)
            {
                return Mining;
            }
            if (this == Mining)
            {
                return Battle;
            }
            if (this == Battle)
            {
                return WaitingForCapture;
            }
            throw new InvalidOperationException("Unknown SSP phase");
        }
    }

    internal enum SSPEntity
    {
        NorthMiner = 1,
        SouthMiner,
        NorthDrill,
        SouthDrill
    }

    internal enum Faction
    {
        Cerulean = 1,
        Crimson
    }
}
