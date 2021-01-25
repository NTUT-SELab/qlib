using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces.SubClients;
using CommandLine;
using Magicodes.ExporterAndImporter.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Crawler
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunAsync);
        }

        static async Task RunAsync(Options opts)
        {
            if (CheckOptions(opts))
            {
                List<BinanceKline> klines = await GetKlinesAsync(opts) as List<BinanceKline>;
                await Export(opts, klines.ToQlibKlines(opts));
            }
        }

        static bool CheckOptions(Options opts)
        {
            opts.Market ??= "Spot";
            opts.StartTime ??= DateTime.Now.AddYears(-4);
            opts.ExportPath ??= Directory.GetCurrentDirectory();

            if (!Directory.Exists(opts.ExportPath))
                Directory.CreateDirectory(opts.ExportPath);

            if (!(opts.Market.ToLower() == "Spot".ToLower() ||
                opts.Market.ToLower() == "FuturesCoin".ToLower() ||
                opts.Market.ToLower() == "FuturesUsdt".ToLower()))
                return false;

            Console.WriteLine($"Symbol: {opts.Symbol}");
            Console.WriteLine($"Market: {opts.Market}");
            Console.WriteLine($"Start time: {opts.StartTime}");
            Console.WriteLine($"End time: {opts.EndTime}");
            Console.WriteLine($"Export path: {opts.ExportPath}");

            return true;
        }

        static async Task<IEnumerable<BinanceKline>> GetKlinesAsync(Options opts)
        {
            using BinanceClient client = new();
            List<BinanceKline> kLines = new();
            IBinanceClientMarket market = GetMarket(opts, client);

            while (opts.StartTime < GetEndTime(opts))
            {
                DateTime endTime = opts.StartTime.Value.AddHours(12);

                var klines = await market.GetKlinesAsync(opts.Symbol, KlineInterval.OneMinute, opts.StartTime, endTime, 1000);
                if (klines != null && klines.Success)
                {
                    kLines.AddRange(klines.Data.OrderBy(item => item.CloseTime).Select(item =>
                        new BinanceKline
                        {
                            BaseVolume = item.BaseVolume,
                            Close = item.Close,
                            CloseTime = item.CloseTime,
                            High = item.High,
                            Low = item.Low,
                            Open = item.Open,
                            OpenTime = item.OpenTime,
                            QuoteVolume = item.QuoteVolume,
                            TakerBuyBaseVolume = item.TakerBuyBaseVolume,
                            TakerBuyQuoteVolume = item.TakerBuyQuoteVolume,
                            TradeCount = item.TradeCount
                        }));

                    Console.WriteLine($"{opts.StartTime}: {klines.Data.Count()}");
                }

                await Task.Delay(3000);

                opts.StartTime = endTime;
            }

            Console.WriteLine("Download done.");

            return kLines;
        }

        static IBinanceClientMarket GetMarket(Options opts, BinanceClient client)
            => (opts.Market.ToLower()) switch
                {
                    "futurescoin" => client.FuturesCoin.Market,
                    "futuresusdt" => client.FuturesUsdt.Market,
                    _ => client.Spot.Market
                };

        static async Task Export(Options opts, ICollection<QlibKline> klines)
            => await new CsvExporter().Export($"./{opts.StockCode}.csv", klines);

        static DateTime GetEndTime(Options opts)
            => opts.EndTime == null ? DateTime.Now : opts.EndTime.Value;

        static ICollection<QlibKline> ToQlibKlines (this List<BinanceKline> klines, Options opts)
        {
            QlibKline[] qlibKlines = new QlibKline[klines.Count];

            Parallel.ForEach(klines, (kline, state, index) =>
            {
                qlibKlines[index] = index == 0 ? kline.ToQlibKline(opts, isFirst: true) : kline.ToQlibKline(opts, klines[Convert.ToInt32(index) - 1].Close);
            });

            return qlibKlines;
        }

        static QlibKline ToQlibKline(this BinanceKline kline, Options opts, decimal LastClose = default, bool isFirst = false)
            => new QlibKline
            {
                StockCode = opts.StockCode,
                Date = kline.OpenTime,
                Open = kline.Open,
                Close = kline.Close,
                High = kline.High,
                Low = kline.Low,
                Volume = kline.BaseVolume,
                Money = kline.QuoteVolume,
                Factor = 1,
                TradeCount = kline.TradeCount,
                TakerBuyBaseVolume = kline.TakerBuyBaseVolume,
                TakerBuyQuoteVolume = kline.TakerBuyQuoteVolume,
                Change = isFirst ? 0 : kline.Close - LastClose
            };
    }

    public class Options
    {
        [Option('s', "symbol", Required = true, HelpText = "BTCUSDT")]
        public string Symbol { get; set; }
#nullable enable
        [Option('m', "market", Required = false, HelpText = "Spot FuturesCoin FuturesUsdt")]
        public string? Market { get; set; }
        [Option('S', "start-time", Required = false, HelpText = "20/01/2020")]
        public DateTime? StartTime { get; set; }
        [Option('E', "end-time", Required = false, HelpText = "20/02/2020")]
        public DateTime? EndTime { get; set; }
        [Option('o', "output", Required = false, HelpText = "歷史資料輸出路徑")]
        public string? ExportPath { get; set; }
#nullable disable
        public string StockCode { get { return $"{Symbol}-{Market}"; } }
    }
}
