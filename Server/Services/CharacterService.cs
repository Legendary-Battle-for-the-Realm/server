using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared.Models;

namespace Server.Services
{
    public class CharacterService
    {
        private readonly AppDbContext _context;

        public CharacterService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Character>> GetAllCharactersAsync()
        {
            return await _context.Characters.Include(c => c.Skills).ToListAsync();
        }

        public async Task<Character> GetCharacterByIdAsync(int id)
        {
            return await _context.Characters.Include(c => c.Skills).FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Character> CreateCharacterAsync(Character character)
        {
            _context.Characters.Add(character);
            await _context.SaveChangesAsync();
            return character;
        }

        public async Task<Character> UpdateCharacterAsync(int id, Character character)
        {
            var existing = await _context.Characters.FindAsync(id);
            if (existing == null) return null;

            existing.Name = character.Name;
            existing.Desc = character.Desc;
            existing.Atk = character.Atk;
            existing.Faction = character.Faction;
            existing.Cultivation = character.Cultivation;
            existing.HP = character.HP;
            existing.Qi = character.Qi;
            existing.Skills = character.Skills;
            existing.UserId = character.UserId;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteCharacterAsync(int id)
        {
            var character = await _context.Characters.FindAsync(id);
            if (character == null) return false;

            _context.Characters.Remove(character);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}