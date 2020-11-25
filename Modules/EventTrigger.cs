using System;
using System.Timers;

namespace CatcherEngine.Modules
{
    class EventTrigger
    {
        readonly int _delay;
        Timer _timer = new Timer();

        public EventTrigger(int delay)
        {
            _delay = delay;
        }

        public void ExecuteOnEventTimeOut(Action action)
        {
            if (!_timer.Enabled)
            {
                _timer = new Timer(_delay)
                {
                    AutoReset = false
                };
                _timer.Elapsed += (object sender, ElapsedEventArgs e) =>
                {
                    action();
                };
                _timer.Start();
            }
            else
            {
                _timer.Stop();
                _timer.Start();
            }
        }
    }
}
