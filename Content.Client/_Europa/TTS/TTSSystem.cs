﻿using Content.Shared._Europa.TTS;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client._Europa.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private ISawmill _sawmill = default!;
    private static MemoryContentRoot _contentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private static bool _contentRootAdded;

    /// <summary>
    /// Reducing the volume of the TTS when whispering. Will be converted to logarithm.
    /// </summary>
    private const float WhisperFade = 4f;

    /// <summary>
    /// The volume at which the TTS sound will not be heard.
    /// </summary>
    private const float MinimalVolume = -10f;

    private float _volume = 0.0f;
    private float _volumeRadio = 0.0f;
    private float _volumeAnnouncement = 0.0f;
    private int _fileIdx = 0;

    public override void Initialize()
    {
        if (!_contentRootAdded)
        {
            _contentRootAdded = true;
            _res.AddRoot(Prefix, _contentRoot);
        }

        _sawmill = Logger.GetSawmill("tts");

        _cfg.OnValueChanged(CCVars.TTSVolume, v => _volume = v, true);
        _cfg.OnValueChanged(CCVars.TTSVolumeRadio, v => _volumeRadio = v, true);
        _cfg.OnValueChanged(CCVars.TTSVolumeAnnouncement, v => _volumeAnnouncement = v, true);

        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
    }

    public void RequestPreviewTTS(string voiceId, string species)
    {
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId, species));
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        _sawmill.Verbose($"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity");

        var filePath = new ResPath($"{_fileIdx++}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, ev.Data);

        var audioResource = new AudioResource();
        audioResource.Load(IoCManager.Instance!, Prefix / filePath);

        var audioParams = AudioParams.Default;

        switch (ev.TtsType)
        {
            case TtsType.Speech:
                audioParams = audioParams
                    .WithVolume(AdjustVolume(ev.IsWhisper))
                    .WithMaxDistance(AdjustDistance(ev.IsWhisper));
                break;
            case TtsType.Radio:
                audioParams = audioParams
                    .WithVolume(AdjustGlobalVolume(_volumeRadio));
                break;
            case TtsType.Announcement:
                audioParams = audioParams
                    .WithVolume(AdjustGlobalVolume(_volumeAnnouncement));
                break;
        }

        var soundSpecifier = new ResolvedPathSpecifier(Prefix / filePath);

        if (ev.SourceUid != null)
        {
            if (!TryGetEntity(ev.SourceUid.Value, out _))
                return;
            var sourceUid = GetEntity(ev.SourceUid.Value);
            _audio.PlayEntity(audioResource.AudioStream, sourceUid, soundSpecifier, audioParams);
        }
        else
        {
            _audio.PlayGlobal(audioResource.AudioStream, soundSpecifier, audioParams);
        }

        _contentRoot.RemoveFile(filePath);
    }

    private float AdjustVolume(bool isWhisper)
    {
        var volume = MinimalVolume + SharedAudioSystem.GainToVolume(_volume);

        if (isWhisper)
        {
            volume -= SharedAudioSystem.GainToVolume(WhisperFade);
        }

        return volume;
    }

    private float AdjustGlobalVolume(float volume)
    {
        return MinimalVolume + SharedAudioSystem.GainToVolume(volume);
    }

    private float AdjustDistance(bool isWhisper)
    {
        return isWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange;
    }
}
