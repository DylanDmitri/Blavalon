using System;
using System.Threading;

namespace Blavalon.Data
{
    public class DylanAsAService
    {
        private readonly Timer _timer;
        private readonly RoomService _rooms;

        public DylanAsAService(RoomService rooms)
        {
            _rooms = rooms;

            _timer = new Timer(
                (state) =>
                {
                    if (_rooms.CheckRoomExpiration()) ForceRefresh();
                },
                state: null,
                dueTime: TimeSpan.FromSeconds(30),
                period: TimeSpan.FromSeconds(30));
        }

        // each client registers their refresh on this event
        // calling ForceRefresh then refreshes all clients
        public event Action RefreshEvent;

        public void ForceRefresh()
        {
            RefreshEvent.Invoke();
        }
    }
}
