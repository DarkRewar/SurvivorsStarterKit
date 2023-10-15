using System.Collections.Generic;
using System.Linq;

namespace GodotSurvivors.Scripts.Twitch
{
    internal class TwitchUsers
    {
        public const double DefaultTimer = 60;

        public Dictionary<string, double> UsersTimers = new();

        public List<string> Users => UsersTimers.Keys.ToList();

        public int Count => UsersTimers.Count;

        public void Update(double delta)
        {
            foreach (var username in UsersTimers.Keys)
            {
                UsersTimers[username] -= delta;
                if (UsersTimers[username] < 0)
                {
                    UsersTimers.Remove(username);
                }
            }
        }

        public void OnMessage(string username)
        {
            UsersTimers[username] = DefaultTimer;
        }
    }
}
