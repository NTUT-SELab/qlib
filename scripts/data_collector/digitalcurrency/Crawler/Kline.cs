using Magicodes.ExporterAndImporter.Core;
using System;

namespace Crawler
{
    public class BinanceKline
    {
        [ExporterHeader(DisplayName = "OpenTime", Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime OpenTime { get; set; }

        [ExporterHeader(DisplayName = "Open")]
        public decimal Open { get; set; }

        [ExporterHeader(DisplayName = "High")]
        public decimal High { get; set; }

        [ExporterHeader(DisplayName = "Low")]
        public decimal Low { get; set; }

        [ExporterHeader(DisplayName = "Close")]
        public decimal Close { get; set; }

        [ExporterHeader(DisplayName = "BaseVolume")]
        public decimal BaseVolume { get; set; }

        [ExporterHeader(DisplayName = "CloseTime", Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime CloseTime { get; set; }

        [ExporterHeader(DisplayName = "QuoteVolume")]
        public decimal QuoteVolume { get; set; }

        [ExporterHeader(DisplayName = "TradeCount")]
        public int TradeCount { get; set; }

        [ExporterHeader(DisplayName = "TakerBuyBaseVolume")]
        public decimal TakerBuyBaseVolume { get; set; }

        [ExporterHeader(DisplayName = "TakerBuyQuoteVolume")]
        public decimal TakerBuyQuoteVolume { get; set; }
    }

    public class QlibKline
    {
        [ExporterHeader(DisplayName = "stock_code")]
        public string StockCode { get; set; }

        [ExporterHeader(DisplayName = "date", Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime Date { get; set; }

        [ExporterHeader(DisplayName = "open")]
        public decimal Open { get; set; }

        [ExporterHeader(DisplayName = "high")]
        public decimal High { get; set; }

        [ExporterHeader(DisplayName = "low")]
        public decimal Low { get; set; }

        [ExporterHeader(DisplayName = "close")]
        public decimal Close { get; set; }

        [ExporterHeader(DisplayName = "volume")]
        public decimal Volume { get; set; }

        [ExporterHeader(DisplayName = "money")]
        public decimal Money { get; set; }

        [ExporterHeader(DisplayName = "factor")]
        public decimal Factor { get; set; }

        [ExporterHeader(DisplayName = "change")]
        public decimal Change { get; set; }

        [ExporterHeader(DisplayName = "TradeCount")]
        public int TradeCount { get; set; }

        [ExporterHeader(DisplayName = "TakerBuyBaseVolume")]
        public decimal TakerBuyBaseVolume { get; set; }

        [ExporterHeader(DisplayName = "TakerBuyQuoteVolume")]
        public decimal TakerBuyQuoteVolume { get; set; }
    }
}
