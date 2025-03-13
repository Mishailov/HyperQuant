using System.Text.Json;

namespace HyperQuantWebApi.Clients
{
    public class RestClient
    {
        private readonly HttpClient _httpClient;

        public RestClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api-pub.bitfinex.com/v2/")
            };
        }

        public async Task<List<List<JsonElement>>> GetJsonTradesAsync(string pair, int count)
        {
            var url = $"trades/t{pair.ToUpper()}/hist?limit={count}";

            return await GetAsync(url);
        }

        public async Task<List<List<JsonElement>>> GetJsonCandlesAsync(string pair, string period, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            var url = $"candles/trade%3A{period}%3At{pair.ToUpper()}/hist?";

            if (from.HasValue)
            {
                url += $"&start={from.Value.ToUnixTimeMilliseconds()}";
            }

            if (to.HasValue)
            {
                url += $"&end={to.Value.ToUnixTimeMilliseconds()}";
            }

            if (count.HasValue)
            {
                url += $"&limit={count}";
            }

            return await GetAsync(url);
        }

        #region for tests swagger
        public async Task<IEnumerable<Trade>> GetTradesAsync(string pair, int maxCount)
        {
            var url = $"trades/t{pair.ToUpper()}/hist?limit={maxCount}";

            var jsonElements = await GetAsync(url);

            return jsonElements.Select(trade => new Trade
            {
                Id = trade[0].ToString(),
                Pair = pair,
                Price = trade[3].GetDecimal(),
                Amount = trade[2].GetDecimal(),
                Side = trade[2].GetDecimal() >= 0 ? "buy" : "sell",
                Time = DateTimeOffset.FromUnixTimeMilliseconds(trade[1].GetInt64()),
            });
        }

        public async Task<IEnumerable<Candle>> GetCandlesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            var availableValue = ConvertSecToAvailableValue(periodInSec);

            var url = $"candles/trade%3A{availableValue}%3At{pair.ToUpper()}/hist?";

            if (from.HasValue)
            {
                url += $"&start={from.Value.ToUnixTimeMilliseconds()}";
            }

            if (to.HasValue)
            {
                url += $"&end={to.Value.ToUnixTimeMilliseconds()}";
            }

            if (count.HasValue)
            {
                url += $"&limit={count}";
            }

            var jsonElements = await GetAsync(url);

            return jsonElements.Select(candle => new Candle
            {
                Pair = pair,
                OpenPrice = candle[1].GetDecimal(),
                HighPrice = candle[3].GetDecimal(),
                LowPrice = candle[4].GetDecimal(),
                ClosePrice = candle[2].GetDecimal(),
                TotalVolume = candle[5].GetDecimal(),
                TotalPrice = candle[5].GetDecimal(),
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(candle[0].GetInt64()),
            });
        }
        #endregion

        private async Task<List<List<JsonElement>>> GetAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<List<JsonElement>>>(jsonResponse);
        }

        private string ConvertSecToAvailableValue(int sec)
        {
            var availableItems = new Dictionary<string, int>
            {
                { "1m", 60 },
                { "5m", 300 },
                { "15m", 900 },
                { "30m", 1800 },
                { "1h", 3600 },
                { "3h", 10800 },
                { "6h", 21600 },
                { "12h", 43200 },
                { "1D", 86400 },
                { "1W", 604800 },
                { "14D", 1209600 },
                { "1M", 2592000 }
            };

            string closeKey = string.Empty;
            int tempValue = availableItems.Last().Value + 1;

            foreach (var item in availableItems)
            {
                int dif = Math.Abs(sec - item.Value);
                if (dif < tempValue)
                {
                    tempValue = dif;
                    closeKey = item.Key;
                }
            }

            return closeKey;
        }
    }
}
