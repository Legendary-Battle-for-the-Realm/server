using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Server.Data;
using Shared.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Services
{
    public class CardSyncService
    {
        private readonly AppDbContext _context;
        private readonly string _dataPath;

        public CardSyncService(AppDbContext context, string dataPath)
        {
            _context = context;
            _dataPath = dataPath;
        }

        public async Task SyncCardsFromJsonAsync()
        {
            // Đọc file action_cards.json
            var cardsPath = Path.Combine(_dataPath, "card.json");
            if (!File.Exists(cardsPath))
            {
                throw new FileNotFoundException($"Không tìm thấy file {cardsPath}.");
            }

            var cardsJson = await File.ReadAllTextAsync(cardsPath);
            var allCards = JsonConvert.DeserializeObject<List<Card>>(cardsJson);

            // Lấy tất cả tên thẻ hiện có từ database
            var existingCardNames = await _context.Cards
                .AsNoTracking()
                .Select(c => c.Name)
                .ToListAsync();

            // Tách danh sách thành hai: thẻ cần thêm và thẻ cần cập nhật
            var cardsToAdd = new List<Card>();
            var cardsToUpdate = new List<Card>();

            foreach (var card in allCards)
            {
                // Kiểm tra xem thẻ có tồn tại trong database không (dựa trên Name)
                if (existingCardNames.Contains(card.Name))
                {
                    // Thẻ đã tồn tại, thêm vào danh sách cập nhật
                    cardsToUpdate.Add(card);
                }
                else
                {
                    // Thẻ không tồn tại, thêm vào danh sách để thêm mới
                    card.Id = 0; // Đặt Id = 0 để EF Core tự tạo Id
                    cardsToAdd.Add(card);
                }
            }

            // Bước 1: Thêm các thẻ mới trước
            if (cardsToAdd.Any())
            {
                // Tạo các thực thể mới để tránh theo dõi trước
                var newCards = cardsToAdd.Select(c => new Card
                {
                    Id = 0, // Đảm bảo Id = 0 để EF Core tự tạo
                    Name = c.Name,
                    Desc = c.Desc,
                    Ref = c.Ref,
                    Type = c.Type,
                    EffectId = c.EffectId,
                    Effect = c.Effect != null ? new Effect
                    {
                        Id = 0, // Đặt Id = 0 để EF Core tự tạo nếu cần
                        Name = c.Effect.Name,
                        Ref = c.Effect.Ref
                    } : null,
                    Quantity = c.Quantity
                }).ToList();

                await _context.Cards.AddRangeAsync(newCards);
                await _context.SaveChangesAsync();

                // Cập nhật lại danh sách tên thẻ hiện có sau khi thêm mới
                existingCardNames = await _context.Cards
                    .AsNoTracking()
                    .Select(c => c.Name)
                    .ToListAsync();
            }

            // Bước 2: Cập nhật các thẻ hiện có
            foreach (var card in cardsToUpdate)
            {
                // Tìm thẻ trong database dựa trên Name
                var cardToUpdate = await _context.Cards
                    .Include(c => c.Effect)
                    .FirstOrDefaultAsync(c => c.Name == card.Name);

                if (cardToUpdate != null)
                {
                    cardToUpdate.Name = card.Name;
                    cardToUpdate.Desc = card.Desc;
                    cardToUpdate.Ref = card.Ref;
                    cardToUpdate.Type = card.Type;
                    cardToUpdate.EffectId = card.EffectId;
                    cardToUpdate.Quantity = card.Quantity;

                    // Cập nhật Effect
                    if (card.Effect != null)
                    {
                        if (cardToUpdate.Effect != null)
                        {
                            cardToUpdate.Effect.Name = card.Effect.Name;
                            cardToUpdate.Effect.Ref = card.Effect.Ref;
                        }
                        else
                        {
                            cardToUpdate.Effect = new Effect
                            {
                                Id = 0, // Gán Id = 0 để EF Core tự tạo
                                Name = card.Effect.Name,
                                Ref = card.Effect.Ref
                            };
                        }
                    }
                    else
                    {
                        cardToUpdate.Effect = null;
                    }
                }
            }

            // Lưu các thay đổi cập nhật
            if (cardsToUpdate.Any())
            {
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    throw new Exception($"Không thể lưu thẻ: {ex.InnerException?.Message}", ex);
                }
            }
        }
    }
}