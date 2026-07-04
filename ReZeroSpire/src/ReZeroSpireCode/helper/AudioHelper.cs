using System;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using ReZeroSpire.ReZeroSpireCode.Setting;
using STS2RitsuLib.Audio;

namespace ReZeroSpire.ReZeroSpireCode.Helper;

public static class AudioHelper
{
    private const float Volume = 1.2f;

    public static bool IsPlayingCustomBgm { get; set; }

    private static string? _currentTrack;

    public static void PlayDeathSound()
    {
        if (!ReZeroSpireSettingsPage.EnableDeathSfx) return;

        CheckpointHelper.DeathCount++;
        StopGameMusic();
        IsPlayingCustomBgm = true;

        if (CheckpointHelper.DeathCount == 1)
        {
            GameAudioService.Shared.PlayMusic(
                AudioSource.Event("event:/ReZeroSpire/music/EdInsert"),
                new AudioPlaybackOptions
                {
                    Volume = Volume,
                    Scope = AudioLifecycleScope.Run,
                });
            Entry.Logger.Info("Playing first death music: EdInsert");
        }
        else
        {
            GameAudioService.Shared.PlayOneShot(
                AudioSource.Event("event:/ReZeroSpire/sfx/DeathSound"),
                new AudioPlaybackOptions
                {
                    Volume = Volume,
                    Scope = AudioLifecycleScope.Run,
                });
            Entry.Logger.Info($"Playing death SFX: DeathSound (death #{CheckpointHelper.DeathCount})");
        }
    }

    public static void TrySwapBossBgm(AbstractRoom room)
    {
        if (!ReZeroSpireSettingsPage.EnableBossBgm) return;
        if (room is not CombatRoom combatRoom) return;
        if (combatRoom.Encounter.RoomType != RoomType.Boss) return;

        var track = ResolveBossTrack(combatRoom.Act, combatRoom.Encounter);
        if (track == null) return;

        try
        {
            if (_currentTrack != null)
            {
                NAudioManager.Instance.StopLoop(_currentTrack);
                _currentTrack = null;
            }

            StopGameMusic();
            IsPlayingCustomBgm = true;
            _currentTrack = track;

            NAudioManager.Instance.PlayLoop(track, usesLoopParam: false);

            Entry.Logger.Info($"Swapping BGM to {track} (act {combatRoom.Act.Index}).");
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"TrySwapBossBgm: {ex}");
        }
    }

    public static void StopBgm()
    {
        if (_currentTrack != null)
        {
            NAudioManager.Instance?.StopLoop(_currentTrack);
            _currentTrack = null;
            Entry.Logger.Info("Boss BGM stopped.");
        }

        IsPlayingCustomBgm = false;
    }

    private static string? ResolveBossTrack(ActModel act, EncounterModel encounter)
    {
        return act.Index switch
        {
            0 => "event:/ReZeroSpire/music/StyxHelix",
            1 => "event:/ReZeroSpire/music/Realize",
            2 => encounter.Id == act.SecondBossEncounter?.Id
                    ? "event:/ReZeroSpire/music/EnderEmber"
                    : "event:/ReZeroSpire/music/Longshot",
            _ => null,
        };
    }

    private static void StopGameMusic()
    {
        NRun.Instance?.RunMusicController?.StopMusic();
        NAudioManager.Instance?.StopMusic();
    }
}
