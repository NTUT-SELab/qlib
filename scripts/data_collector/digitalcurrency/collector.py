import abc
import copy
import importlib
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed

import fire
import numpy as np
import pandas as pd
from tqdm import tqdm
from loguru import logger

CUR_DIR = Path(__file__).resolve().parent


class BinanceCollector:
    COLUMNS = ["open", "high", "low", "close", "volume", "money", "factor", "change", "tradecount", "takerbuyvolume", "takerbuyquotevolume"]
    """

    Parameters
    ----------
    source_dir: str or Path
        The directory where the raw data collected from the Internet is saved
    target_dir: str or Path
        Directory for normalize data
    interval: str
        freq, value from [1m, 3m, 15m, 30m, 1h, 3h, 1d], default 30m
    max_workers: int
        Concurrent number, default is 16
    """

    def __init__(self, source_dir: str or Path, target_dir: str or Path, interval: str = "30m", max_workers=16):
        if not (source_dir and target_dir):
            raise ValueError("source_dir and target_dir cannot be None")
        self._source_dir = Path(source_dir).expanduser()
        self._target_dir = Path(target_dir).expanduser()
        self._interval = interval
        self._max_workers = max_workers

    @abc.abstractmethod
    def download_data(self):
        raise NotImplementedError("")

    def sort_data(self):
        logger.info(f"Sort data by freq:{self._interval}.....")
        group_interval = {'1m': 'T', '3m': '3T', '15m': '15T', '30m': '30T', '1h': 'H', '3h': '3H', '1d': 'D'}

        def _sort_data(source_path: Path):
            columns = copy.deepcopy(self.COLUMNS)
            df = pd.read_csv(source_path)
            df.columns = ["stock_code", "date"] + columns
            df.set_index("date", inplace=True)
            df.index = pd.to_datetime(df.index)
            df = df.groupby(pd.Grouper(freq=group_interval.get(self._interval.lower(), '30T'))).agg({
                'stock_code': 'first',
                'open': 'first',
                'high': 'max',
                'low': 'min',
                'close': 'last',
                'volume': 'sum',
                'money': 'sum',
                'factor': 'mean',
                'change': 'sum',
                'tradecount': 'sum',
                'takerbuyvolume': 'sum',
                'takerbuyquotevolume': 'sum'
            })
            df.to_csv(self._target_dir.joinpath(source_path.name))

        with ThreadPoolExecutor(max_workers=self._max_workers) as worker:
            file_list = list(self._source_dir.glob("*.csv"))
            with tqdm(total=len(file_list)) as p_bar:
                for _ in worker.map(_sort_data, file_list):
                    p_bar.update()

    def normalize_data(self):
        logger.info("Normalize data.....")

        def _normalize(source_path: Path):
            columns = copy.deepcopy(self.COLUMNS)
            df = pd.read_csv(source_path)
            df.set_index("date", inplace=True)
            df.index = pd.to_datetime(df.index)
            df = df[~df.index.duplicated(keep="first")]
            df.sort_index(inplace=True)
            df.loc[(df["volume"] <= 0) | np.isnan(df["volume"]), set(df.columns)] = np.nan
            df.loc[(df["volume"] <= 0) | np.isnan(df["volume"]), columns] = np.nan
            df.index.names = ['date']
            df.loc[:, columns + ['stock_code']].to_csv(self._target_dir.joinpath(source_path.name))

        with ThreadPoolExecutor(max_workers=self._max_workers) as worker:
            file_list = list(self._source_dir.glob("*.csv"))
            with tqdm(total=len(file_list)) as p_bar:
                for _ in worker.map(_normalize, file_list):
                    p_bar.update()

    def manual_adj_data(self):
        """adjust data"""
        logger.info("Manual adjust data......")

        def _adj(file_path: Path):
            df = pd.read_csv(file_path)
            df = df.loc[:, ['date'] + self.COLUMNS + ['stock_code']]
            df.sort_values("date", inplace=True)
            df = df.set_index("date")
            df = df.loc[df.first_valid_index():]
            _close = df["close"].iloc[0]
            for _col in df.columns:
                if _col == "volume" or _col == "takerbuyvolume":
                    pass
                elif _col not in ["stock_code"]:
                    df[_col] = df[_col] / _close
                else:
                    pass
            df.reset_index().to_csv(self._target_dir.joinpath(file_path.name), index=False)

        with ThreadPoolExecutor(max_workers=self._max_workers) as worker:
            file_list = list(self._target_dir.glob("*.csv"))
            with tqdm(total=len(file_list)) as p_bar:
                for _ in worker.map(_adj, file_list):
                    p_bar.update()

    def normalize(self):
        self.normalize_data()
        self.manual_adj_data()


class Run:

    def __init__(self, source_dir=None, normalize_dir=None, sorted_source_dir=None, max_workers=4):
        """

        Parameters
        ----------
        source_dir: str
            The directory where the raw data collected from the Internet is saved, default "Path(__file__).parent/source"
        sorted_source_dir: str
            Directory for sorted source data, default "Path(__file__).parent/sorted_source"
        normalize_dir: str
            Directory for normalize data, default "Path(__file__).parent/normalize"
        max_workers: int
            Concurrent number, default is 4
        """
        if source_dir is None:
            source_dir = CUR_DIR.joinpath("source")
        self.source_dir = Path(source_dir).expanduser().resolve()
        self.source_dir.mkdir(parents=True, exist_ok=True)

        if sorted_source_dir is None:
            sorted_source_dir = CUR_DIR.joinpath("sorted_source")
        self.sorted_source_dir = Path(sorted_source_dir).expanduser().resolve()
        self.sorted_source_dir.mkdir(parents=True, exist_ok=True)

        if normalize_dir is None:
            normalize_dir = CUR_DIR.joinpath("normalize")
        self.normalize_dir = Path(normalize_dir).expanduser().resolve()
        self.normalize_dir.mkdir(parents=True, exist_ok=True)

        self._cur_module = importlib.import_module("collector")
        self.max_workers = max_workers

    def sort_data(
        self,
        interval="30m",
    ):
        """sort download data from Internet

        Parameters
        ----------
        interval: str
            freq, value from [1m, 3m, 15m, 30m, 1h, 3h, 1d], default 30m
        Examples
        ---------
            # get daily data
            $ python collector.py sort_data --source_dir ~/.qlib/digitalcurrency_data/source --interval 1d
            # get 30m data
            $ python collector.py sort_data --source_dir ~/.qlib/digitalcurrency_data/source --interval 30m
        """

        _class = getattr(self._cur_module, "BinanceCollector")
        _class(self.source_dir, self.sorted_source_dir, interval=interval, max_workers=self.max_workers).sort_data()

    def normalize_data(self):
        """normalize data

        Examples
        ---------
            $ python collector.py normalize_data --sorted_source_dir ~/.qlib/digitalcurrency_data/sorted_source --normalize_dir ~/.qlib/stock_data/normalize
        """
        _class = getattr(self._cur_module, "BinanceCollector")
        _class(self.sorted_source_dir, self.normalize_dir, self.max_workers).normalize()

    def collector_data(self, interval="30m"):
        """download -> normalize

        Parameters
        ----------
        interval: str
            freq, value from [1m, 3m, 15m, 30m, 1h, 3h, 1d], default 30m
        Examples
        -------
        python collector.py collector_data --source_dir ~/.qlib/digitalcurrency_data/source --interval 30m
        """
        self.sort_data(interval=interval)
        self.normalize_data()


if __name__ == "__main__":
    fire.Fire(Run)
