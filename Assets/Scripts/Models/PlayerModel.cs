using Normal.Realtime;
using Normal.Realtime.Serialization;

[RealtimeModel]
public partial class PlayerModel
{
    [RealtimeProperty(1, true, true, true)] private string _playerName;
    [RealtimeProperty(2, true, true, true)] private int _playerScore;
    [RealtimeProperty(3, true, true, true)] private int _playerShotStreak;
}
