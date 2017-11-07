using EbayWorker.Helpers.Base;
using Newtonsoft.Json;

namespace EbayWorker.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SettingsModel: NotificationBase
    {
        int _parallelQueries;
        bool _failedQueriesOnly, _scrapBooksInParallel, _excludeEmptyResults, _groupByCondition, _groupByStupidLogic, _isAddToPricePercent;
        decimal _addToPrice;
        SearchFilter _filter;

        public SettingsModel()
        {
            _parallelQueries = 5;

            // TODO: remove this option to make app generic
            _groupByStupidLogic = true;
        }

        [JsonProperty]
        public SearchFilter Filter
        {
            get
            {
                if (_filter == null)
                {
                    _filter = new SearchFilter();
                    _filter.LoadDefaults();
                }

                return _filter;
            }
            set { _filter = value; }
        }

        [JsonProperty]
        public decimal AddToPrice
        {
            get { return _addToPrice; }
            set { Set(nameof(AddToPrice), ref _addToPrice, value); }
        }

        [JsonProperty]
        public bool IsAddToPricePercent
        {
            get { return _isAddToPricePercent; }
            set { Set(nameof(IsAddToPricePercent), ref _isAddToPricePercent, value); }
        }

        [JsonProperty]
        public int ParallelQueries
        {
            get { return _parallelQueries; }
            set { Set(nameof(ParallelQueries), ref _parallelQueries, value); }
        }

        [JsonProperty]
        public bool FailedQueriesOnly
        {
            get { return _failedQueriesOnly; }
            set { Set(nameof(FailedQueriesOnly), ref _failedQueriesOnly, value); }
        }

        [JsonProperty]
        public bool ScrapBooksInParallel
        {
            get { return _scrapBooksInParallel; }
            set { Set(nameof(ScrapBooksInParallel), ref _scrapBooksInParallel, value); }
        }

        [JsonProperty]
        public bool ExcludeEmptyResults
        {
            get { return _excludeEmptyResults; }
            set { Set(nameof(ExcludeEmptyResults), ref _excludeEmptyResults, value); }
        }

        [JsonProperty]
        public bool GroupByCondition
        {
            get { return _groupByCondition; }
            set { Set(nameof(GroupByCondition), ref _groupByCondition, value); }
        }

        [JsonProperty]
        public bool GroupByStupidLogic
        {
            get { return _groupByStupidLogic; }
            set { Set(nameof(GroupByStupidLogic), ref _groupByStupidLogic, value); }
        }
    }
}
