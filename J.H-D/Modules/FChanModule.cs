﻿using Discord;
using Discord.Commands;
using J.H_D.Minions;
using J.H_D.Minions.Responses;
using J.H_D.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;

namespace J.H_D.Modules
{
    class FChanModule : ModuleBase
    {
        [Command("FChan available boards"), Alias("4chan boards")]
        public async Task DisplayFchanBoards()
        {
            await Program.p.DoAction(Context.User, Context.Message.Id, Program.Module.Forum);

            List<Response.FBoard> Boards = await FChanMinion.UpdateAvailableChans();
            await ReplyAsync("", false, BoardInfosBuilder(Boards));
        }

        [Command("Fchan board info"), Alias("4chan board info")]
        public async Task GetBoardInfos(params string[] Args)
        {
            await Program.p.DoAction(Context.User, Context.Message.Id, Program.Module.Forum);

            var result = await FChanMinion.GetBoardInfo(Args);

            switch (result.Error)
            {
                case Error.FChan.Unavailable:
                    await ReplyAsync("The chan you're looking for is unavailable or doesn't exist");
                    break;

                case Error.FChan.ThreadExpired:
                    await ReplyAsync("The thread you're looking for has expired");
                    break;

                case Error.FChan.None:
                    await ReplyAsync("", false, BoardInfos(result.Answer));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        [Command("Random 4image"), Alias("4chan image")]
        public async Task RandomImage(params string[] Args)
        {
            // For later, to make more easy-to-use command
            string AskArgs = null;

            var result = new FeatureRequest<Response.FThread, Error.FChan>();
            ITextChannel chan = (ITextChannel)Context.Channel;

            if (Args.Length >= 1)
            {
                string OneArg = Utilities.MakeArgs(Args);
                result = await FChanMinion.GetRandomThreadFrom(OneArg, new FChanMinion.RequestOptions()
                {
                    MandatoryWord = null,
                    AllowNsfw = chan.IsNsfw,
                    RequestType = FChanMinion.RequestType.Image
                });
            }
            else
                result = await FChanMinion.GetRandomThreadFrom(null, new FChanMinion.RequestOptions()
                {
                    MandatoryWord = null,
                    AllowNsfw = chan.IsNsfw,
                    RequestType = FChanMinion.RequestType.Image
                });

            switch (result.Error)
            {
                case Error.FChan.Unavailable:
                    await ReplyAsync("The chan you're looking for is unavailable or doesn't exist");
                    break;

                case Error.FChan.None:
                    await ReplyAsync("", false, ThreadImageBuild(result.Answer));
                    break;

                case Error.FChan.Nsfw:
                    await ReplyAsync("The board you've asked for is NSFW, please retry in a non-sfw channel");
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        [Command("Random 4thread"), Alias("Random 4chan thread"), Priority(-1)]
        public async Task RandomThread(params string[] Args)
        {
            // For later, to make more easy-to-use command
            string AskArgs = null;

            ITextChannel chan = (ITextChannel)Context.Channel;

            var result = new FeatureRequest<Response.FThread, Error.FChan>();

            if (Args.Length >= 1)
            {
                string OneArg = Utilities.MakeArgs(Args);
                result = await FChanMinion.GetRandomThreadFrom(OneArg, new FChanMinion.RequestOptions()
                {
                    RequestType = FChanMinion.RequestType.Thread,
                    MandatoryWord = null,
                    AllowNsfw = chan.IsNsfw
                });
            }
            else
                result = await FChanMinion.GetRandomThreadFrom(null, new FChanMinion.RequestOptions()
                {
                    RequestType = FChanMinion.RequestType.Thread,
                    MandatoryWord = null,
                    AllowNsfw = chan.IsNsfw
                });

            switch (result.Error)
            {
                case Error.FChan.Unavailable:
                    await ReplyAsync("The chan you're looking for is unavailable or doesn't exist");
                    break;

                case Error.FChan.None:
                    await ReplyAsync("", false, ThreadInfosEmbed(result.Answer));
                    break;

                case Error.FChan.Nsfw:
                    await ReplyAsync("The board you've asked for is NSFW, please retry in a non-sfw channel");
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private Embed ThreadInfosEmbed(Response.FThread thread)
        {
            EmbedBuilder emb = new EmbedBuilder()
            {
                Title = $"From {thread.From} on {thread.Chan}",
                Url = $"http://4chan.org/{thread.Chan}/thread/{thread.ThreadId}",
                Color = Color.DarkGreen,
                Description = Utilities.GetPlainTextFromHtml(thread.Comm)
            };
            if (thread.Filename != null)
            {
                emb.ImageUrl = $"http://i.4cdn.org/{thread.Chan}/{thread.Tim}/{thread.Extension}";
                emb.Footer = new EmbedFooterBuilder()
                {
                    Text = $"{thread.Filename}{thread.Extension}"
                };
            }
            return emb.Build();
        }

        private Embed ThreadImageBuild(Response.FThread image)
        {
            EmbedBuilder emb = new EmbedBuilder()
            {
                Title = $"From {image.From} on {image.Chan}",
                ImageUrl = string.Format($"http://i.4cdn.org/{image.Chan}/{image.Tim}{image.Extension}"),
                Color = Color.DarkGreen
            };
            return emb.Build();
        }

        private Embed BoardInfos(Response.FBoard Board)
        {
            EmbedBuilder emb = new EmbedBuilder()
            {
                Title = Board.Name,
                Url = "https://4chan.org/" + Board.Title,
                Description = Utilities.GetPlainTextFromHtml(Board.Description)
            };
            emb.Color = Color.Green;
            if (Board.Nsfw || Board.Spoilers)
            {
                emb.Color = Color.DarkOrange;
                string WarningPhrase = null;
                if (Board.Nsfw)
                    WarningPhrase += "- This board contains NSFW content" + Environment.NewLine;
                if (Board.Spoilers)
                    WarningPhrase += "- This board can contains Spoilers";
                emb.AddField("Warning", WarningPhrase);
            }

            return emb.Build();
        }

        private Embed BoardInfosBuilder(List<Response.FBoard> Boards)
        {
            string SafeList = null;
            string NSFWList = null;

            EmbedBuilder emb = new EmbedBuilder()
            {
                Title = "4Chan available boards",
                Url = "https://4chan.org/",
                Color = Color.Green
            };

            foreach (Response.FBoard board in Boards)
            {
                if (board.Nsfw == false)
                    SafeList += board.Title + " - " + board.Name + Environment.NewLine;
                else
                    NSFWList += board.Title + " - " + board.Name + Environment.NewLine;
            }

            emb.AddField("Safe chans", SafeList, true);
            emb.AddField("NSFW chans", NSFWList, true);

            return emb.Build();
        }
    }
}