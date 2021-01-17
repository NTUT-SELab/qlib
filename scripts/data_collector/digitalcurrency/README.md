# Collect Data From Binance Exchange

## Requirements

```bash
pip install -r requirements.txt
```

## Collector Data

### Download Data
> Todo

### Sort data and Normalize data
```bash
python collector.py collector_data --source_dir ~/.qlib/digital_currency_data/source --sorted_source_dir  ~/.qlib/digital_currency_data/sorted_source --normalize_dir ~/.qlib/digital_currency_data/normalize --interval 30m
```

### Sort Data

```bash
python collector.py sort_data --source_dir ~/.qlib/digital_currency_data/ --sorted_source_dir  ~/.qlib/digital_currency_data/sorted_source source --interval 30m
```

### Normalize Data

```bash
python collector.py normalize_data --sorted_source_dir  ~/.qlib/digital_currency_data/sorted_source --normalize_dir ~/.qlib/digital_currency_data/normalize
```

### DumpBinData

```bash
python ../../dump_bin.py dump_all --csv_path ~/.qlib/digital_currency_data/normalize --qlib_dir ~/.qlib/qlib_data/my_data --freq 30m --include_fields open,close,high,low,volume,factor
# all feature [open,high,low,close,volume,quotevolume,takecount,takebuyvolume,takebuyquotevolume,factor]
```

### Help
```bash
pythono collector.py collector_data --help
```

## Parameters

- interval: [1m, 30m, 1h, 3h, 1d], default 30m
- dumpbin
    - freq: "day" or else str ## `day = "%Y-%m-%d"` else `"%Y-%m-%d %H:%M:%S"`