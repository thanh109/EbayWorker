﻿using EbayWorker.Helpers;
using EbayWorker.Helpers.Base;
using EbayWorker.Models;
using EbayWorker.Views;
using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Forms = System.Windows.Forms;

namespace EbayWorker.ViewModels
{
    public class HomeViewModel: ViewModelBase
    {
        static object _syncLock;

        string _inputFilePath, _outputDirectoryPath, _executionTime;
        readonly string _settingsFileName;
        int _executedQueries;
        bool _cancelFlag;
        SettingsModel _settings;
        List<SearchModel> _searchQueries;
        Timer _timer;
        Stopwatch _stopWatch;
        
        CommandBase _cancelSearch, _selectInputFile, _selectOutputDirectory, _search, _showSearchQuery, _selectAllowedSellers, _selectRestrictedSellers, _clearAllowedSellers, _clearRestrictedSellers;

        static HomeViewModel()
        {
            _syncLock = new object();
        }

        public HomeViewModel()
        {
            _executionTime = "00:00:00";

            _settingsFileName = Path.Combine(Application.Current.GetStartupDirectory(), "settings.json");
            if (File.Exists(_settingsFileName))
                _settings = JsonConvert.DeserializeObject<SettingsModel>(File.ReadAllText(_settingsFileName));
        }

        ~HomeViewModel()
        {
            if (_settings == null || string.IsNullOrEmpty(_settingsFileName))
                return;

            try
            {
                File.WriteAllText(_settingsFileName, JsonConvert.SerializeObject(_settings));
            }
            catch
            {

            }
        }

        #region properties

        public SettingsModel Settings
        {
            get
            {
                if (_settings == null)
                    _settings = new SettingsModel();

                return _settings;
            }
            private set { _settings = value; }
        }

        public string ExecutionTime
        {
            get { return _executionTime; }
            private set { Set(nameof(ExecutionTime), ref _executionTime, value); }
        }

        public int ExecutedQueries
        {
            get { return _executedQueries; }
            private set { Set(nameof(ExecutedQueries), ref _executedQueries, value); }
        }

        public int MaxParallelQueries
        {
            get { return 20; }
        }

        public string InputFilePath
        {
            get { return _inputFilePath; }
            private set { Set(nameof(InputFilePath), ref _inputFilePath, value); }
        }

        public string OutputDirectoryPath
        {
            get { return _outputDirectoryPath; }
            private set { Set(nameof(OutputDirectoryPath), ref _outputDirectoryPath, value); }
        }

        public List<SearchModel> SearchQueries
        {
            get { return _searchQueries; }
            private set { Set(nameof(SearchQueries), ref _searchQueries, value); }
        }

        #endregion

        #region commands

        public CommandBase SelectInputFileCommand
        {
            get
            {
                if (_selectInputFile == null)
                    _selectInputFile = new RelayCommand(SelectInputFile);

                return _selectInputFile;
            }
        }

        public CommandBase SelectOutputDirectoryCommand
        {
            get
            {
                if (_selectOutputDirectory == null)
                {
                    _selectOutputDirectory = new RelayCommand(() => OutputDirectoryPath = SelectDirectory());
                    _selectOutputDirectory.IsSynchronous = true;
                }

                return _selectOutputDirectory;
            }
        }

        public CommandBase SearchCommand
        {
            get
            {
                if (_search == null)
                    _search = new RelayCommand(Search);

                return _search;
            }
        }

        public CommandBase ShowSearchQueryCommand
        {
            get
            {
                if (_showSearchQuery == null)
                {
                    _showSearchQuery = new RelayCommand<SearchModel>(ShowSearchQuery);
                    _showSearchQuery.IsSynchronous = true;
                }

                return _showSearchQuery;
            }
        }

        public CommandBase SelectAllowedSellersCommand
        {
            get
            {
                if (_selectAllowedSellers == null)
                    _selectAllowedSellers = new RelayCommand<object, HashSet<string>>(SelectSellers, (sellers) => Settings.Filter.AllowedSellers = sellers);

                return _selectAllowedSellers;
            }
        }

        public CommandBase SelectRestrictedSellersCommand
        {
            get
            {
                if (_selectRestrictedSellers == null)
                    _selectRestrictedSellers = new RelayCommand<object, HashSet<string>>(SelectSellers, (sellers) => Settings.Filter.RestrictedSellers = sellers);

                return _selectRestrictedSellers;
            }
        }

        public CommandBase ClearAllowedSellersCommand
        {
            get
            {
                if (_clearAllowedSellers == null)
                    _clearAllowedSellers = new RelayCommand(() => Settings.Filter.AllowedSellers = null);

                return _clearAllowedSellers;
            }
        }

        public CommandBase ClearRestrictedSellersCommand
        {
            get
            {
                if (_clearRestrictedSellers == null)
                    _clearRestrictedSellers = new RelayCommand(() => Settings.Filter.RestrictedSellers = null);

                return _clearRestrictedSellers;
            }
        }

        public CommandBase CancelSearchCommand
        {
            get
            {
                if (_cancelSearch == null)
                    _cancelSearch = new RelayCommand(() => _cancelFlag = true);

                return _cancelSearch;
            }
        }

        #endregion

        HashSet<string> SelectSellers(object parameter)
        {
            var fileName = SelectFile();
            if (string.IsNullOrEmpty(fileName))
                return null;

            var sellerNames = SplitData(fileName);
            var sellers = new HashSet<string>();
            foreach (var sellerName in sellerNames)
                if (!sellers.Contains(sellerName))
                    sellers.Add(sellerName);

            return sellers;
        }

