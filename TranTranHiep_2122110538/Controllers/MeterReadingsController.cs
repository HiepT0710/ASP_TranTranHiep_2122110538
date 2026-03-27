using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeterReadingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MeterReadingsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MeterReading>>> GetMeterReadings()
        {
            return await _context.MeterReadings
                .Include(m => m.Contract)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MeterReading>> GetMeterReading(int id)
        {
            var meterReading = await _context.MeterReadings
                .Include(m => m.Contract)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meterReading == null)
            {
                return NotFound();
            }

            return meterReading;
        }

        [HttpPost]
        public async Task<ActionResult<MeterReading>> CreateMeterReading(MeterReading meterReading)
        {
            meterReading.Id = 0;
            if (!await _context.Contracts.AnyAsync(c => c.Id == meterReading.ContractId))
            {
                return BadRequest("ContractId khong ton tai.");
            }

            _context.MeterReadings.Add(meterReading);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMeterReading), new { id = meterReading.Id }, meterReading);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMeterReading(int id, MeterReading meterReading)
        {
            if (id != meterReading.Id)
            {
                return BadRequest("Id khong hop le.");
            }

            if (!await _context.Contracts.AnyAsync(c => c.Id == meterReading.ContractId))
            {
                return BadRequest("ContractId khong ton tai.");
            }

            _context.Entry(meterReading).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.MeterReadings.AnyAsync(m => m.Id == id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeterReading(int id)
        {
            var meterReading = await _context.MeterReadings.FindAsync(id);
            if (meterReading == null)
            {
                return NotFound();
            }

            _context.MeterReadings.Remove(meterReading);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
