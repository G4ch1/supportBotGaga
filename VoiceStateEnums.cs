
namespace supportBotGaga
{
    public enum VoiceStateEnums : byte
    {
        Normal = 0x0,
        Suppressed = 0x1,
        Muted = 0x2,
        Deafened = 0x4,
        SelfMuted = 0x8,
        SelfDeafened = 0x10,
        SelfStream = 0x20,
        SelfVideo = 0x40
    }
}