        void ShowSearchQuery(SearchModel searchQuery)
        {
            var search = new SearchView(searchQuery);
            search.ShowDialog();
        }

        void StartTimer()
        {
            ExecutionTime = "00:00:00";
            ExecutedQueries = 0;

            if (_timer == null && _stopWatch == null)
            {
                _stopWatch = new Stopwatch();

                _timer = new Timer();
                _timer.Elapsed += (sender, e) =>
                {
                    var time = _stopWatch.Elapsed;
                    ExecutionTime = string.Format("{0:00}:{1:00}:{2:00}", time.Hours, time.Minutes, time.Seconds);
                };
                _timer.Interval = 500;
            }

            _stopWatch.Start();
            _timer.Start();
        }

        void StopTimer()
        {
            _stopWatch.Stop();
            _stopWatch.Reset();
            _timer.Stop();
        }

        void Search()
        {
            if (SearchQueries == null)
                return;

            StartTimer();

            // output file names
            string fileName, notCompletedFileName;
            if (string.IsNullOrEmpty(OutputDirectoryPath))
            {
                fileName = null;
                notCompletedFileName = null;
            }
            else
            {
                var fileTime = DateTime.Now.ToFileTimeUtc();
                fileName = Path.Combine(OutputDirectoryPath, string.Format("{0}.csv", fileTime));
                notCompletedFileName = Path.Combine(OutputDirectoryPath, string.Format("{0}_not_completed.txt", fileTime));
            }

            var parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = Settings.ParallelQueries;

            Parallel.ForEach(SearchQueries, parallelOptions, query =>
            {
                if (_cancelFlag)
                    return;

                var status = query.Status;
                if (status == SearchStatus.Working)
                    return;

                var parser = new HtmlDocument();
                if (Settings.FailedQueriesOnly)
                {
                    if (status != SearchStatus.Complete)
                        query.Search(ref parser, Settings.Filter, Settings.ParallelQueries, Settings.ScrapBooksInParallel);
                }                    
                else
                    query.Search(ref parser, Settings.Filter, Settings.ParallelQueries, Settings.ScrapBooksInParallel);

                WriteOutput(fileName, query);

                ExecutedQueries += 1;
            });

            // create file with search keywoards which failed to complete
            if (notCompletedFileName != null)
            {
                var notCompletedKeywoards = new StringBuilder();
                foreach (var query in SearchQueries)
                {
                    if (query.Status != SearchStatus.Complete)
                        notCompletedKeywoards.AppendLine(query.Keywoard);
                }
                if (notCompletedKeywoards.Length > 0)
                    File.WriteAllText(notCompletedFileName, notCompletedKeywoards.ToString());
            }

            if (Settings.ExcludeEmptyResults)
            {
                for(var index = SearchQueries.Count -1; index >=0; index--)
                {
                    var query = SearchQueries[index];
                    if (query.Status == SearchStatus.Complete && query.Books.Count == 0)
                        SearchQueries.RemoveAt(index);
                }
                RaisePropertyChanged(nameof(SearchQueries));
            }

            _cancelFlag = false;
            StopTimer();
        }

        void WriteOutput(string fileName, SearchModel query)
        {
            if (fileName == null || query.Status != SearchStatus.Complete)
                return;

            string contents;
            if (Settings.GroupByCondition)
                contents = query.Books.ToCsvStringGroupedByCondition(Settings.AddToPrice, Settings.IsAddToPricePercent);
            else if (Settings.GroupByStupidLogic)
                contents = query.Books.ToCsvStringGroupedByConditionStupidLogic(Settings.AddToPrice, query.Keywoard, Settings.IsAddToPricePercent);
            else
                contents = query.Books.ToCsvString(Settings.AddToPrice, Settings.IsAddToPricePercent);

            lock(_syncLock)
            {
                File.AppendAllText(fileName, contents);
            }
        }

        void SelectInputFile()
        {
            var fileName = SelectFile();
            if (string.IsNullOrEmpty(fileName))
                return;

            InputFilePath = fileName;

            var keywoards = SplitData(fileName);
            var searchQueries = new List<SearchModel>();
            foreach (var keywoard in keywoards)
            {
                var search = new SearchModel();
                search.Keywoard = keywoard;
                search.Category = Category.Books;
                searchQueries.Add(search);
            }
            SearchQueries = searchQueries;
            ExecutionTime = "00:00:00";
            ExecutedQueries = 0;
        }

        string SelectDirectory()
        {
            var directoryBrowser = new Forms.FolderBrowserDialog();
            directoryBrowser.ShowNewFolderButton = true;
            directoryBrowser.Description = "Select Directory";
            if (directoryBrowser.ShowDialog() == Forms.DialogResult.OK)
                return directoryBrowser.SelectedPath;

            return null;
        }

        string SelectFile()
        {
            var openFile = new OpenFileDialog();
            openFile.AddExtension = true;
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;
            openFile.Filter = "Text Files|*.txt|CSV Files|*.csv";
            openFile.FilterIndex = 0;
            openFile.Multiselect = false;
            if (openFile.ShowDialog() == true)
                return openFile.FileName;

            return null;
        }

        string[] SplitData(string fileName)
        {
            var fileInfo = new FileInfo(fileName);

            string[] keywoards = null;
            switch (fileInfo.Extension)
            {
                case ".txt":
                    keywoards = File.ReadAllLines(fileInfo.FullName);
                    break;

                case ".csv":
                    keywoards = File.ReadAllText(fileInfo.FullName).Split(',');
                    break;
            }
            return keywoards;
        }
    }
}
