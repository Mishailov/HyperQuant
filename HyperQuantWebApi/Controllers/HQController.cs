using HyperQuantWebApi.Clients;
using Microsoft.AspNetCore.Mvc;

namespace HyperQuantWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HQController : ControllerBase
    {
        private readonly ILogger<HQController> _logger;
        private readonly RestClient _restClient;

        public HQController(ILogger<HQController> logger)
        {
            _logger = logger;
            _restClient = new RestClient();
        }

        [HttpGet("GetRestTrades")]
        public async Task<IEnumerable<Trade>> GetTradesAsync(string pair, int maxCount)
        {
            try
            {
                return await _restClient.GetTradesAsync(pair, maxCount);
            }
            catch (Exception e)
            {
                return new List<Trade>();
            }
        }

        [HttpGet("GetRestCandles")]
        public async Task<IEnumerable<Candle>> GetCandlesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            try
            {
                return await _restClient.GetCandlesAsync(pair, periodInSec, from, to, count);
            }
            catch (Exception e)
            {
                return new List<Candle>();
            }
        }
    }
}
