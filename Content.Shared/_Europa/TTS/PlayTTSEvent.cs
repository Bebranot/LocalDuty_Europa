using Robust.Shared.Serialization;

namespace Content.Shared._Europa.TTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent : EntityEventArgs
{
    public byte[] Data { get; }
    public NetEntity? SourceUid { get; }
    public bool IsWhisper { get; }

    public TtsType TtsType { get; }

    public PlayTTSEvent(byte[] data, NetEntity? sourceUid = null, bool isWhisper = false, TtsType ttsType = TtsType.Speech)
    {
        Data = data;
        SourceUid = sourceUid;
        IsWhisper = isWhisper;
        TtsType = ttsType;
    }
}

public enum TtsType
{
    Speech,
    Radio,
    Announcement
}
