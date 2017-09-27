﻿using EbayWorker.Helpers;
using EbayWorker.Helpers.Base;
using EbayWorker.Models;
using EbayWorker.Views;
using HtmlAgilityPack;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Forms = System.Windows.Forms;

namespace EbayWorker.ViewModels
{
    public class HomeViewModel: ViewModelBase
    {
        string _inputFilePath, _outputDirectoryPath, _executionTime;
        int _parallelQueries, _executedQueries;
        bool _failedQueriesOnly, _scrapBooksInParallel;
        SearchFilter _filter;
        List<SearchModel> _searchQueries;

        Timer _timer;
        Stopwatch _stopWatch;
        
        CommandBase _selectInputFile, _selectOutputDirectory, _search, _showSearchQuery, _selectAllowedSellers, _selectRestrictedSellers, _clearAllowedSellers, _clearRestrictedSellers;

        public HomeViewModel()
        {
            _parallelQueries = 5;
            _executionTime = "00:00:00";
        }

        #region properties

        public string ExecutionTime
        {
            get { return _executionTime; }
            private set { Set("ExecutionTime", ref _executionTime, value); }
        }

        public int ExecutedQueries
        {
            get { return _executedQueries; }
            private set { Set("ExecutedQueries", ref _executedQueries, value); }
        }

        public int MaxParallelQueries
        {
            get { return 20; }
        }

        public SearchFilter Filter
        {
            get
            {
                if (_filter == null)
                    _filter = new SearchFilter();

                return _filter;
            }
        }

        public string InputFilePath
        {
            get { return _inputFilePath; }
            private set { Set("InputFilePath", ref _inputFilePath, value); }
        }

        public string OutputDirectoryPath
        {
            get { return _outputDirectoryPath; }
            private set { Set("OutputDirectoryPath", ref _outputDirectoryPath, value); }
        }

        public int ParallelQueries
        {
            get { return _parallelQueries; }
            set { Set("ParallelQueries", ref _parallelQueries, value); }
        }

        public List<SearchModel> SearchQueries
        {
            get { return _searchQueries; }
            private set { Set("SearchQueries", ref _searchQueries, value); }
        }

        public bool FailedQueriesOnly
        {
            get { return _failedQueriesOnly; }
            set { Set("FailedQueriesOnly", ref _failedQueriesOnly, value); }
        }

        public bool ScrapBooksInParallel
        {
            get { return _scrapBooksInParallel; }
            set { Set("ScrapBooksInParallel", ref _scrapBooksInParallel, value); }
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
                    _selectAllowedSellers = new RelayCommand<object, HashSet<string>>(SelectSellers, (sellers) => Filter.AllowedSellers = sellers);

                return _selectAllowedSellers;
            }
        }

        public CommandBase SelectRestrictedSellersCommand
        {
            get
            {
                if (_selectRestrictedSellers == null)
                    _selectRestrictedSellers = new RelayCommand<object, HashSet<string>>(SelectSellers, (sellers) => Filter.RestrictedSellers = sellers);

                return _selectRestrictedSellers;
            }
        }

        public CommandBase ClearAllowedSellersCommand
        {
            get
            {
                if (_clearAllowedSellers == null)
                    _clearAllowedSellers = new RelayCommand(() => Filter.AllowedSellers = null);

                return _clearAllowedSellers;
            }
        }

        public CommandBase ClearRestrictedSellersCommand
        {
            get
            {
                if (_clearRestrictedSellers == null)
                    _clearRestrictedSellers = new RelayCommand(() => Filter.RestrictedSellers = null);

                return _clearRestrictedSellers;
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

            var parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = ParallelQueries;

            Parallel.ForEach(SearchQueries, parallelOptions, query =>
            {
                var status = query.Status;
                if (status == SearchStatus.Working)
                    return;

                var parser = new HtmlDocument();
                if (FailedQueriesOnly)
                {
                    if (status != SearchStatus.Complete)
                        query.Search(ref parser, Filter, ParallelQueries, ScrapBooksInParallel);
                }                    
                else
                    query.Search(ref parser, Filter, ParallelQueries, ScrapBooksInParallel);

                ExecutedQueries += 1;
            });

            StopTimer();
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
