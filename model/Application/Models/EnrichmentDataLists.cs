using System.Collections.Generic;
using System.Linq;
using Domain.Schema.GNProgramSchema;
using log4net;

namespace Application.Models
{
    public class EnrichmentDataLists
    {
        /// <summary>
        ///     Initialize Log4net
        /// </summary>
        //TODO: move mapping logic somewhere else
        private static readonly ILog Log = LogManager.GetLogger(typeof(EnrichmentDataLists));

        public List<GnApiProgramsSchema.titleDescType> ProgramTitles { get; private set; }

        public List<GnApiProgramsSchema.titleDescType> ProgramDescriptions { get; private set; }

        public List<GnApiProgramsSchema.castTypeMember> CastMembers { get; private set; }

        public List<GnApiProgramsSchema.crewTypeMember> CrewMembers { get; private set; }

        public List<GnApiProgramsSchema.programsProgramGenre> GenresList { get; private set; }

        public List<GnApiProgramsSchema.assetType> ProgramAssets { get; private set; }

        private List<GnApiProgramsSchema.externalLinksTypeExternalLink> ExternalLinks { get; set; }

        public void UpdateListData(GnApiProgramsSchema.programsProgram apiData, string seasonId)
        {
            //Build Data Lists
            //Asset List
            AddProgramAssetsToList(apiData?.assets, "Layer1");
            //Cast List
            AddCastMembersToList(apiData?.cast, "Layer1");
            //Crew List
            AddCrewMembersToList(apiData?.crew, "Layer1");
            //titles
            AddProgramTitlesToList(apiData?.titles, "Layer1");
            //genres
            AddGenresToList(apiData?.genres, "Layer1)");
            //external Links
            AddExternalLinksToList(apiData?.externalLinks);
            //
            var seasonData = apiData?.seasons?.FirstOrDefault(s => s.seasonId == seasonId);

            if (seasonData == null)
            {
                return;
            }

            //Season Asset List
            AddProgramAssetsToList(seasonData.assets, "Layer2");
            //Season Cast List
            AddCastMembersToList(seasonData.cast, "Layer2");
            //Season Crew List
            AddCrewMembersToList(seasonData.crew, "Layer2");
        }

        public void AddProgramTitlesToList(GnApiProgramsSchema.programsProgramTitles programTitles, string apiLevel,
            bool updateTracker = false)
        {
            if (programTitles.title != null && programTitles.title.Any())
            {
                if (ProgramTitles == null)
                {
                    ProgramTitles = new List<GnApiProgramsSchema.titleDescType>();
                }

                if (!updateTracker)
                {
                    Log.Debug($"Number of Program Titles at {apiLevel} Level: {programTitles.title.Count()}");
                }

                foreach (var title in programTitles.title.ToList())
                {
                    ProgramTitles.Add(title);
                }
            }
            else
            {
                if (!updateTracker)
                {
                    Log.Warn($"Title info is currently null at the current api level: {apiLevel}, " +
                             "will continue and check next api results for Title data");
                }
            }
        }

        public void AddProgramDescriptionsToList(GnApiProgramsSchema.programsProgramDescriptions programDescriptions,
            string apiLevel, bool updateTracker = false)
        {
            if (programDescriptions.desc != null && programDescriptions.desc.Any())
            {
                if (ProgramDescriptions == null)
                {
                    ProgramDescriptions = new List<GnApiProgramsSchema.titleDescType>();
                }

                if (!updateTracker)
                {
                    Log.Debug(
                        $"Number of Program Descriptions at {apiLevel} Level: {programDescriptions.desc.Count()}");
                }

                foreach (var description in programDescriptions.desc)
                {
                    ProgramDescriptions.Add(description);
                }
            }
            else
            {
                if (!updateTracker)
                {
                    Log.Warn($"Program Descriptions are currently null at the current api level: {apiLevel}, " +
                             "will continue and check next api results for Description data");
                }
            }
        }

