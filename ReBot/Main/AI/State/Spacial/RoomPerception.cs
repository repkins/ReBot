using MapGeneration;
using ReBot.Navigation;
using UnityEngine;

namespace ReBot.Main.AI.State.Spacial;

internal struct RoomPerception(int botCount)
{
    public void Process()
    {
        for (var botIdx = 0; botIdx < botCount; botIdx++)
        {
            var bot = BotStore.Bots[botIdx];

            var prevRoomWithin = bot.RoomStore.PrevRoomWithin;

            var playerPosition = bot.PlayerStore.Position;
            if (playerPosition.TryGetRoom(out var roomWithin))
            {
                // Room change check
                if (prevRoomWithin != null && prevRoomWithin != roomWithin)
                {
                    bot.RoomStore.RoomLastVisitTime[prevRoomWithin.GetInstanceID()] = Time.time;
                }

                bot.RoomStore.RoomEnterInstanceIds.Clear();
                bot.RoomStore.RoomEnterLocations.Clear();
                bot.RoomStore.RoomExitLocations.Clear();
                bot.RoomStore.RoomExitIsWithins.Clear();
                bot.RoomStore.RoomIsElevations.Clear();

                foreach (var connectedRoom in roomWithin.ConnectedRooms)
                {
                    var connectionPosition = NavigationStore.ConnectionsByRoomPair[(roomWithin, connectedRoom)];
                    var roomDirection = Vector3.Normalize(connectedRoom.transform.position - roomWithin.transform.position);
                    var roomEnterPosition = connectionPosition + roomDirection * 1f;
                    var roomExitPosition = connectionPosition - roomDirection * 1f;

                    bot.RoomStore.RoomEnterInstanceIds.Add(connectedRoom.GetInstanceID());
                    bot.RoomStore.RoomEnterLocations.Add(roomEnterPosition);
                    bot.RoomStore.RoomExitLocations.Add(roomExitPosition);

                    var roomExitIsWithin = Vector3.SqrMagnitude(playerPosition - connectionPosition) < Mathf.Pow(2f, 2f);
                    bot.RoomStore.RoomExitIsWithins.Add(roomExitIsWithin);

                    var roomIsElevation = false;
                    bot.RoomStore.RoomIsElevations.Add(roomIsElevation);
                }
            }

            prevRoomWithin = roomWithin;
            bot.RoomStore.PrevRoomWithin = prevRoomWithin;
        }
    }
}
