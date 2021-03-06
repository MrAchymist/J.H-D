﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Net.Http;

using J.H_D.Tools;
using J.H_D.Data;

namespace J.H_D.Minions.Infos
{
    public static class MovieMinion
    {
        static HttpClient Asker = JHConfig.Asker;

        public enum SearchType
        {
            Movie,
            Serie,
            Director
        }

        private static bool ContainNullCheck(string s1, string s2)
        {
            if (s1 == null)
                return false;
            return s1.Contains(s2, StringComparison.OrdinalIgnoreCase);
        }

        private static readonly Dictionary<SearchType, string> EndpointList = new Dictionary<SearchType, string>
        {
            {SearchType.Movie, "https://api.themoviedb.org/3/search/movie?api_key=" },
            {SearchType.Serie, "https://api.themoviedb.org/3/search/tv?api_key=" },
            {SearchType.Director, "https://api.themoviedb.org/3/search/person?api_key=" }
        };

        public static async Task<FeatureRequest<Response.Movie, Error.Movie>> SearchMovieAsync(string[] args)
        {
            string RequestName = Utilities.MakeQueryArgs(args);
            if (RequestName.Length == 0) {
                return new FeatureRequest<Response.Movie, Error.Movie>(null, Error.Movie.Help);
            }

            dynamic Json;
            Json = JsonConvert.DeserializeObject(await Asker.GetStringAsync($"{EndpointList[SearchType.Movie]}{JHConfig.APIKey["Tmdb"]}&language=en-US&query={RequestName}&page=1"));

            if (Json["total_results"] == "0") {
                return new FeatureRequest<Response.Movie, Error.Movie>(null, Error.Movie.NotFound);
            }

            JArray Results = (JArray)Json["results"];
            dynamic FinalData = Results[0];
            return new FeatureRequest<Response.Movie, Error.Movie>(new Response.Movie
            {
                Name = FinalData.original_title,
                PosterPath = FinalData.poster_path,
                Adult = FinalData.adult,
                Overview = FinalData.overview,
                ReleaseDate = FinalData.release_date,
                Id = FinalData.id,
                OriginalTitle = FinalData.original_title,
                OriginalLanguage = FinalData.original_language,
                BackdropPath = FinalData.backdrop_path,
                AverageNote = FinalData.vote_average,
            }, Error.Movie.None);
        }

        public static async Task<FeatureRequest<Response.TVSeries, Error.Movie>> GetSeriesGeneralInfosAsync(SearchType searchType, string[] Args)
        {
            string RequestName = Utilities.MakeQueryArgs(Args);
            if (RequestName.Length == 0) {
                return new FeatureRequest<Response.TVSeries, Error.Movie>(null, Error.Movie.Help);
            }

            dynamic SeriesInfos = JsonConvert.DeserializeObject(await Asker.GetStringAsync($"{EndpointList[searchType]}{JHConfig.APIKey["Tmdb"]}&language=en-US&query={RequestName}&page=1"));
            if (SeriesInfos["total_results"] == "0") {
                return new FeatureRequest<Response.TVSeries, Error.Movie>(null, Error.Movie.NotFound); 
            }

            JArray Results = (JArray)SeriesInfos["results"];
            dynamic SerieResult = Results[0];

            dynamic DetailsJson = JsonConvert.DeserializeObject(await Asker.GetStringAsync($"https://api.themoviedb.org/3/tv/{SerieResult.id}?api_key={JHConfig.APIKey["Tmdb"]}&language=en-US"));

            List<Response.TVSeason> Seasons = new List<Response.TVSeason>();
            
            foreach (dynamic season in DetailsJson.seasons)
            {
                Seasons.Add(new Response.TVSeason
                {
                    EpisodeNumber = season.episode_count,
                    Id = season.id,
                    SName = season.name,
                    Overview = season.overview,
                    PosterPath = season.poster_path,
                    SNumber = season.season_number
                });
            }

            return new FeatureRequest<Response.TVSeries, Error.Movie>(new Response.TVSeries
            {
                BackdropPath = DetailsJson.backdrop_path,
                EpisodeNumber = DetailsJson.number_of_episodes,
                Started = DetailsJson.first_air_date,
                InProduction = (string)DetailsJson.status == "Ended",
                HomePage = DetailsJson.homepage,
                SeasonNumber = DetailsJson.number_of_seasons,
                EpisodeRunTime = DetailsJson.episode_run_time[0],
                Genres = GetNames(DetailsJson.genres),
                Compagnies = GetNames(DetailsJson.production_compagnies),
                VoteAverage = DetailsJson.vote_average,
                SeriesName = DetailsJson.name,
                SeriesId = DetailsJson.id,
                Overview = DetailsJson.overview,
                Seasons = Seasons
            }, Error.Movie.None);
        }

        public static async Task<FeatureRequest<Response.Movie, Error.Movie>> BonusInfosAsync(SearchType searchType, string[] args)
        {
            string RequestName = Utilities.MakeQueryArgs(args);
            if (RequestName.Length == 0)
                return new FeatureRequest<Response.Movie, Error.Movie>(null, Error.Movie.Help);
            
            dynamic Moviejson;

            Moviejson = JsonConvert.DeserializeObject(await Asker.GetStringAsync($"{EndpointList[SearchType.Movie]}{JHConfig.APIKey["Tmdb"]}&lanuage=en-US&query={RequestName}"));

            if (Moviejson["total_results"] == "0") {
                return new FeatureRequest<Response.Movie, Error.Movie>(null, Error.Movie.NotFound);
            }

            JArray Results = (JArray)Moviejson["results"];
            dynamic MovieResults = Results[0];
            dynamic DetailsJson;

            DetailsJson = JsonConvert.DeserializeObject(await Asker.GetStringAsync($"https://api.themoviedb.org/3/movie/{MovieResults.id}?api_key={JHConfig.APIKey["Tmdb"]}&language=en-US"));

            return new FeatureRequest<Response.Movie, Error.Movie>(new Response.Movie
            {
                Name = DetailsJson.original_title,
                PosterPath = DetailsJson.poster_path,
                Adult = DetailsJson.adult,
                Overview = DetailsJson.overview,
                ReleaseDate = DetailsJson.release_date,
                Id = DetailsJson.id,
                OriginalTitle = DetailsJson.original_title,
                OriginalLanguage = DetailsJson.original_language,
                BackdropPath = DetailsJson.backdrop_path,
                AverageNote = DetailsJson.vote_average,
                Budget = DetailsJson.budget,
                Revenue = DetailsJson.revenue,
                ProductionCompanies = GetNames(DetailsJson.production_companies),
                Genres = GetNames(DetailsJson.genres),
                Runtime = GetRuntime((int)DetailsJson.runtime)
            }, Error.Movie.None);
        }

        private static string GetRuntime(int minutes)
        {
            TimeSpan span = TimeSpan.FromMinutes(minutes);
            return span.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        }

        private static List<string> GetNames(dynamic DynamicArray)
        {
            List<string> results = new List<string>();
            if (DynamicArray == null) {
                return new List<string>();
            }

            foreach (dynamic d in DynamicArray) {
                results.Add((string)d.name);
            }

            return results;
        }
    }
}
