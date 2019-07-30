using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Blavalon.Data
{
    public class DylanAsAService
    {
        private readonly Timer _timer;

        public DylanAsAService()
        {
            _timer = new Timer(
            (state) => RefreshEvent.Invoke(),
            state: null,
            dueTime: 1000,
            period: 500);
        }

        public event Action RefreshEvent;
    }
}
