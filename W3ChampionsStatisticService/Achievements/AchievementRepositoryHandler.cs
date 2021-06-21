using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using W3ChampionsStatisticService.Achievements.Models;
using W3ChampionsStatisticService.CommonValueObjects;
using W3ChampionsStatisticService.Ladder;
using W3ChampionsStatisticService.PlayerStats.RaceOnMapVersusRaceStats;
using W3ChampionsStatisticService.PlayerStats.HeroStats;
using W3ChampionsStatisticService.PlayerProfiles;
using W3ChampionsStatisticService.Ports;
using W3ChampionsStatisticService.Matches;
using W3ChampionsStatisticService.ReadModelBase;
using W3ChampionsStatisticService.PadEvents;

namespace W3ChampionsStatisticService.Achievements {
    public class AchievementRepositoryHandler : IReadModelHandler  {

        private readonly IAchievementRepository _achievementRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IPlayerStatsRepository _playerStatsRepository;

        public AchievementRepositoryHandler(
            IAchievementRepository achievementRepository,
            IMatchRepository matchRepository,
            IPlayerRepository playerRepository,
            IPlayerStatsRepository playerStatsRepository) {
            _achievementRepository = achievementRepository;
            _matchRepository = matchRepository;    
            _playerRepository = playerRepository;
            _playerStatsRepository = playerStatsRepository;
        }

        public async Task Update(MatchFinishedEvent nextEvent) {
            //TODO: get this to work when match is finished....
             if (nextEvent == null || nextEvent.match == null || nextEvent.result == null) {
                 return;
             }
        } 

        public async Task<PlayerAchievements> GetPlayerAchievements(string playerId){
            var playerAchievements = await _achievementRepository.GetPlayerAchievements(playerId);
            if (playerAchievements == null){
                // check if the player exists....
                var playerProfile = await _playerRepository.LoadPlayerProfile(playerId);
                if (playerProfile != null){
                    var newPlayerAchievements = CreateNewPlayerAchievements(playerProfile);
                    // once saved, pass achievments out to be used -- can use playerAchievementsFound
                    // TODO
                }
            }
            return playerAchievements;
        }

        private List<int> ConvertSeasonsToSimpleList(List<Season> seasons) {
            var seasonList = new List<int>();
            foreach (Season s in seasons){seasonList.Add(s.Id);}
            seasonList.Reverse();
            return seasonList;
        }

        private List<Achievement> GenerateNewAchievementList() {
            var achievementList = new List<Achievement>();
            achievementList.Add(new MapWith25WinsAchievement());
            achievementList.Add(new Win10GamesWithATPartnerAchievement());
            return achievementList;
        }

        private async Task<PlayerAchievements> UpdateCurrentPlayerAchievements(PlayerAchievements playerAchievements, PlayerOverallStats playerOverallStats, bool isFirstUpdate){

            // currently working on the first run of getting achievements from previous games....
            var playerMatches = new List<Matchup>();
            var battleTag = playerAchievements.PlayerId;
            var playerRaceOnMapVersusRaceRatios = new List<PlayerRaceOnMapVersusRaceRatio>();

            // get the seasons in ints...
            var seasons = ConvertSeasonsToSimpleList(playerOverallStats.ParticipatedInSeasons);

            foreach(int s in seasons){
                var playerRaceOnMapVersusRaceRatio = await _playerStatsRepository.LoadMapAndRaceStat(battleTag, s);
                playerRaceOnMapVersusRaceRatios.Add(playerRaceOnMapVersusRaceRatio);
                var seasonalMatches = await _matchRepository.LoadFor(battleTag, null, GateWay.Undefined, GameMode.Undefined, 100, 0, s);

                foreach(Matchup matchup in seasonalMatches) {
                    playerMatches.Add(matchup);
                }
            }

// working here............ need to start on 2nd achievement.......
            foreach(Achievement achievement in playerAchievements.PlayerAchievementList) {
                var progressEnd = achievement.ProgressEnd;
                switch(achievement.Id){
                    case 0:
                        var mapWinsCount = new Dictionary<string,int>();
                        var firstMapTo25Wins = "";
                        foreach(Matchup matchup in playerMatches){
                            var map = matchup.Map;
                            var teams = matchup.Teams;
                            if(PlayerDidWin(battleTag, teams)) {
                                var hitWinsLimit = AddMapToMapWinsCount(mapWinsCount, map, 25);
                                if(achievement.ProgressCurrent < progressEnd){
                                    achievement.ProgressCurrent = CheckMostMapWins(mapWinsCount);
                                }
                                if (hitWinsLimit){firstMapTo25Wins = map;}
                            }

                        }
                        if(firstMapTo25Wins != ""){
                            achievement.Caption = $"Player has completed this achievement with 25 games won on {firstMapTo25Wins}";
                            achievement.Completed = true;
                        }
                        break;
                }
            }

            return playerAchievements;
        }

        private long CheckMostMapWins(Dictionary<string,int> mapWinsCount){
            long maxValue = 0;
            foreach(var mapWins in mapWinsCount){
                if(mapWins.Value > maxValue){ maxValue = mapWins.Value;}
            }
            return maxValue;
        }

        private bool AddMapToMapWinsCount(Dictionary<string,int> mapWinsCount, string map, int maxCount) {
            var didReachMaxCount = false;
            if(!mapWinsCount.ContainsKey(map)) {
                mapWinsCount.Add(map, 1);
            } else {
                mapWinsCount[map] += 1;
                if (mapWinsCount[map] == maxCount) {
                    didReachMaxCount = true;
                }
            }
            return didReachMaxCount;
        }

        private bool PlayerDidWin(string battleTag, IList<Team> teams) {
            foreach(Team team in teams) {
                var players = team.Players;
                foreach(PlayerOverviewMatches player in players){
                    var playerName = player.BattleTag;
                    if (playerName == battleTag){return player.Won;}
                }
            }
            return false;
        }

        private async Task<PlayerAchievements> CreateNewPlayerAchievements(PlayerOverallStats playerOverallStats) {
            var newPlayerAchievements = new PlayerAchievements();
            newPlayerAchievements.PlayerId = playerOverallStats.BattleTag;
            newPlayerAchievements.PlayerAchievementList = GenerateNewAchievementList();
            newPlayerAchievements = await UpdateCurrentPlayerAchievements(newPlayerAchievements, playerOverallStats, true);
            return newPlayerAchievements;
        }
    }
}
