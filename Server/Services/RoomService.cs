using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Server.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Services
{
    public class RoomService
    {
        private readonly AppDbContext _context;

        public RoomService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Room> CreateRoomAsync(int maxPlayers)
        {
            var room = new Room
            {
                Name = $"Room {_context.Rooms.Count() + 1}",
                MaxPlayers = maxPlayers,
                IsGameStarted = false
            };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return room;
        }

        public async Task<bool> JoinRoomAsync(int roomId, int userId)
        {
            var room = await _context.Rooms
                .Include(r => r.Players)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.Players.Count >= room.MaxPlayers || room.IsGameStarted)
            {
                return false;
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            var player = new Player
            {
                Id = userId,
                Name = user.Username,
                RoomId = roomId,
                HP = user.HealthPoints
            };
            room.Players.Add(player);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}