﻿using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games.Commands
{
    /// <summary>
    /// Flower picking/planting idea is given to me by its
    /// inceptor Violent Crumble from Game Developers League discord server
    /// (he has !cookie and !nom) Thanks a lot Violent!
    /// Check out GDL (its a growing gamedev community):
    /// https://discord.gg/0TYNJfCU4De7YIk8
    /// </summary>
    class PlantPick : DiscordCommand
    {

        private Random rng;
        public PlantPick(DiscordModule module) : base(module)
        {
            NadekoBot.Client.MessageReceived += PotentialFlowerGeneration;
            rng = new Random();
        }


        private async void PotentialFlowerGeneration(object sender, Discord.MessageEventArgs e)
        {
            if (e.Server == null || e.Channel.IsPrivate)
                return;
            var config = Classes.SpecificConfigurations.Default.Of(e.Server.Id);
            if (config.GenerateCurrencyChannels.Contains(e.Channel.Id))
            {
                var rnd = Math.Abs(GetRandomNumber());
                if ((rnd % 50) == 0)
                {
                    var msg = await e.Channel.SendFile(GetRandomCurrencyImagePath());
                    await e.Channel.SendMessage($"❗ A random {NadekoBot.Config.CurrencyName} appeared! Pick it up by typing `>pick`");
                    plantedFlowerChannels.AddOrUpdate(e.Channel.Id, msg, (u, m) => { m.Delete().GetAwaiter().GetResult(); return msg; });
                }
            }
        }
        //channelid/messageid pair
        ConcurrentDictionary<ulong, Message> plantedFlowerChannels = new ConcurrentDictionary<ulong, Message>();

        private object locker = new object();

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "pick")
                .Description("Picks a flower planted in this channel.")
                .Do(async e =>
                {
                    Message msg;

                    await e.Message.Delete().ConfigureAwait(false);
                    if (!plantedFlowerChannels.TryRemove(e.Channel.Id, out msg))
                        return;

                    await msg.Delete().ConfigureAwait(false);
                    await FlowersHandler.AddFlowersAsync(e.User, "Picked a flower.", 1, true).ConfigureAwait(false);
                    msg = await e.Channel.SendMessage($"**{e.User.Name}** picked a {NadekoBot.Config.CurrencyName}!").ConfigureAwait(false);
                    await Task.Delay(10000).ConfigureAwait(false);
                    await msg.Delete().ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "plant")
                .Description("Spend a flower to plant it in this channel. (If bot is restarted or crashes, flower will be lost)")
                .Do(async e =>
                {
                    lock (locker)
                    {
                        if (plantedFlowerChannels.ContainsKey(e.Channel.Id))
                        {
                            e.Channel.SendMessage($"There is already a {NadekoBot.Config.CurrencyName} in this channel.");
                            return;
                        }
                        var removed = FlowersHandler.RemoveFlowers(e.User, "Planted a flower.", 1);
                        if (!removed)
                        {
                            e.Channel.SendMessage($"You don't have any {NadekoBot.Config.CurrencyName}s.").Wait();
                            return;
                        }

                        var file = GetRandomCurrencyImagePath();
                        Message msg;
                        //todo send message after, not in lock
                        if (file == null)
                            msg = e.Channel.SendMessage(NadekoBot.Config.CurrencySign).GetAwaiter().GetResult();
                        else
                            msg = e.Channel.SendFile(file).GetAwaiter().GetResult();
                        plantedFlowerChannels.TryAdd(e.Channel.Id, msg);
                    }
                    var vowelFirst = new[] { 'a', 'e', 'i', 'o', 'u' }.Contains(NadekoBot.Config.CurrencyName[0]);
                    var msg2 = await e.Channel.SendMessage($"Oh how Nice! **{e.User.Name}** planted {(vowelFirst ? "an" : "a")} {NadekoBot.Config.CurrencyName}. Pick it using {Module.Prefix}pick");
                    await Task.Delay(20000).ConfigureAwait(false);
                    await msg2.Delete().ConfigureAwait(false);
                });

            cgb.CreateCommand(Prefix + "gencurrency")
                .Alias(Prefix + "gc")
                .Description($"Toggles currency generation on this channel. Every posted message will have 2% chance to spawn a {NadekoBot.Config.CurrencyName}. Requires Manage Messages permission. | `>gc`")
                .AddCheck(SimpleCheckers.ManageMessages())
                .Do(async e =>
                {
                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
                    if (config.GenerateCurrencyChannels.Remove(e.Channel.Id))
                    {
                        await e.Channel.SendMessage("`Currency generation disabled on this channel.`");
                    }
                    else
                    {
                        config.GenerateCurrencyChannels.Add(e.Channel.Id);
                        await e.Channel.SendMessage("`Currency generation enabled on this channel.`");
                    }
                });
        }

        private string GetRandomCurrencyImagePath() =>
            Directory.GetFiles("data/currency_images").OrderBy(s => rng.Next()).FirstOrDefault();

        int GetRandomNumber()
        {
            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider())
            {
                byte[] rno = new byte[4];
                rg.GetBytes(rno);
                int randomvalue = BitConverter.ToInt32(rno, 0);
                return randomvalue;
            }
        }
    }
}
