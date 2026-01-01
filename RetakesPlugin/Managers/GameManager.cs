using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Models;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Managers;

public class GameManager
{
    private readonly RetakesPlugin _plugin;
    private Dictionary<int, PlayerScore> _playerScores = new();
    public readonly QueueManager QueueManager;
    private readonly int _consecutiveRoundWinsToScramble;
    private readonly bool _isScrambleEnabled;
    private readonly bool _removeSpectatorsEnabled;
    private readonly bool _isBalanceEnabled;

    public const int ScoreForKill = 50;
    public const int ScoreForAssist = 25;
    public const int ScoreForDefuse = 50;

    public GameManager(RetakesPlugin plugin, QueueManager queueManager, int? roundsToScramble, bool? isScrambleEnabled, bool? removeSpectatorsEnabled, bool? isBalanceEnabled)
    {
        _plugin = plugin;
        QueueManager = queueManager;
        _consecutiveRoundWinsToScramble = roundsToScramble ?? 5;
        _isScrambleEnabled = isScrambleEnabled ?? true;
        _removeSpectatorsEnabled = removeSpectatorsEnabled ?? false;
        _isBalanceEnabled = isBalanceEnabled ?? true;

        Logger.LogInfo("GameManager", "Game manager initialized");
    }

    private bool _scrambleNextRound;

