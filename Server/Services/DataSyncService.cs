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
            // Đồng bộ Effects trước (vì Cards tham chiếu đến EffectId)
            await SyncEffectsAsync();

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

        private async Task SyncEffectsAsync()
        {
            var filePath = Path.Combine(_dataPath, "effect.json");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file {filePath}.");
            }

            var json = await File.ReadAllTextAsync(filePath);
            var effects = JsonConvert.DeserializeObject<List<Effect>>(json);

            if (effects == null)
            {
                throw new InvalidOperationException("Không thể phân tích file effect.json.");
            }

            var existingRefs = await _context.Effects
                .AsNoTracking()
                .Select(e => e.Ref)
                .ToListAsync();

            var toAdd = new List<Effect>();
            var toUpdate = new List<Effect>();

            foreach (var effect in effects)
            {
                if (existingRefs.Contains(effect.Ref))
                {
                    toUpdate.Add(effect);
                }
                else
                {
                    effect.Id = 0; // Đặt Id = 0 để EF Core tự động tạo Id mới
                    toAdd.Add(effect);
                }
            }

            if (toAdd.Any())
            {
                await _context.Effects.AddRangeAsync(toAdd);
                await _context.SaveChangesAsync();
            }

            foreach (var effect in toUpdate)
            {
                var existing = await _context.Effects
                    .FirstOrDefaultAsync(e => e.Ref == effect.Ref);

                if (existing != null)
                {
                    existing.Name = effect.Name;
                    existing.Ref = effect.Ref;
                }
            }

            if (toUpdate.Any())
            {
                await _context.SaveChangesAsync();
            }
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
                }
            }

            if (toUpdate.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task SyncArmorsAsync()
        {
            try
            {
                var filePath = Path.Combine(_dataPath, "armor.json");
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Không tìm thấy file {filePath}.");
                }

                var json = await File.ReadAllTextAsync(filePath);
                Console.WriteLine($"Nội dung file armor.json: {json}");

                var armors = JsonConvert.DeserializeObject<List<Armor>>(json);
                if (armors == null)
                {
                    throw new InvalidOperationException("Không thể phân tích file armor.json.");
                }

                Console.WriteLine($"Số lượng armors: {armors.Count}");
                if (!armors.Any())
                {
                    throw new InvalidOperationException("Danh sách armors rỗng sau khi phân tích file armor.json.");
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
                    // Tạo danh sách các EquipmentSkill cần lưu
                    var skillsToAdd = toAdd
                        .Where(a => a.Skill != null && !string.IsNullOrEmpty(a.Skill.Ref))
                        .Select(a => new EquipmentSkill
                        {
                            Id = 0,
                            Name = a.Skill.Name ?? string.Empty,
                            Ref = a.Skill.Ref
                        })
                        .DistinctBy(s => s.Ref)
                        .ToList();

                    // Lưu các EquipmentSkill vào bảng EquipmentSkills
                    if (skillsToAdd.Any())
                    {
                        await _context.EquipmentSkills.AddRangeAsync(skillsToAdd);
                        await _context.SaveChangesAsync();
                    }

                    // Lấy lại danh sách skill vừa lưu để ánh xạ SkillId
                    var skillDict = await _context.EquipmentSkills
                        .AsNoTracking()
                        .ToDictionaryAsync(s => s.Ref, s => s.Id);

                    // Tạo danh sách Armor mới với SkillId hợp lệ
                    var newArmors = toAdd.Select(a => new Armor
                    {
                        Id = 0,
                        Name = a.Name ?? string.Empty,
                        Atk = a.Atk,
                        Def = a.Def,
                        Desc = a.Desc ?? string.Empty,
                        UserId = a.UserId,
                        CultivationRequired = a.CultivationRequired ?? "Nhập môn",
                        SkillId = a.Skill != null && !string.IsNullOrEmpty(a.Skill.Ref) && skillDict.ContainsKey(a.Skill.Ref)
                            ? skillDict[a.Skill.Ref]
                            : null,
                        Skill = null
                    }).ToList();

                    await _context.Armors.AddRangeAsync(newArmors);
                    await _context.SaveChangesAsync();

                    existingNames = await _context.Armors
                        .AsNoTracking()
                        .Select(a => a.Name)
                        .ToListAsync();
                }

                if (toUpdate.Any())
                {
                    foreach (var armor in toUpdate)
                    {
                        var existingArmor = await _context.Armors
                            .FirstOrDefaultAsync(a => a.Name == armor.Name);

                        if (existingArmor != null)
                        {
                            existingArmor.Atk = armor.Atk;
                            existingArmor.Def = armor.Def;
                            existingArmor.Desc = armor.Desc ?? string.Empty;
                            existingArmor.UserId = armor.UserId;
                            existingArmor.CultivationRequired = armor.CultivationRequired ?? "Nhập môn";

                            if (armor.Skill != null && !string.IsNullOrEmpty(armor.Skill.Ref))
                            {
                                var skill = await _context.EquipmentSkills
                                    .FirstOrDefaultAsync(s => s.Ref == armor.Skill.Ref);
                                if (skill == null)
                                {
                                    skill = new EquipmentSkill
                                    {
                                        Id = 0,
                                        Name = armor.Skill.Name ?? string.Empty,
                                        Ref = armor.Skill.Ref
                                    };
                                    _context.EquipmentSkills.Add(skill);
                                    await _context.SaveChangesAsync();
                                }
                                existingArmor.SkillId = skill.Id;
                            }
                            else
                            {
                                existingArmor.SkillId = null;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi trong SyncArmorsAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task SyncWeaponsAsync()
        {
            try
            {
                var filePath = Path.Combine(_dataPath, "weapon.json");
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Không tìm thấy file {filePath}.");
                }

                var json = await File.ReadAllTextAsync(filePath);
                Console.WriteLine($"Nội dung file weapon.json: {json}");

                var weapons = JsonConvert.DeserializeObject<List<Weapon>>(json);
                if (weapons == null)
                {
                    throw new InvalidOperationException("Không thể phân tích file weapon.json.");
                }

                Console.WriteLine($"Số lượng weapons: {weapons.Count}");
                if (!weapons.Any())
                {
                    throw new InvalidOperationException("Danh sách weapons rỗng sau khi phân tích file weapon.json.");
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
                    // Tạo danh sách các EquipmentSkill cần lưu
                    var skillsToAdd = toAdd
                        .Where(w => w.Skill != null && !string.IsNullOrEmpty(w.Skill.Ref)) // Chỉ lấy skill hợp lệ
                        .Select(w => new EquipmentSkill
                        {
                            Id = 0,
                            Name = w.Skill.Name ?? string.Empty,
                            Ref = w.Skill.Ref
                        })
                        .DistinctBy(s => s.Ref)
                        .ToList();

                    // Lưu các EquipmentSkill vào bảng EquipmentSkills
                    if (skillsToAdd.Any())
                    {
                        await _context.EquipmentSkills.AddRangeAsync(skillsToAdd);
                        await _context.SaveChangesAsync();
                    }

                    // Lấy lại danh sách skill vừa lưu để ánh xạ SkillId
                    var skillDict = await _context.EquipmentSkills
                        .AsNoTracking()
                        .ToDictionaryAsync(s => s.Ref, s => s.Id);

                    // Tạo danh sách Weapon mới với SkillId hợp lệ
                    var newWeapons = toAdd.Select(w => new Weapon
                    {
                        Id = 0,
                        Name = w.Name ?? string.Empty,
                        Atk = w.Atk,
                        Desc = w.Desc ?? string.Empty,
                        UserId = w.UserId,
                        CultivationRequired = w.CultivationRequired ?? "Nhập môn",
                        SkillId = w.Skill != null && !string.IsNullOrEmpty(w.Skill.Ref) && skillDict.ContainsKey(w.Skill.Ref)
                            ? skillDict[w.Skill.Ref]
                            : null,
                        Skill = null
                    }).ToList();

                    await _context.Weapons.AddRangeAsync(newWeapons);
                    await _context.SaveChangesAsync();

                    existingNames = await _context.Weapons
                        .AsNoTracking()
                        .Select(w => w.Name)
                        .ToListAsync();
                }

                if (toUpdate.Any())
                {
                    foreach (var weapon in toUpdate)
                    {
                        var existingWeapon = await _context.Weapons
                            .FirstOrDefaultAsync(w => w.Name == weapon.Name);

                        if (existingWeapon != null)
                        {
                            existingWeapon.Atk = weapon.Atk;
                            existingWeapon.Desc = weapon.Desc ?? string.Empty;
                            existingWeapon.UserId = weapon.UserId;
                            existingWeapon.CultivationRequired = weapon.CultivationRequired ?? "Nhập môn";

                            if (weapon.Skill != null && !string.IsNullOrEmpty(weapon.Skill.Ref))
                            {
                                var skill = await _context.EquipmentSkills
                                    .FirstOrDefaultAsync(s => s.Ref == weapon.Skill.Ref);
                                if (skill == null)
                                {
                                    skill = new EquipmentSkill
                                    {
                                        Id = 0,
                                        Name = weapon.Skill.Name ?? string.Empty,
                                        Ref = weapon.Skill.Ref
                                    };
                                    _context.EquipmentSkills.Add(skill);
                                    await _context.SaveChangesAsync();
                                }
                                existingWeapon.SkillId = skill.Id;
                            }
                            else
                            {
                                existingWeapon.SkillId = null;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi trong SyncWeaponsAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
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