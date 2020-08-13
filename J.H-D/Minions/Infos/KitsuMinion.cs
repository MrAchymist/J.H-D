﻿using J.H_D.Data;
using J.H_D.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anime = J.H_D.Data.Response.Anime;

namespace J.H_D.Minions.Infos
{
    class KitsuMinion
    {
        public static async Task<FeatureRequest<Anime, Error.Anime>> SearchAnime(string[] Args)
        {
            string SearchName = Utilities.MakeQueryArgs(Args);
            if (SearchName.Length == 0)
                return new FeatureRequest<Anime, Error.Anime>(null, Error.Anime.Help);

            dynamic json = JsonConvert.DeserializeObject(await Program.p.KitsuClient.GetStringAsync($"https://kitsu.io/api/edge/anime?page[limit]=1&filter[text]=" + SearchName));


            if (json == null)
                return new FeatureRequest<Anime, Error.Anime>(null, Error.Anime.NotFound);

            dynamic Results = json.data[0];
            dynamic Attributes = Results.attributes;

            Anime Response = new Anime()
            {
                Id = Results.id,
                Synopsis = Attributes.synopsis,
                Title = Attributes.titles.en,
                LATitle = Attributes.titles.en_jp,
                OriginalTitle = Attributes.titles.ja_jp,
                Rating = Attributes.averageRating,
                StartDate = Attributes.startDate,
                EndDate = Attributes.endDate,
                AgeRating = Attributes.ageRating,
                Guideline = Attributes.ageRatingGuide,
                Status = Attributes.status,
                PosterImage = Attributes.posterImage.original,
                CoverImage = Attributes.coverImage.original,
                EpisodeCount = Attributes.episodeCount,
                EpLength = Attributes.episodeLength,
                HumanReadableWatchtime = GetReadableWatchtime((string)Attributes.totalLength),
                VideoUrl = $"https://youtube.com/watch?v={Attributes.youtubeVideoId}"
            };

            return new FeatureRequest<Anime, Error.Anime>(Response, Error.Anime.None);
        }

        private static string GetReadableWatchtime(string minutes)
        {
            int Mins = int.Parse(minutes);
            TimeSpan span = TimeSpan.FromMinutes(Mins);
            return span.ToString(@"hh\hmm");
        }
    }
}