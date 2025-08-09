using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TradeDataHub.Core.Models;

namespace TradeDataHub.Features.Common.ViewModels
{
    /// <summary>
    /// ViewModel for database object selection in the UI
    /// </summary>
    public class DbObjectSelectorViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<DbObjectOption> _views;
        private ObservableCollection<DbObjectOption> _storedProcedures;
        private DbObjectOption _selectedView;
        private DbObjectOption _selectedStoredProcedure;

        /// <summary>
        /// Collection of available views
        /// </summary>
        public ObservableCollection<DbObjectOption> Views
        {
            get => _views;
            set
            {
                _views = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Collection of available stored procedures
        /// </summary>
        public ObservableCollection<DbObjectOption> StoredProcedures
        {
            get => _storedProcedures;
            set
            {
                _storedProcedures = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Currently selected view
        /// </summary>
        public DbObjectOption SelectedView
        {
            get => _selectedView;
            set
            {
                _selectedView = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Currently selected stored procedure
        /// </summary>
        public DbObjectOption SelectedStoredProcedure
        {
            get => _selectedStoredProcedure;
            set
            {
                _selectedStoredProcedure = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Initialize the ViewModel with default values
        /// </summary>
        public DbObjectSelectorViewModel()
        {
            Views = new ObservableCollection<DbObjectOption>();
            StoredProcedures = new ObservableCollection<DbObjectOption>();
        }

        /// <summary>
        /// Initialize the ViewModel with the provided collections
        /// </summary>
        public DbObjectSelectorViewModel(IEnumerable<DbObjectOption> views, IEnumerable<DbObjectOption> storedProcedures,
            string defaultViewName = null, string defaultStoredProcedureName = null)
        {
            Views = new ObservableCollection<DbObjectOption>(views);
            StoredProcedures = new ObservableCollection<DbObjectOption>(storedProcedures);

            // Set default selections
            if (!string.IsNullOrEmpty(defaultViewName))
            {
                SelectedView = Views.FirstOrDefault(v => v.Name == defaultViewName) ?? Views.FirstOrDefault();
            }
            else
            {
                SelectedView = Views.FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(defaultStoredProcedureName))
            {
                SelectedStoredProcedure = StoredProcedures.FirstOrDefault(sp => sp.Name == defaultStoredProcedureName) ?? StoredProcedures.FirstOrDefault();
            }
            else
            {
                SelectedStoredProcedure = StoredProcedures.FirstOrDefault();
            }
        }

        /// <summary>
        /// Get the current database object pair
        /// </summary>
        public DbObjectPair GetCurrentDbObjectPair()
        {
            return new DbObjectPair(
                SelectedView?.Name,
                SelectedStoredProcedure?.Name
            );
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}