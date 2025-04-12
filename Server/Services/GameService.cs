using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Services
{
    public class GameService
    {
        private readonly AppDbContext _context;

        public GameService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> StartGameAsync(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Players)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.IsGameStarted || room.Players.Count < 2)
            {
                return false;
            }

            // Xáo trộn bộ bài
            var cards = await _context.Cards.ToListAsync();
            room.Deck = cards.OrderBy(x => Guid.NewGuid()).ToList();

            // Xác định lượt chơi (Turn Order Phase)
            var turnOrder = new List<int>();
            foreach (var player in room.Players)
            {
                var card = room.Deck.First();
                room.Deck.RemoveAt(0);
                turnOrder.Add(player.Id);
            }
            room.TurnOrder = turnOrder;
            room.CurrentTurnPlayerId = turnOrder.First();
            room.IsGameStarted = true;

            // Chia bài ban đầu (Setup Phase)
            foreach (var player in room.Players)
            {
                for (int i = 0; i < 5; i++) // Chia 5 lá bài ban đầu
                {
                    if (room.Deck.Any())
                    {
                        var card = room.Deck.First();
                        player.Hand.Add(card);
                        room.Deck.RemoveAt(0);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Card> DrawCardAsync(int roomId, int playerId)
        {
            var room = await _context.Rooms
                .Include(r => r.Players)
                .Include(r => r.Deck)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || !room.IsGameStarted || room.CurrentTurnPlayerId != playerId)
            {
                return null;
            }

            var player = room.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                return null;
            }

            if (!room.Deck.Any())
            {
                return null; // Hết bài để bốc
            }

            var card = room.Deck.First();
            player.Hand.Add(card);
            room.Deck.RemoveAt(0);

            // Chuyển lượt cho người chơi tiếp theo
            var currentIndex = room.TurnOrder.IndexOf(playerId);
            var nextIndex = (currentIndex + 1) % room.TurnOrder.Count;
            room.CurrentTurnPlayerId = room.TurnOrder[nextIndex];

            await _context.SaveChangesAsync();
            return card;
        }

        public async Task<bool> UseCardAsync(int roomId, int playerId, int cardId)
        {
            var room = await _context.Rooms
                .Include(r => r.Players)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || !room.IsGameStarted || room.CurrentTurnPlayerId != playerId)
            {
                return false;
            }

            var player = room.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                return false;
            }

            var card = player.Hand.FirstOrDefault(c => c.Id == cardId);
            if (card == null)
            {
                return false;
            }

            // Xử lý logic sử dụng bài (ví dụ: tấn công, phòng thủ)
            // Giả sử đây là bài tấn công
            if (card.Type == 1) // Type 1: Attack
            {
                // Tấn công người chơi tiếp theo
                var nextPlayerId = room.TurnOrder[(room.TurnOrder.IndexOf(playerId) + 1) % room.TurnOrder.Count];
                var nextPlayer = room.Players.FirstOrDefault(p => p.Id == nextPlayerId);
                if (nextPlayer != null)
                {
                    nextPlayer.HP -= 10; // Giả sử gây 10 sát thương
                }
            }

            player.Hand.Remove(card);

            // Chuyển lượt
            var currentIndex = room.TurnOrder.IndexOf(playerId);
            var nextIndex = (currentIndex + 1) % room.TurnOrder.Count;
            room.CurrentTurnPlayerId = room.TurnOrder[nextIndex];

            await _context.SaveChangesAsync();
            return true;
        }
    }
}