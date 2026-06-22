using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Project.Models
{
    public class SteamGame : INotifyPropertyChanged
    {
        public string AppId { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string StoreUrl { get; set; }

        public string Price { get; set; }
        public string OriginalPrice { get; set; }
        public string Discount { get; set; }
        public string CurrentPrice { get; set; }
        public string DiscountPercent { get; set; }

        public double PriceValue { get; set; }
        public int DiscountValue { get; set; }

        public string Description { get; set; }
        public string TrailerUrl { get; set; }
        public string MetacriticScore { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Screenshots { get; set; } = new List<string>();

        public bool IsBlocked { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}