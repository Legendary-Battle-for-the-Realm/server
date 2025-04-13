using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shared.Enums;
using Server.Hubs;

namespace Server.Services
{
    public class GameService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<GameHub> _hubContext;

        public GameService(AppDbContext context, IHubContext<GameHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
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

            // Xóa các CardLocation cũ liên quan đến Room (nếu có)
            var existingLocations = await _context.CardLocations
                .Where(cl => cl.RoomId == roomId || cl.PlayerId.HasValue && room.Players.Select(p => p.Id).Contains(cl.PlayerId.Value))
                .ToListAsync();
            _context.CardLocations.RemoveRange(existingLocations);

            // Lấy tất cả thẻ bài từ cơ sở dữ liệu
            var cards = await _context.Cards.ToListAsync();
            if (cards == null || cards.Count < room.Players.Count * 5)
            {
                return false;
            }

            // Xáo bài (Setup Phase)
            var shuffledCards = cards.OrderBy(x => Guid.NewGuid()).ToList();

            // Thêm các thẻ vào Deck của Room
            foreach (var card in shuffledCards)
            {
                var cardLocation = new CardLocation
                {
                    CardId = card.Id,
                    RoomId = room.Id,
                    PlayerId = null,
                    LocationType = "Deck"
                };
                _context.CardLocations.Add(cardLocation);
            }

            // Turn Order Phase: Mỗi người chơi bốc 1 lá bài để xác định thứ tự lượt
            var playerOrder = new List<(int PlayerId, int CardValue)>();
            var deckCards = await _context.CardLocations
                .Where(cl => cl.RoomId == roomId && cl.LocationType == "Deck")
                .Include(cl => cl.Card)
                .ToListAsync();

            foreach (var player in room.Players)
            {
                if (deckCards.Any())
                {
                    var cardLocation = deckCards.First();
                    cardLocation.RoomId = room.Id;
                    cardLocation.PlayerId = null;
                    cardLocation.LocationType = "DiscardPile";
                    playerOrder.Add((player.Id, cardLocation.Card.Quantity)); // Sửa ở đây: bỏ ?? 0
                    deckCards.RemoveAt(0);
                }
            }

            // Sắp xếp người chơi theo giá trị lá bài (giảm dần)
            var turnOrder = playerOrder.OrderByDescending(p => p.CardValue).Select(p => p.PlayerId).ToList();
            room.TurnOrder = turnOrder;
            room.CurrentTurnPlayerId = turnOrder.First();

            // Chia bài: mỗi người chơi nhận 5 lá (Setup Phase)
            deckCards = await _context.CardLocations
                .Where(cl => cl.RoomId == roomId && cl.LocationType == "Deck")
                .Include(cl => cl.Card)
                .ToListAsync();

            foreach (var player in room.Players)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (deckCards.Any())
                    {
                        var cardLocation = deckCards.First();
                        cardLocation.RoomId = null;
                        cardLocation.PlayerId = player.Id;
                        cardLocation.LocationType = "Hand";
                        deckCards.RemoveAt(0);
                    }
                }
            }

            room.IsGameStarted = true;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(roomId.ToString())
                .SendAsync("GameStarted", $"Game in room {roomId} has started!");
            return true;
        }

        public async Task<Card?> DrawCardAsync(int roomId, int playerId)
        {
            var room = await _context.Rooms
                .Include(r => r.Players)
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

            // Lấy danh sách Deck
            var deckCards = await _context.CardLocations
                .Where(cl => cl.RoomId == roomId && cl.LocationType == "Deck")
                .Include(cl => cl.Card)
                .ToListAsync();

            // Nếu hết bài, xáo lại từ DiscardPile
            if (!deckCards.Any())
            {
                var discardCards = await _context.CardLocations
                    .Where(cl => cl.RoomId == roomId && cl.LocationType == "DiscardPile")
                    .Include(cl => cl.Card)
                    .ToListAsync();

                if (!discardCards.Any())
                {
                    return null;
                }

                foreach (var cardLocation in discardCards)
                {
                    cardLocation.LocationType = "Deck";
                }
                deckCards = discardCards;
            }

            var cardLocationToDraw = deckCards.FirstOrDefault();
            if (cardLocationToDraw == null)
            {
                return null;
            }

            cardLocationToDraw.RoomId = null;
            cardLocationToDraw.PlayerId = playerId;
            cardLocationToDraw.LocationType = "Hand";

            var currentIndex = room.TurnOrder.IndexOf(playerId);
            var nextIndex = (currentIndex + 1) % room.TurnOrder.Count;
            room.CurrentTurnPlayerId = room.TurnOrder[nextIndex];

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(roomId.ToString())
                .SendAsync("ReceiveGameUpdate", $"Player {playerId} drew a card.");

            await ApplyRandomEventAsync(roomId);
            var winner = await CheckWinConditionAsync(roomId);
            if (winner != null)
            {
                await _hubContext.Clients.Group(roomId.ToString())
                    .SendAsync("GameEnded", $"Player {winner.Id} wins!");
            }

            return cardLocationToDraw.Card;
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

            var cardLocation = await _context.CardLocations
                .Where(cl => cl.PlayerId == playerId && cl.LocationType == "Hand" && cl.CardId == cardId)
                .Include(cl => cl.Card)
                .FirstOrDefaultAsync();

            if (cardLocation == null)
            {
                return false;
            }

            var card = cardLocation.Card;
            if (card.Type == CardType.Action)
            {
                var nextPlayerId = room.TurnOrder[(room.TurnOrder.IndexOf(playerId) + 1) % room.TurnOrder.Count];
                var nextPlayer = room.Players.FirstOrDefault(p => p.Id == nextPlayerId);
                if (nextPlayer != null)
                {
                    nextPlayer.HP -= 10;
                    await _hubContext.Clients.Group(roomId.ToString())
                        .SendAsync("ReceiveGameUpdate", $"Player {playerId} used an Action card on Player {nextPlayerId}, dealing 10 damage.");
                }
            }
            else if (card.Type == CardType.Consumable)
            {
                player.HP += 20;
                await _hubContext.Clients.Group(roomId.ToString())
                    .SendAsync("ReceiveGameUpdate", $"Player {playerId} used a Consumable card, restoring 20 HP.");
            }

            cardLocation.PlayerId = null;
            cardLocation.RoomId = room.Id;
            cardLocation.LocationType = "DiscardPile";

            var currentIndex = room.TurnOrder.IndexOf(playerId);
            var nextIndex = (currentIndex + 1) % room.TurnOrder.Count;
            room.CurrentTurnPlayerId = room.TurnOrder[nextIndex];

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(roomId.ToString())
                .SendAsync("ReceiveGameUpdate", $"Player {playerId} used card {cardId}.");

            await ApplyRandomEventAsync(roomId);
            var winner = await CheckWinConditionAsync(roomId);
            if (winner != null)
            {
                await _hubContext.Clients.Group(roomId.ToString())
                    .SendAsync("GameEnded", $"Player {winner.Id} wins!");
            }

            return true;
        }

        public async Task<bool> PassTurnAsync(int roomId, int playerId)
        {
            var room = await _context.Rooms
                .Include(r => r.Players)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || !room.IsGameStarted || room.CurrentTurnPlayerId != playerId)
            {
                return false;
            }

            var currentIndex = room.TurnOrder.IndexOf(playerId);
            var nextIndex = (currentIndex + 1) % room.TurnOrder.Count;
            room.CurrentTurnPlayerId = room.TurnOrder[nextIndex];

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(roomId.ToString())
                .SendAsync("ReceiveGameUpdate", $"Player {playerId} passed their turn.");

            await ApplyRandomEventAsync(roomId);
            var winner = await CheckWinConditionAsync(roomId);
            if (winner != null)
            {
                await _hubContext.Clients.Group(roomId.ToString())
                    .SendAsync("GameEnded", $"Player {winner.Id} wins!");
            }

            return true;
        }

        public async Task ApplyRandomEventAsync(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Players)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || !room.IsGameStarted)
            {
                return;
            }

            var random = new Random();
            var eventType = random.Next(1, 4);
            string eventMessage = "";
            switch (eventType)
            {
                case 1:
                    foreach (var player in room.Players)
                    {
                        player.HP -= 20;
                    }
                    eventMessage = "A dragon attacks! All players lose 20 HP.";
                    break;
                case 2:
                    foreach (var player in room.Players)
                    {
                        player.HP -= 10;
                    }
                    eventMessage = "An earthquake strikes! All players lose 10 HP.";
                    break;
                default:
                    eventMessage = "Nothing happens this turn.";
                    break;
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(roomId.ToString())
                .SendAsync("ReceiveGameUpdate", eventMessage);
        }

        public async Task<Player?> CheckWinConditionAsync(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Players)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || !room.IsGameStarted)
            {
                return null;
            }

            var eliminatedPlayers = room.Players.Where(p => p.HP <= 0).ToList();
            foreach (var player in eliminatedPlayers)
            {
                room.TurnOrder.Remove(player.Id);
                room.Players.Remove(player);
                await _hubContext.Clients.Group(roomId.ToString())
                    .SendAsync("ReceiveGameUpdate", $"Player {player.Id} has been eliminated!");
            }

            var alivePlayers = room.Players.Where(p => p.HP > 0).ToList();
            if (alivePlayers.Count == 1)
            {
                room.IsGameStarted = false;
                await _context.SaveChangesAsync();
                return alivePlayers.First();
            }
            else if (alivePlayers.Count == 0)
            {
                room.IsGameStarted = false;
                await _context.SaveChangesAsync();
                await _hubContext.Clients.Group(roomId.ToString())
                    .SendAsync("GameEnded", "Game ends in a draw!");
                return null;
            }

            await _context.SaveChangesAsync();
            return null;
        }
    }
}