        public void AddCastMembersToList(IEnumerable<GnApiProgramsSchema.castTypeMember> castList, string apiLevel,
            bool updateTracker = false)
        {
            var castTypeMembers = castList.ToList();
            if (castTypeMembers.Any())
            {
                if (CastMembers == null)
                {
                    CastMembers = new List<GnApiProgramsSchema.castTypeMember>();
                }

                if (!updateTracker)
                {
                    Log.Debug($"Number of Cast Members at {apiLevel} Level: {castTypeMembers.Count()}");
                }

                foreach (var cast in castTypeMembers)
                {
                    CastMembers.Add(cast);
                }
            }
            else
            {
                if (!updateTracker)
                {
                    Log.Warn($"Cast info is currently null at the current api level: {apiLevel}, " +
                             "will continue and check next api results for Cast data");
                }
            }
        }

        public void AddCrewMembersToList(IEnumerable<GnApiProgramsSchema.crewTypeMember> crewList, string apiLevel,
            bool updateTracker = false)
        {
            var crewTypeMembers = crewList.ToList();
            if (crewTypeMembers.Any())
            {
                if (CrewMembers == null)
                {
                    CrewMembers = new List<GnApiProgramsSchema.crewTypeMember>();
                }

                if (!updateTracker)
                {
                    Log.Debug($"Number of Crew Members at {apiLevel} Level: {crewTypeMembers.Count()}");
                }

                foreach (var crew in crewTypeMembers)
                {
                    CrewMembers.Add(crew);
                }
            }
            else
            {
                if (!updateTracker)
                {
                    Log.Warn($"Crew info is currently null at the current api level: {apiLevel}, " +
                             "will continue and check next api results for Crew data");
                }
            }
        }

        public void AddGenresToList(IEnumerable<GnApiProgramsSchema.programsProgramGenre> genres, string apiLevel,
            bool updateTracker = false)
        {
            var programsProgramGenres = genres.ToList();
            if (programsProgramGenres.Any())
            {
                if (GenresList == null)
                {
                    GenresList = new List<GnApiProgramsSchema.programsProgramGenre>();
                }

                if (!updateTracker)
                {
                    Log.Debug($"Number of Genres at {apiLevel} Level: {programsProgramGenres.Count()}");
                }

                foreach (var genre in programsProgramGenres)
                {
                    GenresList.Add(genre);
                }
            }
            else
            {
                if (!updateTracker)
                {
                    Log.Warn($"Genre info is currently null at the current api level: {apiLevel}, " +
                             "will continue and check next api results for genre data");
                }
            }
        }


        public void AddProgramAssetsToList(IEnumerable<GnApiProgramsSchema.assetType> programsList, string apiLevel,
            bool updateTracker = false)
        {
            var assetTypes = programsList.ToList();

            if (assetTypes.Any())
            {
                if (ProgramAssets == null)
                {
                    ProgramAssets = new List<GnApiProgramsSchema.assetType>();
                }

                if (!updateTracker)
                {
                    Log.Debug($"Number of Assets at {apiLevel} Level: {assetTypes.Count()}");
                }

                foreach (var item in assetTypes)
                {
                    ProgramAssets.Add(item);
                }
            }
            else
            {
                if (!updateTracker)
                {
                    Log.Warn($"Asset is currently null at the current api level: {apiLevel}, " +
                             "will continue and check next api results for Cast data");
                }
            }
        }


        public void AddExternalLinksToList(IEnumerable<GnApiProgramsSchema.externalLinksTypeExternalLink> externalLinks,
            bool updateTracker = false)
        {
            var externalLinksTypeExternalLinks = externalLinks.ToList();

            if (externalLinksTypeExternalLinks.Any())
            {
                if (!updateTracker)
                {
                    Log.Info("Asset has External Links, Storing IMDB Data.");
                }

                if (ExternalLinks == null)
                {
                    ExternalLinks = new List<GnApiProgramsSchema.externalLinksTypeExternalLink>();
                }

                foreach (var link in externalLinksTypeExternalLinks)
                {
                    ExternalLinks.Add(link);
                }
            }
            else
            {
                if (!updateTracker)
                {
                    Log.Warn("No Imdb Data available for the current package.");
                }
            }
        }
    }
}