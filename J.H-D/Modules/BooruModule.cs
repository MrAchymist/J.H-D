﻿using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using J.H_D.Data;
using J.H_D.Minions;
using J.H_D.Minions.NSFW;
using J.H_D.Tools;
using System.Globalization;
using BooruSharp.Search.Tag;

namespace J.H_D.Modules
{
    class BooruModule : ModuleBase
    {
        [Command("Konachan", RunMode = RunMode.Async), Priority(-1)]
        [Help("Booru", "Find an image on Konachan, only send Safe images in non-NSFW channels", Warnings.NSFW | Warnings.Spoilers)]
        [Parameter("Search", "One or multiple tags you're looking for", ParameterType.Optional)]
        public async Task SearchKonachanAsync(params string[] Args)
        {
            await Program.DoActionAsync(Context.User, Context.Guild.Id, Module.Booru);

            var result = await BooruMinion.GetBooruImageAsync(new BooruMinion.BooruOptions(BooruMinion.BooruType.Konachan, Args, Utilities.IsChannelNsfw(Context)));

            await ProccessResultAsync(result).ConfigureAwait(false);
        }

        [Command("Konachan with infos", RunMode = RunMode.Async)]
        [Help("Booru", "Find an image on Konachan and return it with informations about the characters, the tags, and the artist", Warnings.NSFW | Warnings.Spoilers)]
        [Parameter("Search", "One or multiple tags you're looking for", ParameterType.Optional)]
        public async Task SearchWithBonusAsync(params string[] Args)
        {
            await Program.DoActionAsync(Context.User, Context.Guild.Id, Module.Booru);

            var result = await BooruMinion.GetBooruImageAsync(new BooruMinion.BooruOptions(BooruMinion.BooruType.Konachan, Args, Utilities.IsChannelNsfw(Context)));

            await ProccessInfosResultAsync(result, BooruMinion.BooruType.Konachan).ConfigureAwait(false);
        }

        private async Task ProccessResultAsync(FeatureRequest<BooruSharp.Search.Post.SearchResult, Error.Booru> Result)
        {
            if (!Utilities.IsChannelNsfw(Context) && Result.Answer.rating != BooruSharp.Search.Post.Rating.Safe)
            {
                await ReplyAsync("No safe image was found with theses parameters, please try on an NSFW channel or with others");
                return;
            }

            switch (Result.Error)
            {
                case Error.Booru.NotFound:
                    await ReplyAsync("I don't know what you're looking for, but it's definitively not here!");
                    break;

                case Error.Booru.None:
                    await ReplyAsync("", false, BuildImageEmbed(Result.Answer)).ConfigureAwait(false);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private async Task ProccessInfosResultAsync(FeatureRequest<BooruSharp.Search.Post.SearchResult, Error.Booru> Result, BooruMinion.BooruType Website)
        {
            if (!Utilities.IsChannelNsfw(Context) && Result.Answer.rating != BooruSharp.Search.Post.Rating.Safe)
            {
                await ReplyAsync("No safe image was found with theses parameters, please try on an NSFW channel or with others");
                return;
            }

            switch (Result.Error)
            {
                case Error.Booru.NotFound:
                    await ReplyAsync("I don't know what you're looking for, but it's definitively not here!");
                    break;

                case Error.Booru.None:
                    await ReplyAsync("", false, await BuildImageInfosEmbedAsync(Result.Answer, Website).ConfigureAwait(false));
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private Embed BuildImageEmbed(BooruSharp.Search.Post.SearchResult Result)
        {
            EmbedBuilder emb = new EmbedBuilder
            {
                Title = "Sauce",
                Url = Result.source,
                ImageUrl = Result.fileUrl.AbsoluteUri,
            };

            switch (Result.rating)
            {
                case BooruSharp.Search.Post.Rating.Safe:
                    emb.Color = Color.Green;
                    break;

                case BooruSharp.Search.Post.Rating.Questionable:
                    emb.Color = Color.LightOrange;
                    break;

                case BooruSharp.Search.Post.Rating.Explicit:
                    emb.Color = Color.Purple;
                    break;

                default:
                    throw new NotSupportedException();
            }
            
            emb.Footer = new EmbedFooterBuilder
            {
                Text = $"Posted the {Result.creation}"
            };
            return emb.Build();
        }

        private async Task<Embed> BuildImageInfosEmbedAsync(BooruSharp.Search.Post.SearchResult Result, BooruMinion.BooruType Website)
        {
            EmbedBuilder emb = new EmbedBuilder
            {
                Title = "Sauce",
                Url = Result.source,
                ImageUrl = Result.fileUrl.AbsoluteUri,
            };

            switch (Result.rating)
            {
                case BooruSharp.Search.Post.Rating.Safe:
                    emb.Color = Color.Blue;
                    break;

                case BooruSharp.Search.Post.Rating.Questionable:
                    emb.Color = Color.LightOrange;
                    break;

                case BooruSharp.Search.Post.Rating.Explicit:
                    emb.Color = Color.Purple;
                    break;

                default:
                    throw new NotSupportedException();
            }

            var TagResults = await BooruMinion.GetTagsAsync(Website, Result.tags, BooruSharp.Search.Tag.TagType.Metadata);
            List<BooruSharp.Search.Tag.SearchResult> FoundTags = TagResults.Answer;

            string Artist = BuildTagsString(FoundTags, BooruSharp.Search.Tag.TagType.Artist);
            string Parodies = BuildTagsString(FoundTags, BooruSharp.Search.Tag.TagType.Copyright);
            string GeneralTags = BuildTagsString(FoundTags, BooruSharp.Search.Tag.TagType.Trivia);
            string Characters = BuildTagsString(FoundTags, BooruSharp.Search.Tag.TagType.Character);

            emb.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Artist",
                Value = Artist ?? "Not found"
            });

            emb.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Parodies",
                Value = Parodies ?? "Original"
            });

            emb.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Characters",
                Value = Characters ?? "Original"
            });

            emb.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Tags",
                Value = GeneralTags ?? ""
            });

            emb.Footer = new EmbedFooterBuilder
            {
                Text = $"Posted the {Result.creation}"
            };
            return emb.Build();
        }

        private string BuildTagsString(List<BooruSharp.Search.Tag.SearchResult> TagsList, BooruSharp.Search.Tag.TagType tagType)
        {
            string TagsString = null;

            if (tagType == BooruSharp.Search.Tag.TagType.Character) {
                foreach (var tag in TagsList.Where(x => x.type == tagType)) {
                    TagsString = $"{TagsString}{CleanTag(tag.name, tagType == BooruSharp.Search.Tag.TagType.Character)}{Environment.NewLine}";
                }
            }

            return TagsString;
        }

        private string CleanTag(string tag, bool name = false)
        {
            tag = tag.Replace('_', ' ');
            tag = char.ToUpper(tag[0], CultureInfo.InvariantCulture) + tag.Substring(1);

            if (name)
            {
                string[] Name = tag.Split(' ');
                for (int i = 0; i < Name.Length; i++)
                    Name[i] = char.ToUpper(Name[i][0], CultureInfo.InvariantCulture) + Name[i].Substring(1);

                tag = String.Join(" ", Name);
            }

            return tag;
        }
    }
}
