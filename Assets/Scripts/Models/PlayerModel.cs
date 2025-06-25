using Normal.Realtime;
using Normal.Realtime.Serialization;

[RealtimeModel]
public partial class PlayerModel
{
    [RealtimeProperty(1, true, true)] private string _playerName;
}
