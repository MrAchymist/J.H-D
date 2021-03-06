﻿using BooruSharp.Booru;
using BooruSharp.Search.Post;
using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using J.H_D.Data;
using BooruSharp.Search;

namespace J.H_D.Minions.NSFW
{
    public static class BooruMinion
    {
        public enum BooruType
        {
            Danbooru,
            E621,
            E926,
            Gelbooru,
            Konachan,
            Realbooru,
            R34,
            Safebooru,
            Sakugabooru,
            SankakuComplex,
            Yandere
        }

        public static readonly ImmutableDictionary<BooruType, Type> WebsiteEndpoints = new Dictionary<BooruType, Type>
        {
            {BooruType.Danbooru, typeof(DanbooruDonmai) },
            {BooruType.E621, typeof(E621) },
            {BooruType.E926, typeof(E926) },
            {BooruType.Gelbooru, typeof(Gelbooru) },
            {BooruType.Konachan, typeof(Konachan) },
            {BooruType.Realbooru, typeof(Realbooru) },
            {BooruType.R34, typeof(Rule34) },
            {BooruType.SankakuComplex, typeof(SankakuComplex) },
            {BooruType.Yandere, typeof(Yandere) }
        }.ToImmutableDictionary();

        public struct BooruOptions : IEquatable<BooruOptions>
        {
            public bool AllowNsfw { get; private set; }
            public BooruType Booru { get; set; }
            public string[] SearchQuery { get; set; }

            public BooruOptions(BooruType Website, string[] Search, bool Allow)
            {
                Booru = Website;
                SearchQuery = Search;
                AllowNsfw = Allow;
            }

            public bool Equals(BooruOptions other)
            {
                return
                    Booru == other.Booru &&
                    SearchQuery == other.SearchQuery &&
                    AllowNsfw == other.AllowNsfw;
            }
        }

        public static async Task<FeatureRequest<SearchResult, Error.Booru>> GetBooruImageAsync(BooruOptions options)
        {
            Type Booru = WebsiteEndpoints[options.Booru];
            var BooruSearch = (ABooru)Activator.CreateInstance(Booru);
            SearchResult Result;

            if (!options.AllowNsfw) {
                options.SearchQuery.Append("Safe");
            }

            try
            {
                Result = await BooruSearch.GetRandomPostAsync(options.SearchQuery);
            }
            catch(Exception e) when (e is InvalidTags)
            {
                Console.Error.Write(e.Message);
                return new FeatureRequest<SearchResult, Error.Booru>(new SearchResult(), Error.Booru.NotFound);
            }

            if (Result.fileUrl == null) {
                return new FeatureRequest<SearchResult, Error.Booru>(Result, Error.Booru.NotFound);
            }

            return new FeatureRequest<SearchResult, Error.Booru>(Result, Error.Booru.None);
        }

        public static async Task<FeatureRequest<BooruSharp.Search.Tag.SearchResult, Error.Booru>> GetTagAsync(BooruType Booru, string Id)
        {
            Type BType = WebsiteEndpoints[Booru];
            var BooruWebsite = (ABooru)Activator.CreateInstance(BType);

            var TagResult = await BooruWebsite.GetTagAsync(Id);
            return new FeatureRequest<BooruSharp.Search.Tag.SearchResult, Error.Booru>(TagResult, Error.Booru.None);
        }

        public static async Task<FeatureRequest<List<BooruSharp.Search.Tag.SearchResult>, Error.Booru>> GetTagsAsync(BooruType Booru, string[] Tags, 
            BooruSharp.Search.Tag.TagType OnlyType)
        {
            bool StandardList = OnlyType == BooruSharp.Search.Tag.TagType.Metadata;

            Type BType = WebsiteEndpoints[Booru];
            var BooruWebsite = (ABooru)Activator.CreateInstance(BType);

            List<BooruSharp.Search.Tag.SearchResult> FoundTags = new List<BooruSharp.Search.Tag.SearchResult>();
            foreach (string Tag in Tags) {
                FoundTags.Add(await BooruWebsite.GetTagAsync(Tag));
            }

            return StandardList ?
                new FeatureRequest<List<BooruSharp.Search.Tag.SearchResult>, Error.Booru>(FoundTags, Error.Booru.None) :
                new FeatureRequest<List<BooruSharp.Search.Tag.SearchResult>, Error.Booru>(
                    FoundTags.Where(x => x.type == OnlyType).ToList(),
                    Error.Booru.None);
        }
    }
}
