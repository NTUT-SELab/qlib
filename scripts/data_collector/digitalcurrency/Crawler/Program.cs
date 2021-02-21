using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.SubClients;
using CommandLine;
using Magicodes.ExporterAndImporter.Csv;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Crawler
{
    static class Program
    {
        private static Options opts;
        private static ProgressBarOptions layer1BarOptions;
        private static ProgressBarOptions layer2BarOptions;
        private static ProgressBarOptions layer3BarOptions;

        static async Task Main(string[] args)
        {
            layer1BarOptions = new()
            {
                ForegroundColor = ConsoleColor.Yellow,
                ForegroundColorDone = ConsoleColor.DarkGreen,
                BackgroundColor = ConsoleColor.DarkGray,
                BackgroundCharacter = '\u2593'
            };
            layer2BarOptions = new()
            {
                DenseProgressBar = true,
                ProgressCharacter = '─',
                CollapseWhenFinished = false,
            };
            layer3BarOptions = new()
            {
                DenseProgressBar = true,
                CollapseWhenFinished = true,
                ProgressCharacter = '─',
                ForegroundColor = ConsoleColor.Cyan,
                ForegroundColorDone = ConsoleColor.DarkGreen,
                BackgroundColor = ConsoleColor.DarkGray,
            };

            await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunAsync);
        }

        static async Task RunAsync(Options opts)
        {
            if (!CheckOptions(opts))
                return;
            Program.opts = opts;
            
            if (opts.Symbol != null)
            {
                using var pbar = new ProgressBar(1000, $"加密貨幣爬蟲-{opts.Symbol}", layer1BarOptions);
                IProgress<float> progress = pbar.AsProgress<float>();
                List<BinanceKline> klines = await GetKlinesAsync(opts.Symbol, progress) as List<BinanceKline>;
                await Export(klines.ToQlibKlines(opts.Symbol), opts.Symbol);
            }
            else
            {
                IEnumerable<IBinance24HPrice> prices = await Get24HPricesAsync();
                if (opts.TradingChip != null)
                {
                    IEnumerable<string> symbols = GetCrawerSymbols(prices, opts.TradingChip);
                    using var pbar = new ProgressBar(1000, $"加密貨幣爬蟲-{opts.TradingChip}", layer1BarOptions);
                    await RunCrawerAsync(symbols, pbar);
                }
                else
                {
                    using var pbar = new ProgressBar(3, "加密貨幣爬蟲", layer1BarOptions);

                    IEnumerable<string> usdtSymbols = GetCrawerSymbols(prices, "USDT");
                    IEnumerable<string> btcSymbols = GetCrawerSymbols(prices, "BTC");
                    IEnumerable<string> ethSymbols = GetCrawerSymbols(prices, "ETH");
                    using var USDTBar = pbar.Spawn(usdtSymbols.Count(), "USDT", layer2BarOptions);
                    using var BTCBar = pbar.Spawn(btcSymbols.Count(), "BTC", layer2BarOptions);
                    using var ETHBar = pbar.Spawn(ethSymbols.Count(), "ETH", layer2BarOptions);
                    await RunCrawerAsync(usdtSymbols, USDTBar);
                    pbar.Tick();
                    await RunCrawerAsync(btcSymbols, BTCBar);
                    pbar.Tick();
                    await RunCrawerAsync(ethSymbols, ETHBar);
                    pbar.Tick();
                }
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

            if (opts.Symbol is not null)
                Console.WriteLine($"Symbol: {opts.Symbol}");
            else if (opts.TradingChip is not null && opts.ChangeRank > 0)
                Console.WriteLine($"Top {opts.ChangeRank} */{opts.TradingChip}");
            else if (opts.TradingChip is not null)
                Console.WriteLine($"All */{opts.TradingChip}");
            else if (opts.ChangeRank > 0)
                Console.WriteLine($"Top {opts.ChangeRank} */USDT */BTC */ETH");
            else
                Console.WriteLine($"All */USDT */BTC */ETH");
            Console.WriteLine($"Market: {opts.Market}");
            Console.WriteLine($"Start time: {opts.StartTime}");
            Console.WriteLine($"End time: {opts.EndTime}");
            Console.WriteLine($"Export path: {opts.ExportPath}");

            return true;
        }

        static IEnumerable<string> GetCrawerSymbols(IEnumerable<IBinance24HPrice> prices, string tradingChip)
        {
            Regex pattern = new Regex(@$"\S+{tradingChip}+$", RegexOptions.IgnoreCase);
            var maskPrices = prices.Where(item => pattern.IsMatch(item.Symbol));
            if (opts.ChangeRank > 0)
                return maskPrices.OrderByDescending(item => item.BaseVolume).Take((int)opts.ChangeRank).Select(item => item.Symbol);
            return maskPrices.Select(item => item.Symbol);
        }

        static async Task RunCrawerAsync(IEnumerable<string> symbols, IProgressBar progressBar)
        {
            foreach (var item in symbols.Select((value, index) => new { value, index }))
            {
                using var symbolBar = progressBar.Spawn(1000, item.value, layer3BarOptions);
                List<BinanceKline> klines = await GetKlinesAsync(item.value, symbolBar.AsProgress<float>()) as List<BinanceKline>;
                await Export(klines.ToQlibKlines(item.value), item.value);
                progressBar.Tick($"Finished {item.value} of {symbols.Count()}: {item.value}");
            }
        }

        static async Task<IEnumerable<BinanceKline>> GetKlinesAsync(string symbol, IProgress<float> progress)
        {
            using BinanceClient client = new();
            List<BinanceKline> kLines = new();
            IBinanceClientMarket market = GetMarket(client);
            DateTime startTime = opts.StartTime.Value;
            int totalDay = (GetEndTime() - startTime).Days;

            while (startTime < GetEndTime())
            {
                DateTime endTime = startTime.AddHours(12);

                var klines = await market.GetKlinesAsync(symbol, KlineInterval.OneMinute, startTime, endTime, 1000);
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
                }

                await Task.Delay(3000);

                int dayDiff = (GetEndTime() - startTime).Days;
                float dayDiffPercentage = (float)dayDiff / (float)totalDay;
                progress.Report(1 - dayDiffPercentage);

                startTime = endTime;
            }
            
            return kLines;
        }

        static IBinanceClientMarket GetMarket(BinanceClient client)
            => (opts.Market.ToLower()) switch
                {
                    "futurescoin" => client.FuturesCoin.Market,
                    "futuresusdt" => client.FuturesUsdt.Market,
                    _ => client.Spot.Market
                };

        static string GetStockCode(string symbol)
            => $"{symbol}-{opts.Market}";

        static async Task<IEnumerable<IBinance24HPrice>> Get24HPricesAsync()
        {
            using BinanceClient client = new();

            return (opts.Market.ToLower()) switch
            {
                "futurescoin" => (await client.FuturesCoin.Market.Get24HPricesAsync()).Data,
                "futuresusdt" => (await client.FuturesUsdt.Market.Get24HPricesAsync()).Data,
                _ => (await client.Spot.Market.Get24HPricesAsync()).Data
            };
        }

        static async Task Export(ICollection<QlibKline> klines, string symbol)
            => await new CsvExporter().Export($"./{GetStockCode(symbol)}.csv", klines);

        static DateTime GetEndTime()
            => opts.EndTime == null ? DateTime.Now : opts.EndTime.Value;

        static ICollection<QlibKline> ToQlibKlines (this List<BinanceKline> klines, string symbol)
        {
            QlibKline[] qlibKlines = new QlibKline[klines.Count];

            Parallel.ForEach(klines, (kline, state, index) =>
            {
                qlibKlines[index] = index == 0 ? kline.ToQlibKline(symbol, isFirst: true) : kline.ToQlibKline(symbol, klines[Convert.ToInt32(index) - 1].Close);
            });

            return qlibKlines;
        }

        static QlibKline ToQlibKline(this BinanceKline kline, string symbol, decimal LastClose = default, bool isFirst = false)
            => new QlibKline
            {
                StockCode = GetStockCode(symbol),
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
#nullable enable
        [Option('s', "symbol", Required = false, HelpText = "BTCUSDT")]
        public string? Symbol { get; set; }
        [Option('t', "trading-chip", Required = false, HelpText = "USDT BTC ETH")]
        public string? TradingChip { get; set; }
        [Option('m', "market", Required = false, HelpText = "Spot FuturesCoin FuturesUsdt")]
        public string? Market { get; set; }
        [Option('S', "start-time", Required = false, HelpText = "20/01/2020")]
        public DateTime? StartTime { get; set; }
        [Option('E', "end-time", Required = false, HelpText = "20/02/2020")]
        public DateTime? EndTime { get; set; }
        [Option('o', "output", Required = false, HelpText = "歷史資料輸出路徑")]
        public string? ExportPath { get; set; }
#nullable disable
        [Option('c', "change-rank", Required = false, HelpText = "歷史資料輸出路徑", Default = 0)]
        public uint ChangeRank { get; set; }
    }
}
