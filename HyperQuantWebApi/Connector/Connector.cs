using HyperQuantWebApi.Clients;
using System;
using System.Text.Json;

namespace HyperQuantWebApi.Connector
{
    public class Connector : IConnector
    {
        private readonly RestClient _restClient;
        private readonly WebsocketClient _socketClient;

        private const string WebsocketUrl = "wss://api.bitfinex.com/ws/2";

        private readonly HashSet<string> _subscribedTrades = new();
        private readonly HashSet<string> _subscribedCandles = new();

        public Connector()
        {
            _restClient = new RestClient();
            _socketClient = new WebsocketClient(OnMessageReceived);
        }

        #region REST
        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            var jsonTrades = await _restClient.GetJsonTradesAsync(pair, maxCount);

            return jsonTrades.Select(trade => new Trade
            {
                Id = trade[0].ToString(),
                Pair = pair,
                Price = trade[3].GetDecimal(),
                Amount = trade[2].GetDecimal(),
                Side = trade[2].GetDecimal() >= 0 ? "buy" : "sell",
                Time = DateTimeOffset.FromUnixTimeMilliseconds(trade[1].GetInt64()),
            });
        }

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, long? count, DateTimeOffset? from, DateTimeOffset? to = null)
        {
            var availableValue = ConvertSecToAvailableValue(periodInSec);

            var jsonCandles = await _restClient.GetJsonCandlesAsync(pair, availableValue, from, to, count);

            return jsonCandles.Select(candle => new Candle
            {
                Pair = pair,
                OpenPrice = candle[1].GetDecimal(),
                HighPrice = candle[3].GetDecimal(),
                LowPrice = candle[4].GetDecimal(),
                ClosePrice = candle[2].GetDecimal(),
                TotalVolume = candle[5].GetDecimal(),
                TotalPrice = candle[5].GetDecimal(), //don't understand
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(candle[0].GetInt64()),
            });
        }

        #endregion

        #region Socket

        public event Action<Trade>? NewBuyTrade;
        public event Action<Trade>? NewSellTrade;
        public void SubscribeTrades(string pair, int maxCount = 100)
        {
            if (_subscribedTrades.Contains(pair))
            {
                return;
            }

            var request = $"{{\"event\": \"subscribe\", \"channel\": \"trades\", \"symbol\": \"{pair}\"}}";

            _socketClient.StartClientAsync(WebsocketUrl, request);

            _subscribedTrades.Add(pair);
        }

        public void UnsubscribeTrades(string pair)
        {
            if (!_subscribedTrades.Contains(pair))
            {
                return;
            }

            var request = $"{{\"event\": \"unsubscribe\", \"channel\": \"trades\", \"symbol\": \"{pair}\"}}";

            _socketClient.StartClientAsync(WebsocketUrl, request);

            _subscribedTrades.Remove(pair);
        }

        public event Action<Candle>? CandleSeriesProcessing;

        public void SubscribeCandles(string pair, int periodInSec, long? count = 0, DateTimeOffset? from = null, DateTimeOffset? to = null)
        {
            var availableValue = ConvertSecToAvailableValue(periodInSec);

            var candleKey = $"trade:{availableValue}:{pair}";

            if (_subscribedCandles.Contains(candleKey))
            {
                return;
            }

            var request = $"{{\"event\": \"subscribe\", \"channel\": \"candles\", \"key\": \"{candleKey}\"}}";

            _socketClient.StartClientAsync(WebsocketUrl, request);

            _subscribedCandles.Add(candleKey);
        }

        public void UnsubscribeCandles(string pair, int periodInSec)
        {
            var availableValue = ConvertSecToAvailableValue(periodInSec);

            var candleKey = $"trade:{availableValue}:{pair}";

            if (!_subscribedCandles.Contains(candleKey))
            {
                return;
            }

            var request = $"{{\"event\": \"unsubscribe\", \"channel\": \"candles\", \"key\": \"{candleKey}\"}}";

            _socketClient.StartClientAsync(WebsocketUrl, request);

            _subscribedCandles.Remove(candleKey);
        }

        #endregion

        #region private methods

        private void HandleTrade(JsonDocument json)
        {
            var jsonTrade = json.RootElement.EnumerateArray().Skip(2).First();

            var trade = new Trade
            {
                Id = jsonTrade[0].ToString(),
                Pair = "",
                Price = jsonTrade[3].GetDecimal(),
                Amount = jsonTrade[2].GetDecimal(),
                Side = jsonTrade[2].GetDecimal() >= 0 ? "buy" : "sell",
                Time = DateTimeOffset.FromUnixTimeMilliseconds(jsonTrade[1].GetInt64()),
            };

            if (trade.Side == "buy")
            {
                NewBuyTrade?.Invoke(trade);
            }
            else
            {
                NewSellTrade?.Invoke(trade);
            }
        }

        private void HandleCandle(JsonDocument json)
        {
            var jsonCandle = json.RootElement.EnumerateArray().Skip(2).First();

            var candle = new Candle
            {
                Pair = "",
                OpenPrice = jsonCandle[1].GetDecimal(),
                HighPrice = jsonCandle[3].GetDecimal(),
                LowPrice = jsonCandle[4].GetDecimal(),
                ClosePrice = jsonCandle[2].GetDecimal(),
                TotalVolume = jsonCandle[5].GetDecimal(),
                TotalPrice = jsonCandle[5].GetDecimal(),
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(jsonCandle[0].GetInt64()),
            };

            CandleSeriesProcessing?.Invoke(candle);
        }

        private void OnMessageReceived(string message)
        {
            if (message.Contains("tu"))
            {
                HandleTrade(JsonDocument.Parse(message));
            }
            else if (message.Contains("candles"))
            {
                HandleCandle(JsonDocument.Parse(message));
            }
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

        #endregion

    }
}
