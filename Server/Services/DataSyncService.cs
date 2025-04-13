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
    public class DataSyncService
    {
        private readonly AppDbContext _context;
        private readonly string _dataPath;

        public DataSyncService(AppDbContext context, string dataPath)
        {
            _context = context;
            _dataPath = dataPath;
        }

        public async Task SyncAllDataAsync()
        {
            // Đồng bộ Cultivations trước (vì Armor và Weapon tham chiếu đến CultivationRequired)
            await SyncCultivationsAsync();

            // Đồng bộ Cards
            await SyncCardsAsync();

            // Đồng bộ Armors
            await SyncArmorsAsync();

            // Đồng bộ Weapons
            await SyncWeaponsAsync();

            // Đồng bộ Characters và Skills
            await SyncCharactersAsync();
        }

        private async Task SyncCultivationsAsync()
        {
            var filePath = Path.Combine(_dataPath, "cultivation.json");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file {filePath}.");
            }

            var json = await File.ReadAllTextAsync(filePath);
            var cultivations = JsonConvert.DeserializeObject<List<Cultivation>>(json);

            if (cultivations == null)
            {
                throw new InvalidOperationException("Không thể phân tích file cultivation.json.");
            }

            var existingNames = await _context.Cultivations
                .AsNoTracking()
                .Select(c => c.Name)
                .ToListAsync();

            var toAdd = new List<Cultivation>();
            var toUpdate = new List<Cultivation>();

            foreach (var cultivation in cultivations)
            {
                if (existingNames.Contains(cultivation.Name))
                {
                    toUpdate.Add(cultivation);
                }
                else
                {
                    cultivation.Id = 0;
                    toAdd.Add(cultivation);
                }
            }

            if (toAdd.Any())
            {
                await _context.Cultivations.AddRangeAsync(toAdd);
                await _context.SaveChangesAsync();
            }

            foreach (var cultivation in toUpdate)
            {
                var existing = await _context.Cultivations
                    .FirstOrDefaultAsync(c => c.Name == cultivation.Name);

                if (existing != null)
                {
                    existing.Name = cultivation.Name;
                    existing.RequiredQi = cultivation.RequiredQi;
                }
            }

            if (toUpdate.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task SyncCardsAsync()
        {
            var filePath = Path.Combine(_dataPath, "card.json");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file {filePath}.");
            }

            var json = await File.ReadAllTextAsync(filePath);
            var cards = JsonConvert.DeserializeObject<List<Card>>(json);

            if (cards == null)
            {
                throw new InvalidOperationException("Không thể phân tích file card.json.");
            }

            var existingNames = await _context.Cards
                .AsNoTracking()
                .Select(c => c.Name)
                .ToListAsync();

            var toAdd = new List<Card>();
            var toUpdate = new List<Card>();

            foreach (var card in cards)
            {
                if (existingNames.Contains(card.Name))
                {
                    toUpdate.Add(card);
                }
                else
                {
                    card.Id = 0;
                    toAdd.Add(card);
                }
            }

            if (toAdd.Any())
            {
                var newCards = toAdd.Select(c => new Card
                {
                    Id = 0,
                    Name = c.Name,
                    Desc = c.Desc,
                    Ref = c.Ref,
                    Type = c.Type,
                    EffectId = c.EffectId,
                    Effect = c.Effect != null ? new Effect
                    {
                        Id = 0,
                        Name = c.Effect.Name,
                        Ref = c.Effect.Ref
                    } : null,
                    Quantity = c.Quantity
                }).ToList();

                await _context.Cards.AddRangeAsync(newCards);
                await _context.SaveChangesAsync();

                existingNames = await _context.Cards
                    .AsNoTracking()
                    .Select(c => c.Name)
                    .ToListAsync();
            }

            foreach (var card in toUpdate)
            {
                var existing = await _context.Cards
                    .Include(c => c.Effect)
                    .FirstOrDefaultAsync(c => c.Name == card.Name);

                if (existing != null)
                {
                    existing.Name = card.Name;
                    existing.Desc = card.Desc;
                    existing.Ref = card.Ref;
                    existing.Type = card.Type;
                    existing.EffectId = card.EffectId;
                    existing.Quantity = card.Quantity;

                    if (card.Effect != null)
                    {
                        if (existing.Effect != null)
                        {
                            existing.Effect.Name = card.Effect.Name;
                            existing.Effect.Ref = card.Effect.Ref;
                        }
                        else
                        {
                            existing.Effect = new Effect
                            {
                                Id = 0,
                                Name = card.Effect.Name,
                                Ref = card.Effect.Ref
                            };
                        }
                    }
                    else
                    {
                        existing.Effect = null;
                    }
                }
            }

            if (toUpdate.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task SyncArmorsAsync()
        {
            var filePath = Path.Combine(_dataPath, "armor.json");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file {filePath}.");
            }

            var json = await File.ReadAllTextAsync(filePath);
            var armors = JsonConvert.DeserializeObject<List<Armor>>(json);

            if (armors == null)
            {
                throw new InvalidOperationException("Không thể phân tích file armor.json.");
            }

            var existingNames = await _context.Armors
                .AsNoTracking()
                .Select(a => a.Name)
                .ToListAsync();

            var toAdd = new List<Armor>();
            var toUpdate = new List<Armor>();

            foreach (var armor in armors)
            {
                if (existingNames.Contains(armor.Name))
                {
                    toUpdate.Add(armor);
                }
                else
                {
                    armor.Id = 0;
                    toAdd.Add(armor);
                }
            }

            if (toAdd.Any())
            {
                var newArmors = toAdd.Select(a => new Armor
                {
                    Id = 0,
                    Name = a.Name,
                    Atk = a.Atk,
                    Def = a.Def,
                    Desc = a.Desc,
                    UserId = a.UserId,
                    CultivationRequired = a.CultivationRequired ?? "Nhập môn",
                    Skill = new EquipmentSkill
                    {
                        Id = 0,
                        Name = a.Skill.Name,
                        Ref = a.Skill.Ref
                    }
                }).ToList();

                await _context.Armors.AddRangeAsync(newArmors);
                await _context.SaveChangesAsync();

                existingNames = await _context.Armors
                    .AsNoTracking()
                    .Select(a => a.Name)
                    .ToListAsync();
            }

            foreach (var armor in toUpdate)
            {
                var existing = await _context.Armors
                    .Include(a => a.Skill)
                    .FirstOrDefaultAsync(a => a.Name == armor.Name);

                if (existing != null)
                {
                    existing.Name = armor.Name;
                    existing.Atk = armor.Atk;
                    existing.Def = armor.Def;
                    existing.Desc = armor.Desc;
                    existing.UserId = armor.UserId;
                    existing.CultivationRequired = armor.CultivationRequired ?? "Nhập môn";

                    existing.Skill.Name = armor.Skill.Name;
                    existing.Skill.Ref = armor.Skill.Ref;
                }
            }

            if (toUpdate.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task SyncWeaponsAsync()
        {
            var filePath = Path.Combine(_dataPath, "weapon.json");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file {filePath}.");
            }

            var json = await File.ReadAllTextAsync(filePath);
            var weapons = JsonConvert.DeserializeObject<List<Weapon>>(json);

            if (weapons == null)
            {
                throw new InvalidOperationException("Không thể phân tích file weapon.json.");
            }

            var existingNames = await _context.Weapons
                .AsNoTracking()
                .Select(w => w.Name)
                .ToListAsync();

            var toAdd = new List<Weapon>();
            var toUpdate = new List<Weapon>();

            foreach (var weapon in weapons)
            {
                if (existingNames.Contains(weapon.Name))
                {
                    toUpdate.Add(weapon);
                }
                else
                {
                    weapon.Id = 0;
                    toAdd.Add(weapon);
                }
            }

            if (toAdd.Any())
            {
                var newWeapons = toAdd.Select(w => new Weapon
                {
                    Id = 0,
                    Name = w.Name,
                    Atk = w.Atk,
                    Desc = w.Desc,
                    UserId = w.UserId,
                    CultivationRequired = w.CultivationRequired,
                    Skill = new EquipmentSkill
                    {
                        Id = 0,
                        Name = w.Skill.Name,
                        Ref = w.Skill.Ref
                    }
                }).ToList();

                await _context.Weapons.AddRangeAsync(newWeapons);
                await _context.SaveChangesAsync();

                existingNames = await _context.Weapons
                    .AsNoTracking()
                    .Select(w => w.Name)
                    .ToListAsync();
            }

            foreach (var weapon in toUpdate)
            {
                var existing = await _context.Weapons
                    .Include(w => w.Skill)
                    .FirstOrDefaultAsync(w => w.Name == weapon.Name);

                if (existing != null)
                {
                    existing.Name = weapon.Name;
                    existing.Atk = weapon.Atk;
                    existing.Desc = weapon.Desc;
                    existing.UserId = weapon.UserId;
                    existing.CultivationRequired = weapon.CultivationRequired;

                    existing.Skill.Name = weapon.Skill.Name;
                    existing.Skill.Ref = weapon.Skill.Ref;
                }
            }

            if (toUpdate.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task SyncCharactersAsync()
        {
            // Load skills from skill.json
            var skillFilePath = Path.Combine(_dataPath, "skill.json");
            if (!File.Exists(skillFilePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file {skillFilePath}.");
            }

            var skillJson = await File.ReadAllTextAsync(skillFilePath);
            var skillData = JsonConvert.DeserializeObject<List<Skill>>(skillJson);

            if (skillData == null)
            {
                throw new InvalidOperationException("Không thể phân tích file skill.json.");
            }

            // Load characters from character.json
            var characterFilePath = Path.Combine(_dataPath, "character.json");
            if (!File.Exists(characterFilePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file {characterFilePath}.");
            }

            var characterJson = await File.ReadAllTextAsync(characterFilePath);
            var characters = JsonConvert.DeserializeObject<List<Character>>(characterJson);

            if (characters == null)
            {
                throw new InvalidOperationException("Không thể phân tích file character.json.");
            }

            var existingNames = await _context.Characters
                .AsNoTracking()
                .Select(c => c.Name)
                .ToListAsync();

            var toAdd = new List<Character>();
            var toUpdate = new List<Character>();

            foreach (var character in characters)
            {
                if (existingNames.Contains(character.Name))
                {
                    toUpdate.Add(character);
                }
                else
                {
                    character.Id = 0;
                    toAdd.Add(character);
                }
            }

            if (toAdd.Any())
            {
                var newCharacters = toAdd.Select(c => new Character
                {
                    Id = 0,
                    Name = c.Name,
                    Desc = c.Desc,
                    Faction = c.Faction,
                    Cultivation = c.Cultivation ?? "Nhập môn",
                    UserId = c.UserId != 0 ? c.UserId : 0,
                    HP = c.HP,
                    Qi = c.Qi,
                    Atk = c.Atk,
                    Skills = new List<Skill>() // Tạm thời để trống, sẽ gán sau
                }).ToList();

                await _context.Characters.AddRangeAsync(newCharacters);
                await _context.SaveChangesAsync();

                // Sau khi lưu Characters, gán Skills cho từng Character
                var savedCharacters = await _context.Characters
                    .Include(c => c.Skills)
                    .Where(c => newCharacters.Select(nc => nc.Name).Contains(c.Name))
                    .ToListAsync();

                foreach (var character in savedCharacters)
                {
                    var originalCharacter = characters.FirstOrDefault(c => c.Name == character.Name);
                    if (originalCharacter?.Skills == null) continue;

                    foreach (var skillRef in originalCharacter.Skills)
                    {
                        var skill = skillData.FirstOrDefault(s => s.Ref == skillRef.Ref);
                        if (skill != null)
                        {
                            var newSkill = new Skill
                            {
                                CharacterId = character.Id,
                                Name = skill.Name,
                                Ref = skill.Ref,
                                Desc = skill.Desc,
                                Effect = skill.Effect,
                                Cost = skill.Cost
                            };

                            // Kiểm tra xem skill đã tồn tại chưa
                            if (!character.Skills.Any(s => s.Ref == newSkill.Ref))
                            {
                                character.Skills.Add(newSkill);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Skill with ref '{skillRef.Ref}' not found in skill.json for character '{character.Name}'.");
                        }
                    }
                }

                await _context.SaveChangesAsync();

                existingNames = await _context.Characters
                    .AsNoTracking()
                    .Select(c => c.Name)
                    .ToListAsync();
            }

            foreach (var character in toUpdate)
            {
                var existing = await _context.Characters
                    .Include(c => c.Skills)
                    .FirstOrDefaultAsync(c => c.Name == character.Name);

                if (existing != null)
                {
                    existing.Name = character.Name;
                    existing.Desc = character.Desc;
                    existing.Faction = character.Faction;
                    existing.Cultivation = character.Cultivation ?? "Nhập môn";
                    existing.UserId = character.UserId != 0 ? character.UserId : 0;

                    // Xóa các kỹ năng cũ
                    _context.Skills.RemoveRange(existing.Skills);

                    // Thêm các kỹ năng mới
                    existing.Skills = new List<Skill>();
                    if (character.Skills != null)
                    {
                        foreach (var skillRef in character.Skills)
                        {
                            var skill = skillData.FirstOrDefault(s => s.Ref == skillRef.Ref);
                            if (skill != null)
                            {
                                var newSkill = new Skill
                                {
                                    CharacterId = existing.Id,
                                    Name = skill.Name,
                                    Ref = skill.Ref,
                                    Desc = skill.Desc,
                                    Effect = skill.Effect,
                                    Cost = skill.Cost
                                };
                                existing.Skills.Add(newSkill);
                            }
                            else
                            {
                                Console.WriteLine($"Skill with ref '{skillRef.Ref}' not found in skill.json for character '{character.Name}'.");
                            }
                        }
                    }
                }
            }

            if (toUpdate.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}