    public void ScrambleNextRound(CCSPlayerController? admin = null)
    {
        _scrambleNextRound = true;
        var message = $"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.teams.admin_scramble", admin?.PlayerName ?? "The server owner"]}";
        Server.PrintToChatAll(message);
        Logger.LogInfo("GameManager", $"Teams will be scrambled next round by {admin?.PlayerName ?? "server"}");
    }

    private void ScrambleTeams()
    {
        _scrambleNextRound = false;
        _consecutiveRoundsWon = 0;

        var shuffledActivePlayers = PlayerHelper.Shuffle(QueueManager.ActivePlayers.ToList(), new Random());

        var newTerrorists = shuffledActivePlayers.Take(QueueManager.GetTargetNumTerrorists()).ToList();
        var newCounterTerrorists = shuffledActivePlayers.Except(newTerrorists).ToList();

        SetTeams(newTerrorists, newCounterTerrorists);
        Logger.LogInfo("GameManager", "Teams scrambled");
    }

    public void ResetPlayerScores()
    {
        _playerScores = new Dictionary<int, PlayerScore>();
        Logger.LogDebug("GameManager", "Player scores reset");
    }

    public void AddScore(CCSPlayerController player, int score)
    {
        if (!PlayerHelper.IsValid(player) || player.UserId == null)
        {
            return;
        }

        var playerId = (int)player.UserId;

        if (!_playerScores.ContainsKey(playerId))
        {
            _playerScores[playerId] = new PlayerScore
            {
                UserId = playerId,
                PlayerName = player.PlayerName
            };
        }

        _playerScores[playerId].AddScore(score);
        Logger.LogDebug("GameManager", $"{player.PlayerName} scored {score} points (Total: {_playerScores[playerId].Score})");
    }

    public void AddKill(CCSPlayerController player)
    {
        if (!PlayerHelper.IsValid(player) || player.UserId == null) return;

        var playerId = (int)player.UserId;
        if (!_playerScores.ContainsKey(playerId))
        {
            _playerScores[playerId] = new PlayerScore
            {
                UserId = playerId,
                PlayerName = player.PlayerName
            };
        }

        _playerScores[playerId].AddKill();
    }

    public void AddAssist(CCSPlayerController player)
    {
        if (!PlayerHelper.IsValid(player) || player.UserId == null) return;

        var playerId = (int)player.UserId;
        if (!_playerScores.ContainsKey(playerId))
        {
            _playerScores[playerId] = new PlayerScore
            {
                UserId = playerId,
                PlayerName = player.PlayerName
            };
        }

        _playerScores[playerId].AddAssist();
    }

    public void AddDefuse(CCSPlayerController player)
    {
        if (!PlayerHelper.IsValid(player) || player.UserId == null) return;

        var playerId = (int)player.UserId;
        if (!_playerScores.ContainsKey(playerId))
        {
            _playerScores[playerId] = new PlayerScore
            {
                UserId = playerId,
                PlayerName = player.PlayerName
            };
        }

        _playerScores[playerId].AddDefuse();
    }

    private int _consecutiveRoundsWon;

    private void TerroristRoundWin()
    {
        _consecutiveRoundsWon++;

        var shouldScrambleNow = _isScrambleEnabled && _consecutiveRoundsWon == _consecutiveRoundWinsToScramble;
        var roundsLeftToScramble = _consecutiveRoundWinsToScramble - _consecutiveRoundsWon;
        var shouldAlmostScramble = _isScrambleEnabled && roundsLeftToScramble > 0 && roundsLeftToScramble <= 2;

        if (shouldScrambleNow)
        {
            var message = $"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.teams.scramble", _consecutiveRoundWinsToScramble]}";
            Server.PrintToChatAll(message);
            Logger.LogInfo("GameManager", $"Scrambling teams after {_consecutiveRoundWinsToScramble} T wins");
            ScrambleTeams();
        }
        else if (shouldAlmostScramble)
        {
            var message = $"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.teams.almost_scramble", _consecutiveRoundsWon, roundsLeftToScramble]}";
            Server.PrintToChatAll(message);
        }
        else if (_consecutiveRoundsWon >= 3)
        {
            var message = $"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.teams.win_streak", _consecutiveRoundsWon]}";
            Server.PrintToChatAll(message);
        }
    }

    private void CounterTerroristRoundWin()
    {
        if (_consecutiveRoundsWon >= 3)
        {
            var message = $"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.teams.win_streak_over", _consecutiveRoundsWon]}";
            Server.PrintToChatAll(message);
            Logger.LogInfo("GameManager", $"CT broke {_consecutiveRoundsWon} round win streak");
        }

        _consecutiveRoundsWon = 0;

        var targetNumTerrorists = QueueManager.GetTargetNumTerrorists();
        var sortedCounterTerroristPlayers = GetSortedActivePlayers(CsTeam.CounterTerrorist);

        var newTerrorists = sortedCounterTerroristPlayers
            .Where(p => GetPlayerScore(p) > 0)
            .Take(targetNumTerrorists)
            .ToList();

        if (newTerrorists.Count < targetNumTerrorists)
        {
            var playersLeft = PlayerHelper.Shuffle(
                sortedCounterTerroristPlayers.Except(newTerrorists).ToList(),
                new Random()
            );
            newTerrorists.AddRange(playersLeft.Take(targetNumTerrorists - newTerrorists.Count));
        }

        if (newTerrorists.Count < targetNumTerrorists)
        {
            newTerrorists.AddRange(
                GetSortedActivePlayers(CsTeam.Terrorist)
                    .Take(targetNumTerrorists - newTerrorists.Count)
            );
        }

        var newCounterTerrorists = QueueManager.ActivePlayers.Except(newTerrorists).ToList();

        SetTeams(newTerrorists, newCounterTerrorists);
    }

    private void BalanceTeams()
    {
        List<CCSPlayerController> newTerrorists = [];
        List<CCSPlayerController> newCounterTerrorists = [];

        var currentNumTerrorist = PlayerHelper.GetPlayerCount(CsTeam.Terrorist);
        var numTerroristsNeeded = QueueManager.GetTargetNumTerrorists() - currentNumTerrorist;
        if (numTerroristsNeeded > 0)
        {
            var sortedCounterTerroristPlayers = GetSortedActivePlayers(CsTeam.CounterTerrorist);

            newTerrorists = sortedCounterTerroristPlayers
                .Where(p => GetPlayerScore(p) > 0)
                .Take(numTerroristsNeeded)
                .ToList();

            if (newTerrorists.Count < numTerroristsNeeded)
            {
                var playersLeft = PlayerHelper.Shuffle(
                    sortedCounterTerroristPlayers.Except(newTerrorists).ToList(),
                    new Random()
                );
                newTerrorists.AddRange(playersLeft.Take(numTerroristsNeeded - newTerrorists.Count));
            }
        }

        var currentNumCounterTerroristAfterBalance = PlayerHelper.GetPlayerCount(CsTeam.CounterTerrorist);
        var numCounterTerroristsNeeded = QueueManager.GetTargetNumCounterTerrorists() - currentNumCounterTerroristAfterBalance;

        if (numCounterTerroristsNeeded > 0)
        {
            var terroristsWithZeroScore = QueueManager.ActivePlayers
                .Where(player => PlayerHelper.IsValid(player) && player.Team == CsTeam.Terrorist && GetPlayerScore(player) == 0)
                .Except(newTerrorists)
                .ToList();

            newCounterTerrorists = PlayerHelper.Shuffle(terroristsWithZeroScore, new Random())
                .Take(numCounterTerroristsNeeded)
                .ToList();

            if (numCounterTerroristsNeeded > newCounterTerrorists.Count)
            {
                newCounterTerrorists.AddRange(
                    QueueManager.ActivePlayers
                        .Except(newCounterTerrorists)
                        .Except(newTerrorists)
                        .Where(player => PlayerHelper.IsValid(player) && player.Team == CsTeam.Terrorist)
                        .OrderBy(GetPlayerScore)
                        .Take(numTerroristsNeeded - newCounterTerrorists.Count)
                        .ToList()
                );
            }
        }

        SetTeams(newTerrorists, newCounterTerrorists);
    }

    public void OnRoundPreStart(CsTeam winningTeam)
    {
        Logger.LogDebug("GameManager", $"Round pre-start. Winning team: {winningTeam}");

        switch (winningTeam)
        {
            case CsTeam.CounterTerrorist:
                if (_isBalanceEnabled)
                {
                    CounterTerroristRoundWin();
                }
                break;

            case CsTeam.Terrorist:
                TerroristRoundWin();
                break;
        }

        if (_scrambleNextRound)
        {
            ScrambleTeams();
        }

        if (_isBalanceEnabled)
        {
            BalanceTeams();
        }
    }

    private List<CCSPlayerController> GetSortedActivePlayers(CsTeam? team = null)
    {
        return QueueManager.ActivePlayers
            .Where(PlayerHelper.IsValid)
            .Where(player => team == null || player.Team == team)
            .OrderByDescending(GetPlayerScore)
            .ToList();
    }

    private int GetPlayerScore(CCSPlayerController player)
    {
        if (player.UserId == null) return 0;
        return _playerScores.TryGetValue((int)player.UserId, out var score) ? score.Score : 0;
    }

    private void SetTeams(List<CCSPlayerController>? terrorists, List<CCSPlayerController>? counterTerrorists)
    {
        terrorists ??= [];
        counterTerrorists ??= [];

        foreach (var player in QueueManager.ActivePlayers.Where(PlayerHelper.IsValid))
        {
            if (terrorists.Contains(player))
            {
                player.SwitchTeam(CsTeam.Terrorist);
            }
            else if (counterTerrorists.Contains(player))
            {
                player.SwitchTeam(CsTeam.CounterTerrorist);
            }
        }

        Logger.LogDebug("GameManager", $"Teams set: {terrorists.Count} T, {counterTerrorists.Count} CT");
    }

    public HookResult RemoveSpectators(EventPlayerTeam @event, HashSet<CCSPlayerController> hasMutedVoices)
    {
        if (!_removeSpectatorsEnabled) return HookResult.Continue;

        CCSPlayerController? player = @event.Userid;
        if (!PlayerHelper.IsValid(player))
        {
            return HookResult.Continue;
        }

        int team = @event.Team;
        if (team == (int)CsTeam.Spectator)
        {
            if (QueueManager.ActivePlayers.Contains(player))
            {
                QueueManager.RemovePlayerFromQueues(player);
                hasMutedVoices.Remove(player);
                Logger.LogInfo("GameManager", $"{player.PlayerName} moved to spectator and removed from queues");
            }
        }

        return HookResult.Continue;
    }
}