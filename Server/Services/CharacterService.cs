using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared.Models; // Giả sử model Character nằm trong Shared

namespace Server.Services
{
    public class CharacterService
    {
        private readonly AppDbContext _context;

        public CharacterService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Character>> GetCharactersAsync()
        {
            return await _context.Characters.ToListAsync();
        }

        // Bạn có thể thêm các phương thức khác như Create, Update, Delete
    }
}