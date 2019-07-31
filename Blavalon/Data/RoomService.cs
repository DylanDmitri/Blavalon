using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Blavalon.Data
{
    public class RoomService
    {
        private object _lock = new object();
        private Dictionary<Guid, Room> _rooms = new Dictionary<Guid, Room>();

        private readonly TimeSpan _roomTimeout = TimeSpan.FromHours(2);

        public bool CheckRoomExpiration()
        {
            var expired = _rooms.Keys.Where(id => _rooms[id].Age > _roomTimeout).ToList();
            foreach (var key in expired)
            {
                _rooms.Remove(key);
            }

            return expired.Count > 0;
        }

        public List<RoomStatus> Descriptions =>
            _rooms.Values
                .OrderBy(room => room.Age)
                .Select(room => room.Status)
                .ToList();

        public Room GetRoom(Guid key)
        {
            if (_rooms.TryGetValue(key, out Room room))
            {
                return room;
            }
            throw new KeyNotFoundException();
        }

        public Guid CreateRoom(string creator)
        {
            lock (_lock)
            {
                var id = Guid.NewGuid();
                _rooms.Add(id, new Room(id, creator));
                return id;
            }
        }

        public Room GetRoomForPlayer(string username)
        {
            if (username == "") return null;

            lock (_lock)
            {
                var possible = _rooms.Values.Where(room => room.Players.Contains(username)).ToList();
                switch (possible.Count)
                {
                    case 0: return null;
                    case 1: return _rooms[possible[0]._id];
                    default: throw new InvalidOperationException("User is in multiple rooms.");
                }
            }
        }
    }

    public class Room
    {
        public string Creator;
        public bool Active = false;
        public RoomConfiguration Config = new RoomConfiguration();

        public HashSet<string> Players = new HashSet<string>();
        public Dictionary<string, GameInformation> Assignments = new Dictionary<string, GameInformation>();

        public TimeSpan Age => DateTime.UtcNow - _timeCreated;

        internal readonly Guid _id;
        internal readonly DateTime _timeCreated;
        internal readonly Random rnd = new Random();

        public Room(Guid id, string creator)
        {
            _id = id;
            _timeCreated = DateTime.UtcNow;

            Creator = creator;
        }

        public RoomStatus Status => new RoomStatus
        {
            Id = _id,
            Status = $"({Players.Count}/{Config.NumPlayers} players)",
            Name = $"{Creator}'s Room",
        };

        public void StartGame()
        {
            Active = true;
            AssignRoles();
        }

        private void AssignRoles()
        {
            // build roles list
            int numEvil = 2;
            if (Config.NumPlayers >= 7)
            {
                numEvil = 3;
            }
            if (Config.NumPlayers >= 10)
            {
                numEvil = 4;
            }

            List<Role> roles = new List<Role>();
            if (Config.HasAssassin) roles.Add(Role.Assassin);
            if (Config.HasMordred) roles.Add(Role.Mordred);
            if (Config.HasMorganna) roles.Add(Role.Morganna);
            if (Config.HasOberron) roles.Add(Role.Oberon);
            while (roles.Count < numEvil)
            {
                roles.Add(Role.GenericEvil);
            }
            Debug.Assert(roles.Count == numEvil);

            if (Config.HasMerlin) roles.Add(Role.Merlin);
            if (Config.HasPercival) roles.Add(Role.Percival);
            while (roles.Count < Config.NumPlayers)
            {
                roles.Add(Role.GenericGood);
            }
            Debug.Assert(roles.Count == Players.Count);

            // give roles to players
            var temp = roles.Select(t => t).ToList();
            foreach (var name in Players)
            {
                Assignments[name] = new GameInformation();

                var index = rnd.Next(temp.Count);
                Assignments[name].YourRole = temp[index];
                temp.RemoveAt(index);
            }

            var playerLookup = new Dictionary<Role, List<string>>();
            foreach (var name in Assignments.Keys)
            {
                var role = Assignments[name].YourRole;
                if (!playerLookup.ContainsKey(role))
                {
                    playerLookup[role] = new List<string>();
                }
                playerLookup[role].Add(name);
            }

            // get player information
            void maybeAdd(string player, List<string> knowledge, string description, List<Role> targets)
            {
                foreach (Role target in targets.Where(playerLookup.ContainsKey))
                {
                    foreach (var name in playerLookup[target].Where(n => n != player))
                    {
                        knowledge.Add($"{name} is {description}.");
                    }
                }
            }

            // load player knowledge
            foreach (var name in Assignments.Keys)
            {
                var info = Assignments[name];
                var role = info.YourRole;

                List<Role> merlinSees = new[] { Role.Morganna, Role.Assassin, Role.GenericEvil, Role.Oberon }.ToList();
                List<Role> evilSees = new[] { Role.Morganna, Role.Assassin, Role.Mordred, Role.GenericEvil }.ToList();

                switch (info.YourRole)
                {
                    case Role.Merlin:
                        maybeAdd(name, info.Knowledge, "evil", merlinSees);
                        break;
                    case Role.Percival:
                        maybeAdd(name, info.Knowledge, "magical", new[] { Role.Merlin, Role.Morganna }.ToList());
                        break;
                    case Role.Assassin:
                    case Role.Mordred:
                    case Role.Morganna:
                    case Role.GenericEvil:
                        maybeAdd(name, info.Knowledge, "evil with you", evilSees);
                        break;
                }
            }
        }
    }

    public class RoomStatus
    {
        public Guid Id;
        public string Status;
        public string Name;
    }

    public class RoomConfiguration
    {
        public int NumPlayers = 7;

        public bool HasMerlin = true;
        public bool HasPercival = true;
        public bool HasMordred = true;
        public bool HasMorganna = true;
        public bool HasAssassin = true;
        public bool HasOberron = false;

        public bool LakeShowsRoles = false;
    }

    public enum Role
    {
        GenericGood,
        GenericEvil,
        Merlin,
        Percival,
        Mordred,
        Morganna,
        Assassin,
        Oberon,
    }

    public class GameInformation
    {
        public Role YourRole;
        public List<string> Knowledge = new List<string>();
    }
}
