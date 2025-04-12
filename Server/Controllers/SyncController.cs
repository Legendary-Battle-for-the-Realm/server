using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Controllers
{
    [Route("api")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly DataSyncService _dataSyncService;

        public SyncController(DataSyncService dataSyncService)
        {
            _dataSyncService = dataSyncService;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncAllData()
        {
            try
            {
                await _dataSyncService.SyncAllDataAsync();
                return Ok("Đồng bộ dữ liệu thành công.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi đồng bộ dữ liệu: {ex.Message}");
            }
        }
    }
}