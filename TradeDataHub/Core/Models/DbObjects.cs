using System;
using System.Collections.Generic;

namespace TradeDataHub.Core.Models
{
    /// <summary>
    /// Represents a pair of database objects (View and Stored Procedure)
    /// </summary>
    public class DbObjectPair
    {
        /// <summary>
        /// The name of the view
        /// </summary>
        public string? ViewName { get; set; }

        /// <summary>
        /// The name of the stored procedure
        /// </summary>
        public string? StoredProcedureName { get; set; }

        public DbObjectPair(string? viewName, string? storedProcedureName)
        {
            ViewName = viewName;
            StoredProcedureName = storedProcedureName;
        }
    }

    /// <summary>
    /// Represents a database object option for dropdown selection
    /// </summary>
    public class DbObjectOption
    {
        /// <summary>
        /// The actual name of the database object
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The display name shown in the UI
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Optional order by column for views
        /// </summary>
        public string OrderByColumn { get; set; }

        public DbObjectOption(string name, string displayName, string orderByColumn = null)
        {
            Name = name;
            DisplayName = displayName;
            OrderByColumn = orderByColumn;
        }
    }
}