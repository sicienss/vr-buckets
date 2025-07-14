using Normal.Realtime;
using Normal.Realtime.Serialization;

[RealtimeModel]
public partial class PlayerModel
{
    [RealtimeProperty(1, true, true, true)] private string _playerName;
    [RealtimeProperty(2, true, true, true)] private int _playerScore = 0;
    [RealtimeProperty(3, true, true, true)] private int _playerShotStreak = 0;
    [RealtimeProperty(4, true, true, true)] private bool _playerIsReady = false; // Used by host to check when all players have loaded into scenes (for example, gameplay scene), so that host only changes state when all players have loaded into scene and everything is accessible on every client
}
