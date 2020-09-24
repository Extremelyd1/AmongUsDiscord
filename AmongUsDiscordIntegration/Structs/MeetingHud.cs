using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
public struct MeetingHud {
    [FieldOffset(8)] public uint cachedPtr;
    [FieldOffset(32)] public bool DespawnOnDestroy;
    [FieldOffset(36)] public IntPtr ButtonParent;
    [FieldOffset(40)] public IntPtr TitleText;
    [FieldOffset(44)] public IntPtr VoteOrigin;
    [FieldOffset(56)] public IntPtr VoteButtonOffsets;
    [FieldOffset(68)] public IntPtr CounterOrigin;
    [FieldOffset(80)] public IntPtr CounterOffsets;
    [FieldOffset(92)] public IntPtr SkipVoteButton;
    [FieldOffset(96)] public IntPtr playerStates;
    [FieldOffset(100)] public IntPtr PlayerButtonPrefab;
    [FieldOffset(104)] public IntPtr PlayerVotePrefab;
    [FieldOffset(108)] public IntPtr CrackedGlass;
    [FieldOffset(112)] public IntPtr Glass;
    [FieldOffset(116)] public IntPtr ProcessButton;
    [FieldOffset(120)] public IntPtr VoteSound;
    [FieldOffset(124)] public IntPtr VoteLockinSound;
    [FieldOffset(128)] public IntPtr VoteEndingSound;
    [FieldOffset(132)] public uint state;
    [FieldOffset(136)] public IntPtr SkippedVoting;
    [FieldOffset(140)] public IntPtr HostIcon;
    [FieldOffset(144)] public IntPtr KillBackground;
    [FieldOffset(148)] public IntPtr exiledPlayer;
    [FieldOffset(152)] public bool wasTie;
    [FieldOffset(156)] public IntPtr TimerText;
    [FieldOffset(160)] public float discussionTimer;
    [FieldOffset(164)] public byte reporterId;
    [FieldOffset(165)] public bool amDead;
    [FieldOffset(168)] public float resultsStartedAt;
    [FieldOffset(172)] public uint lastSecond;
}