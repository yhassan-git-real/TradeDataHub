using System.Collections.Generic;
using System.Windows.Controls;
using TradeDataHub.Core.Models;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Controls;

namespace TradeDataHub.Core.Controllers
{
    /// <summary>
    /// Service for binding UI input controls to DTO objects
    /// </summary>
    public class InputBinder
    {
        /// <summary>
        /// Creates ExportInputs DTO from UI controls
        /// </summary>
        public static ExportInputs GetExportInputs(
            YearMonthPicker fromMonthPicker,
            YearMonthPicker toMonthPicker,
            TextBox portTextBox,
            TextBox hsTextBox,
            TextBox productTextBox,
            TextBox exporterTextBox,
            TextBox iecTextBox,
            TextBox forCountryTextBox,
            TextBox forNameTextBox)
        {
            return new ExportInputs(
                fromMonthPicker.SelectedYearMonth,
                toMonthPicker.SelectedYearMonth,
                ExportParameterHelper.ParseFilterList(portTextBox.Text),
                ExportParameterHelper.ParseFilterList(hsTextBox.Text),
                ExportParameterHelper.ParseFilterList(productTextBox.Text),
                ExportParameterHelper.ParseFilterList(exporterTextBox.Text),
                ExportParameterHelper.ParseFilterList(iecTextBox.Text),
                ExportParameterHelper.ParseFilterList(forCountryTextBox.Text),
                ExportParameterHelper.ParseFilterList(forNameTextBox.Text)
            );
        }

        /// <summary>
        /// Creates ImportInputs DTO from UI controls
        /// </summary>
        public static ImportInputs GetImportInputs(
            YearMonthPicker fromMonthPicker,
            YearMonthPicker toMonthPicker,
            TextBox portTextBox,
            TextBox hsTextBox,
            TextBox productTextBox,
            TextBox importerTextBox,
            TextBox iecTextBox,
            TextBox forCountryTextBox,
            TextBox forNameTextBox)
        {
            return new ImportInputs(
                fromMonthPicker.SelectedYearMonth,
                toMonthPicker.SelectedYearMonth,
                ImportParameterHelper.ParseFilterList(portTextBox.Text),
                ImportParameterHelper.ParseFilterList(hsTextBox.Text),
                ImportParameterHelper.ParseFilterList(productTextBox.Text),
                ImportParameterHelper.ParseFilterList(importerTextBox.Text),
                ImportParameterHelper.ParseFilterList(iecTextBox.Text),
                ImportParameterHelper.ParseFilterList(forCountryTextBox.Text),
                ImportParameterHelper.ParseFilterList(forNameTextBox.Text)
            );
        }
    }
}
