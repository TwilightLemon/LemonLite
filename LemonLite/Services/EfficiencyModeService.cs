using H.NotifyIcon.EfficiencyMode;
using System;
using System.Collections.Generic;

namespace LemonLite.Services
{
    public class EfficiencyModeService
    {
        private readonly HashSet<string> _runningWindows = [];
        private readonly object _lock = new();

        public void NotifyWindowOpened(string windowName)
        {
            if (string.IsNullOrWhiteSpace(windowName))
            {
                return;
            }

            lock (_lock)
            {
                _runningWindows.Add(windowName);
                EfficiencyModeUtilities.SetEfficiencyMode(false);
            }
        }

        public void NotifyWindowClosed(string windowName)
        {
            if (string.IsNullOrWhiteSpace(windowName))
            {
                return;
            }

            lock (_lock)
            {
                _runningWindows.Remove(windowName);
                if (_runningWindows.Count == 0)
                {
                    EfficiencyModeUtilities.SetEfficiencyMode(true);
                }
            }
        }
    }
}
