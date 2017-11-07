﻿using EbayWorker.Helpers.Base;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace EbayWorker.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SearchFilter: NotificationBase
    {
        long _feedbackScore;
        decimal _feedbackPercent, _priceMaximum, _priceMinimum;
        bool _checkFeedbackScore, _checkFeedbakcPercent, _checkAllowedSellers, _checkRestrictedSellers, _isAuction, _isBuyItNow, _isClassifiedAds, _isPriceFiltered;
        HashSet<string> _allowedSellerNames, _restrictedSellerNames;
        string _location;
        readonly Dictionary<string, int> _locations;

        public SearchFilter()
        {
            _locations = new Dictionary<string, int>
            {
                { "United States", 1 },
                { "Canada", 2 },
                { "United Kingdom", 3 }
            };
        }

        #region properties

        public IEnumerable<string> Locations
        {
            get { return _locations.Keys; }
        }

        [JsonProperty]
        public string Location
        {
            get { return _location; }
            set { Set(nameof(Location), ref _location, value); }
        }

        [JsonProperty]
        public bool CheckFeedbackScore
        {
            get { return _checkFeedbackScore; }
            set { Set(nameof(CheckFeedbackScore), ref _checkFeedbackScore, value); }
        }

        [JsonProperty]
        public long FeedbackScore
        {
            get { return _feedbackScore; }
            set { Set(nameof(FeedbackScore), ref _feedbackScore, value); }
        }

        [JsonProperty]
        public bool CheckFeedbackPercent
        {
            get { return _checkFeedbakcPercent; }
            set { Set(nameof(CheckFeedbackPercent), ref _checkFeedbakcPercent, value); }
        }

        [JsonProperty]
        public decimal FeedbackPercent
        {
            get { return _feedbackPercent; }
            set
            {
                if (value < 0m)
                    value = 0m;
                else if (value > 99.99m)
                    value = 99.99m;

                Set(nameof(FeedbackPercent), ref _feedbackPercent, value);
            }
        }

        [JsonProperty]
        public bool IsPriceFiltered
        {
            get { return _isPriceFiltered; }
            set { Set(nameof(IsPriceFiltered), ref _isPriceFiltered, value); }
        }

        [JsonProperty]
        public decimal MinimumPrice
        {
            get { return _priceMinimum; }
            set { Set(nameof(MinimumPrice), ref _priceMinimum, value); }
        }

        [JsonProperty]
        public decimal MaximumPrice
        {
            get { return _priceMaximum; }
            set { Set(nameof(MaximumPrice), ref _priceMaximum, value); }
        }

        [JsonProperty]
        public bool CheckAllowedSellers
        {
            get { return _checkAllowedSellers; }
            set { Set(nameof(CheckAllowedSellers), ref _checkAllowedSellers, value); }
        }

        [JsonProperty]
        public HashSet<string> AllowedSellers
        {
            get { return _allowedSellerNames; }
            set { Set(nameof(AllowedSellers), ref _allowedSellerNames, value); }
        }

        [JsonProperty]
        public bool CheckRestrictedSellers
        {
            get { return _checkRestrictedSellers; }
            set { Set(nameof(CheckRestrictedSellers), ref _checkRestrictedSellers, value); }
        }

        [JsonProperty]
        public HashSet<string> RestrictedSellers
        {
            get { return _restrictedSellerNames; }
            set { Set(nameof(RestrictedSellers), ref _restrictedSellerNames, value); }
        }

        [JsonProperty]
        public bool IsAuction
        {
            get { return _isAuction; }
            set
            {
                Set(nameof(IsAuction), ref _isAuction, value);
                if (value)
                    IsClassifiedAds = false;
            }
        }

        [JsonProperty]
        public bool IsBuyItNow
        {
            get { return _isBuyItNow; }
            set
            {
                Set(nameof(IsBuyItNow), ref _isBuyItNow, value);
                if (value)
                    IsClassifiedAds = false;
            }
        }

        [JsonProperty]
        public bool IsClassifiedAds
        {
            get { return _isClassifiedAds; }
            set
            {
                Set(nameof(IsClassifiedAds), ref _isClassifiedAds, value);
                if (value)
                {
                    IsBuyItNow = false;
                    IsAuction = false;
                }
            }
        }

        #endregion

        internal void LoadDefaults()
        {
            _location = "United States";
            _isBuyItNow = true;
        }

        internal int GetLocation()
        {
            if (string.IsNullOrEmpty(Location))
                return 0;

            return _locations[Location];
        }
    }
}
