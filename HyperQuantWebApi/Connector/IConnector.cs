﻿namespace HyperQuantWebApi.Connector
{
    public interface IConnector
    {
        //#region Rest

        //Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);
        //Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0);

        //#endregion

        #region Socket


        event Action<Trade> NewBuyTrade;
        event Action<Trade> NewSellTrade;
        void SubscribeTrades(string pair, int maxCount = 100);
        void UnsubscribeTrades(string pair);

        event Action<Candle> CandleSeriesProcessing;
        void SubscribeCandles(string pair, int periodInSec, long? count = 0, DateTimeOffset? from = null, DateTimeOffset? to = null);
        void UnsubscribeCandles(string pair, int periodInSec);

        #endregion

    }
}
