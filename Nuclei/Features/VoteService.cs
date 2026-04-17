using System;
using System.Collections.Generic;
using System.Timers;
using BepInEx;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using Nuclei.CritzOS.Features;
using Nuclei.Helpers;
using UnityEngine;

namespace Nuclei.Features;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class VoteService
{
    internal static VoteSession? ActiveVote;

    public static bool CanStartVote()
    {
        return ActiveVote == null;
    }

    /// <summary>
    /// start a vote-kick session for target player
    /// </summary>
    /// <param name="initiator"></param>
    /// <param name="action"></param>
    /// <param name="cancelIfMissionChanges"></param>
    /// <param name="thresholdByFullServer"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public static void StartVote(Player initiator, Action action, bool cancelIfMissionChanges, bool thresholdByFullServer = true, string? reason = null)
    {
        ActiveVote = new VoteSession(initiator, action, cancelIfMissionChanges, thresholdByFullServer, reason);
        ActiveVote.Start();
    }

    /// <summary>
    /// handles a vote from the vote command
    /// </summary>
    /// <param name="voter"></param>
    /// <param name="votedYes"></param>
    public static void HandleVote(Player voter, bool votedYes)
    {
        if (ActiveVote == null)
        {
            ChatService.SendPrivateChatMessage($"A vote session has not been started, use a vote command to start one.", voter);
        }
        else ActiveVote.AddVote(voter, votedYes);
    }

    public static void StopVoteKick()
    {
        ActiveVote = null;
    }
}

// Usually only used by VoteService
public class VoteSession
{
    private readonly Player _initiator;
    private readonly Timer _timer;
    private readonly HashSet<ulong> _yesVoters;
    private readonly HashSet<ulong> _noVoters;
    private int _timeLeft;
    private readonly int _voteThreshold; // don't want threshold changing as players leave or join
    private readonly string? _reason;
    
    // If true, vote will pass ONLY IF it reaches threshold.
    // If false, vote will pass if it reaches threshold OR runs out of time and YES votes is greater than NO votes
    private readonly bool _thresholdByFullServer;

    public bool CancelIfMissionChanges { get; }

    private float _previousTimeSinceLevelLoad;

    // Function to call when vote succeeds
    private readonly Action _action;

    private static readonly int DefaultVotingWindow = NucleiConfig.KickTimeout!.Value; 

    public VoteSession(Player initiator, Action action, bool cancelIfMissionChanges, bool thresholdByFullServer = true, string? reason = null)
    {
        _initiator = initiator;
        _voteThreshold = VoteThreshold();
        _timeLeft = DefaultVotingWindow;
        _timer = new Timer(1000);
        _timer.Elapsed += OnTimerTick;
        _yesVoters = [];
        _noVoters = [];
        _action = action;
        _thresholdByFullServer = thresholdByFullServer;
        CancelIfMissionChanges = cancelIfMissionChanges;
        _reason = reason;
    }

    public void Start()
    {
        ChatService.SendChatMessage($"Type '{NucleiConfig.CommandPrefixChar}y' to vote yes, '{NucleiConfig.CommandPrefixChar}n' to vote no. You have {_timeLeft} seconds to cast your vote. ({_yesVoters.Count}/{_voteThreshold} YES votes, {_noVoters.Count}/{_voteThreshold} NO votes).");
        if (!_reason.IsNullOrWhiteSpace())
            ChatService.SendChatMessage($"Reason: {_reason}");
        _timer.Start();
        AddVote(_initiator, true);
    }

    /// <summary>
    /// Will add a vote to the vote kick if the player is not already in the hashset.
    /// </summary>
    /// <param name="voter"></param>
    /// <param name="votedYes"></param>
    public void AddVote(Player voter, bool votedYes)
    {
        if (votedYes)
        {
            if (_yesVoters.Add(voter.SteamID))
            {
                ChatService.SendChatMessage(
                    $"{voter.PlayerName} has voted. ({_yesVoters.Count}/{_voteThreshold} YES votes, {_noVoters.Count}/{_voteThreshold} NO votes).");

                if (_yesVoters.Count >= _voteThreshold)
                {
                    ChatService.SendChatMessage("YES votes have reached a majority.");
                    FinaliseVote(true);
                }
            }
            else
            {
                ChatService.SendPrivateChatMessage("You have already voted.", voter);
            }
        }
        else
        {
            if (_noVoters.Add(voter.SteamID))
            {
                ChatService.SendChatMessage(
                    $"{voter.PlayerName} has voted. ({_yesVoters.Count}/{_voteThreshold} YES votes, {_noVoters.Count}/{_voteThreshold} NO votes).");

                if (_noVoters.Count >= _voteThreshold)
                {
                    ChatService.SendChatMessage("NO votes have reached a majority.");
                    FinaliseVote(false);
                }
            }
            else
            {
                ChatService.SendPrivateChatMessage("You have already voted.", voter);
            }
        }
    }

    /// <summary>
    /// Callback that is called every timer tick which is set to 1 second (1000)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnTimerTick(object sender, ElapsedEventArgs e)
    {
        _timeLeft--;

        if ((_timeLeft % 10 == 0 && _timeLeft > 0) || _timeLeft < 10) // every ten seconds or below 10 seconds every tick
        {
            ChatService.SendChatMessage($"Vote ends in {_timeLeft} seconds. Type `{NucleiConfig.CommandPrefixChar}y` to vote YES, '{NucleiConfig.CommandPrefixChar}n' for NO. ({_yesVoters.Count}/{_voteThreshold} YES votes, {_noVoters.Count}/{_voteThreshold} NO votes).");
            if (!_reason.IsNullOrWhiteSpace())
                ChatService.SendChatMessage($"Reason: {_reason}");
        }
        
        if (_timeLeft <= 0)
        {
            if (_thresholdByFullServer)
                FinaliseVote(false);
            else FinaliseVote(_yesVoters.Count > _noVoters.Count);
        }
    }


    /// <summary>
    /// Checks if vote threshold is met, then calls the action function associated
    /// </summary>
    public void FinaliseVote(bool thresholdMet)
    {
        _timer.Stop();
        _timer.Dispose();
        VoteService.StopVoteKick();
        
        if (thresholdMet)
        {
            ReportCommandService.LogChatMessage("CritzOS",
                    $"The vote has passed. ({_yesVoters.Count}/{_voteThreshold} YES votes, {_noVoters.Count}/{_voteThreshold} NO votes)");
            ChatService.SendChatMessage($"The vote has passed!");
            _action();
        }
        else
        {
            ReportCommandService.LogChatMessage("CritzOS",
                $"The vote has failed. ({_yesVoters.Count}/{_voteThreshold} YES votes, {_noVoters.Count}/{_voteThreshold} NO votes)");
            ChatService.SendChatMessage($"The vote has failed. ({_yesVoters.Count}/{_voteThreshold} YES votes, {_noVoters.Count}/{_voteThreshold} NO votes)");
        }
    }

    private int VoteThreshold()
    {
        var threshold = NucleiConfig.KickThreshold!.Value;
        var totalPlayers = Globals.AuthenticatedPlayers.Count;
        if (totalPlayers == 1) return 1;
        return (int)Math.Ceiling(totalPlayers * threshold);
    }
}