using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TradeDataHub.Controls
{
    public partial class YearMonthPicker : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty SelectedYearMonthProperty =
            DependencyProperty.Register(nameof(SelectedYearMonth), typeof(string), typeof(YearMonthPicker),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedYearMonthChanged));

        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(YearMonthPicker),
                new PropertyMetadata("Select Year/Month"));

        private bool _isUpdating = false;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<string>? YearMonthChanged;

        public string SelectedYearMonth
        {
            get => (string)GetValue(SelectedYearMonthProperty);
            set => SetValue(SelectedYearMonthProperty, value);
        }

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public YearMonthPicker()
        {
            InitializeComponent();
            Loaded += YearMonthPicker_Loaded;
        }

        private void YearMonthPicker_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeComboBoxes();
        }

        private void InitializeComboBoxes()
        {
            try
            {
                // Initialize years (2001 to 2100)
                var currentYear = DateTime.Now.Year;
                var years = new List<YearItem>();
                
                for (int year = 2001; year <= 2100; year++)
                {
                    years.Add(new YearItem { Year = year, DisplayText = year.ToString() });
                }
                
                YearComboBox.ItemsSource = years;
                YearComboBox.DisplayMemberPath = "DisplayText";
                YearComboBox.SelectedValuePath = "Year";

                // Initialize months with compact display
                var months = new List<MonthItem>
                {
                    new MonthItem { Month = 1, DisplayText = "01 - Jan" },
                    new MonthItem { Month = 2, DisplayText = "02 - Feb" },
                    new MonthItem { Month = 3, DisplayText = "03 - Mar" },
                    new MonthItem { Month = 4, DisplayText = "04 - Apr" },
                    new MonthItem { Month = 5, DisplayText = "05 - May" },
                    new MonthItem { Month = 6, DisplayText = "06 - Jun" },
                    new MonthItem { Month = 7, DisplayText = "07 - Jul" },
                    new MonthItem { Month = 8, DisplayText = "08 - Aug" },
                    new MonthItem { Month = 9, DisplayText = "09 - Sep" },
                    new MonthItem { Month = 10, DisplayText = "10 - Oct" },
                    new MonthItem { Month = 11, DisplayText = "11 - Nov" },
                    new MonthItem { Month = 12, DisplayText = "12 - Dec" }
                };

                MonthComboBox.ItemsSource = months;
                MonthComboBox.DisplayMemberPath = "DisplayText";
                MonthComboBox.SelectedValuePath = "Month";
                
                // Set default to current year and month for testing
                var currentDate = DateTime.Now;
                var currentYearItem = years.FirstOrDefault(y => y.Year == currentDate.Year);
                var currentMonthItem = months.FirstOrDefault(m => m.Month == currentDate.Month);
                
                if (currentYearItem != null)
                    YearComboBox.SelectedItem = currentYearItem;
                if (currentMonthItem != null)
                    MonthComboBox.SelectedItem = currentMonthItem;
                
                // Debug output
                System.Diagnostics.Debug.WriteLine($"YearMonthPicker initialized with {years.Count} years and {months.Count} months");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing YearMonthPicker: {ex.Message}");
            }
        }

        private static void OnSelectedYearMonthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is YearMonthPicker picker && !picker._isUpdating)
            {
                picker.UpdateFromYearMonth((string)e.NewValue);
            }
        }

        private void UpdateFromYearMonth(string yearMonth)
        {
            if (string.IsNullOrWhiteSpace(yearMonth) || yearMonth.Length != 6)
            {
                YearComboBox.SelectedItem = null;
                MonthComboBox.SelectedItem = null;
                return;
            }

            if (int.TryParse(yearMonth.Substring(0, 4), out int year) &&
                int.TryParse(yearMonth.Substring(4, 2), out int month))
            {
                _isUpdating = true;
                
                // Set year
                var yearItem = ((List<YearItem>)YearComboBox.ItemsSource)?.FirstOrDefault(y => y.Year == year);
                YearComboBox.SelectedItem = yearItem;
                
                // Set month
                var monthItem = ((List<MonthItem>)MonthComboBox.ItemsSource)?.FirstOrDefault(m => m.Month == month);
                MonthComboBox.SelectedItem = monthItem;
                
                _isUpdating = false;
            }
        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isUpdating)
            {
                UpdateSelectedYearMonth();
            }
        }

        private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isUpdating)
            {
                UpdateSelectedYearMonth();
            }
        }

        private void UpdateSelectedYearMonth()
        {
            if (YearComboBox.SelectedItem is YearItem yearItem && 
                MonthComboBox.SelectedItem is MonthItem monthItem)
            {
                _isUpdating = true;
                var yearMonth = $"{yearItem.Year:D4}{monthItem.Month:D2}";
                SelectedYearMonth = yearMonth;
                YearMonthChanged?.Invoke(this, yearMonth);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedYearMonth)));
                _isUpdating = false;
            }
            else if (YearComboBox.SelectedItem == null && MonthComboBox.SelectedItem == null)
            {
                _isUpdating = true;
                SelectedYearMonth = string.Empty;
                YearMonthChanged?.Invoke(this, string.Empty);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedYearMonth)));
                _isUpdating = false;
            }
        }

        public void Clear()
        {
            _isUpdating = true;
            YearComboBox.SelectedItem = null;
            MonthComboBox.SelectedItem = null;
            SelectedYearMonth = string.Empty;
            _isUpdating = false;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SelectedYearMonth) && SelectedYearMonth.Length == 6;
        }

        private class YearItem
        {
            public int Year { get; set; }
            public string DisplayText { get; set; } = string.Empty;
        }

        private class MonthItem
        {
            public int Month { get; set; }
            public string DisplayText { get; set; } = string.Empty;
        }
    }
}