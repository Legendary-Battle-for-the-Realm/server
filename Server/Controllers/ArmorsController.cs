using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared.Models;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArmorsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ArmorsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Armor>>> GetArmors()
        {
            return await _context.Armors.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Armor>> GetArmor(int id)
        {
            var armor = await _context.Armors.FindAsync(id);
            if (armor == null) return NotFound();
            return armor;
        }

        [HttpPost]
        public async Task<ActionResult<Armor>> CreateArmor(Armor armor)
        {
            _context.Armors.Add(armor);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetArmor), new { id = armor.Id }, armor);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArmor(int id, Armor armor)
        {
            if (id != armor.Id) return BadRequest();
            _context.Entry(armor).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArmor(int id)
        {
            var armor = await _context.Armors.FindAsync(id);
            if (armor == null) return NotFound();
            _context.Armors.Remove(armor);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}