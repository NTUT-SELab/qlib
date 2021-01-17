using Binance.Net;
using Binance.Net.Enums;
using CommandLine;
using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Crawler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunAsync);
        }

        static async Task RunAsync(Options opts)
        {
            if (CheckOptions(opts))
            {
                ICollection<BinanceKline> klines = await GetKlinesAsync(opts);
                await Export(opts, klines);
            }
        }

        static bool CheckOptions(Options opts)
        {
            opts.StartTime ??= DateTime.Now.AddYears(-4);
            opts.ExportPath ??= Directory.GetCurrentDirectory();

            if (!Directory.Exists(opts.ExportPath))
                Directory.CreateDirectory(opts.ExportPath);

            Console.WriteLine($"Symbol: {opts.Symbol}");
            Console.WriteLine($"Start time: {opts.StartTime}");
            Console.WriteLine($"End time: {opts.EndTime}");
            Console.WriteLine($"Export path: {opts.ExportPath}");

            return true;
        }

        static async Task<ICollection<BinanceKline>> GetKlinesAsync(Options opts)
        {
            using BinanceClient client = new();
            List<BinanceKline> kLines = new();

            while (opts.StartTime < GetEndTime(opts))
            {
                DateTime endTime = opts.StartTime.Value.AddHours(12);

                var klines = await client.Spot.Market.GetKlinesAsync(opts.Symbol, KlineInterval.OneMinute, opts.StartTime, endTime, 1000);
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

                await Task.Delay(10000);

                opts.StartTime = endTime;
            }

            Console.WriteLine("Download done.");

            return kLines;
        }

        static async Task Export(Options opts, ICollection<BinanceKline> klines)
        {
            IExporter exporter = new CsvExporter();

            var result = await exporter.Export($"./{opts.Symbol}.csv", klines);

            Console.WriteLine("Export done.");
        }

        static DateTime GetEndTime(Options opts)
            => opts.EndTime == null ? DateTime.Now : opts.EndTime.Value;
    }

    public class Options
    {
        [Option('s', "symbol", Required = true, HelpText = "BTCUSDT")]
        public string Symbol { get; set; }
        [Option('S', "start-time", Required = false, HelpText = "20/01/2020")]
#nullable enable
        public DateTime? StartTime { get; set; }
        [Option('E', "end-time", Required = false, HelpText = "20/02/2020")]
        public DateTime? EndTime { get; set; }
        [Option('o', "output", Required = false, HelpText = "歷史資料輸出路徑")]
        public string? ExportPath { get; set; }
#nullable disable
    }
}
