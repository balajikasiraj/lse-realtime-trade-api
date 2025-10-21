using Lse.Application.Services;
using Lse.Domain;
using Lse.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Lse.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TradesController : ControllerBase
    {
        private readonly ITradeService _service;

        public TradesController(ITradeService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Trade trade)
        {
            await _service.RecordTradeAsync(trade); 
            return Accepted(new ApiResponse<object>
            {
                Success = true,
                Message = "Trade recorded",
                StatusCode = 202
            });
        }

        [HttpGet("{ticker}")]
        public async Task<IActionResult> Get(string ticker)
        {
            var val = await _service.GetCurrentValueAsync(ticker);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { ticker, value = val },
                StatusCode = 200
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vals = await _service.GetAllCurrentValuesAsync();
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = vals,
                StatusCode = 200
            });
        }

        [HttpPost("range")]
        public async Task<IActionResult> GetRange([FromBody] string[] tickers)
        {
            var vals = await _service.GetCurrentValuesAsync(tickers);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = vals,
                StatusCode = 200
            });
        }
    }
}
