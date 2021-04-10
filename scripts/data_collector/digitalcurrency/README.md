# Collect Data From Binance Exchange

## Requirements
Install [.Net 5](https://dotnet.microsoft.com/download/dotnet/5.0)
```bash
pip install -r requirements.txt
```

## Collector Data

### Download Data
```
-s, --symbol                BTCUSDT

-p, --principal-currency    本金貨幣: USDT BTC ETH

-m, --market                Spot FuturesCoin FuturesUsdt

-S, --start-time            20/01/2020

-E, --end-time              20/02/2020

-o, --output                歷史資料輸出路徑

-v, --volume-rank           (Default: 0) 交易量前 N 名

--help                      Display this help screen.

--version                   Display version information.
```
```
dotnet run -p .\Crawler\Crawler.csproj -- -S 01/16/2021 -E 01/17/2021 -o ~/.qlib/digital_currency_data/source
```

### Sort data and Normalize data
```bash
python collector.py collector_data --source_dir ~/.qlib/digital_currency_data/source --sorted_source_dir  ~/.qlib/digital_currency_data/sorted_source --normalize_dir ~/.qlib/digital_currency_data/normalize --interval 30m
```

### Sort Data

```bash
python collector.py sort_data --source_dir ~/.qlib/digital_currency_data/source --sorted_source_dir  ~/.qlib/digital_currency_data/sorted_source source --interval 30m
```

### Normalize Data

```bash
python collector.py normalize_data --sorted_source_dir  ~/.qlib/digital_currency_data/sorted_source --normalize_dir ~/.qlib/digital_currency_data/normalize
```

### DumpBinData

```bash
python ../../dump_bin.py dump_all --csv_path ~/.qlib/digital_currency_data/normalize --qlib_dir ~/.qlib/qlib_data/my_data --freq 30m --include_fields open,high,low,close,volume,money,factor,change,tradecount,takerbuyvolume,takerbuyquotevolume

# all feature open,high,low,close,volume,money,factor,change,tradecount,takerbuyvolume,takerbuyquotevolume
```

### Help
```bash
pythono collector.py collector_data --help
```

## Parameters

- interval: [1m, 3m, 15m, 30m, 1h, 3h, 1d], default 30m
- dumpbin
    - freq: "day" or else str ## `day = "%Y-%m-%d"` else `"%Y-%m-%d %H:%M:%S"`
