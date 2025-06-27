using Normal.Realtime;
using Normal.Realtime.Serialization;

[RealtimeModel]
public partial class GameManagerModel
{
    [RealtimeProperty(1, true, true, true)] private int _gameState; 
}