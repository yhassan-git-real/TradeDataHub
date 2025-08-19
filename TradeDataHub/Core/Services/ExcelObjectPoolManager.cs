using System;
using System.Collections.Concurrent;
using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using TradeDataHub.Config;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Manages pooled Excel objects to reduce allocation overhead and improve performance
    /// </summary>
    public class ExcelObjectPoolManager
    {
        private static readonly Lazy<ExcelObjectPoolManager> _instance = new(() => new ExcelObjectPoolManager());
        public static ExcelObjectPoolManager Instance => _instance.Value;

        private readonly ConcurrentQueue<ExcelPackage> _packagePool = new();
        private readonly ConcurrentDictionary<string, Color> _colorCache = new();
        private readonly ConcurrentDictionary<string, ExcelBorderStyle> _borderStyleCache = new();
        private readonly PerformanceSettings _performanceSettings;
        private readonly object _poolLock = new object();

        private const int MAX_POOLED_PACKAGES = 5; // Limit pool size to prevent memory buildup
        private int _pooledPackagesCount = 0;

        private ExcelObjectPoolManager()
        {
            _performanceSettings = ConfigurationCacheService.GetPerformanceSettings();
            InitializeCommonObjects();
        }

        /// <summary>
        /// Pre-initialize commonly used objects for better performance
        /// </summary>
        private void InitializeCommonObjects()
        {
            // Pre-cache common colors
            _colorCache.TryAdd("#4F81BD", ColorTranslator.FromHtml("#4F81BD")); // Default header color
            _colorCache.TryAdd("#FFFFFF", ColorTranslator.FromHtml("#FFFFFF")); // White
            _colorCache.TryAdd("#F0F0F0", ColorTranslator.FromHtml("#F0F0F0")); // Light gray
            _colorCache.TryAdd("#E0E0E0", ColorTranslator.FromHtml("#E0E0E0")); // Gray
            
            // Pre-cache border styles
            _borderStyleCache.TryAdd("thin", ExcelBorderStyle.Thin);
            _borderStyleCache.TryAdd("none", ExcelBorderStyle.None);
            _borderStyleCache.TryAdd("medium", ExcelBorderStyle.Medium);
            _borderStyleCache.TryAdd("thick", ExcelBorderStyle.Thick);
        }

        /// <summary>
        /// Gets a pooled ExcelPackage or creates a new one if none available
        /// </summary>
        public ExcelPackage GetExcelPackage()
        {
            if (!_performanceSettings.ExcelProcessing.EnableObjectPooling)
            {
                return new ExcelPackage();
            }

            if (_packagePool.TryDequeue(out var package))
            {
                lock (_poolLock)
                {
                    _pooledPackagesCount--;
                }
                
                // Reset package state - clear all worksheets
                for (int i = package.Workbook.Worksheets.Count - 1; i >= 0; i--)
                {
                    package.Workbook.Worksheets.Delete(i);
                }
                return package;
            }

            return new ExcelPackage();
        }

        /// <summary>
        /// Returns an ExcelPackage to the pool for reuse
        /// </summary>
        public void ReturnExcelPackage(ExcelPackage package)
        {
            if (!_performanceSettings.ExcelProcessing.EnableObjectPooling || package == null)
            {
                package?.Dispose();
                return;
            }

            lock (_poolLock)
            {
                if (_pooledPackagesCount < MAX_POOLED_PACKAGES)
                {
                    _packagePool.Enqueue(package);
                    _pooledPackagesCount++;
                    return;
                }
            }

            // Pool is full, dispose the package
            package.Dispose();
        }

        /// <summary>
        /// Gets a cached color or creates and caches it
        /// </summary>
        public Color GetCachedColor(string colorHtml)
        {
            return _colorCache.GetOrAdd(colorHtml, html => 
            {
                try
                {
                    return ColorTranslator.FromHtml(html);
                }
                catch
                {
                    // Return default color if parsing fails
                    return ColorTranslator.FromHtml("#4F81BD");
                }
            });
        }

        /// <summary>
        /// Gets a cached border style or creates and caches it
        /// </summary>
        public ExcelBorderStyle GetCachedBorderStyle(string? borderStyleName)
        {
            if (string.IsNullOrEmpty(borderStyleName))
                return ExcelBorderStyle.Thin;

            return _borderStyleCache.GetOrAdd(borderStyleName.ToLowerInvariant(), style =>
            {
                return style switch
                {
                    "none" => ExcelBorderStyle.None,
                    "thin" => ExcelBorderStyle.Thin,
                    "medium" => ExcelBorderStyle.Medium,
                    "thick" => ExcelBorderStyle.Thick,
                    _ => ExcelBorderStyle.Thin
                };
            });
        }

        /// <summary>
        /// Applies optimized formatting to a range using cached objects
        /// </summary>
        public void ApplyOptimizedRangeFormatting(ExcelRange range, string fontName, int fontSize, bool wrapText)
        {
            // Use single operation to set multiple properties
            var style = range.Style;
            style.Font.Name = fontName;
            style.Font.Size = fontSize;
            style.WrapText = wrapText;
        }

        /// <summary>
        /// Applies header formatting using cached objects
        /// </summary>
        public void ApplyHeaderFormatting(ExcelRange headerRange, string headerBackgroundColor, string? borderStyleName)
        {
            var style = headerRange.Style;
            
            // Font formatting
            style.Font.Bold = true;
            
            // Background color using cached color
            style.Fill.PatternType = ExcelFillStyle.Solid;
            style.Fill.BackgroundColor.SetColor(GetCachedColor(headerBackgroundColor));
            
            // Border formatting using cached border style
            var borderStyle = GetCachedBorderStyle(borderStyleName);
            style.Border.Top.Style = borderStyle;
            style.Border.Left.Style = borderStyle;
            style.Border.Right.Style = borderStyle;
            style.Border.Bottom.Style = borderStyle;
        }

        /// <summary>
        /// Applies data range border formatting using cached objects
        /// </summary>
        public void ApplyDataBorderFormatting(ExcelRange dataRange, string? borderStyleName)
        {
            var borderStyle = GetCachedBorderStyle(borderStyleName);
            var style = dataRange.Style;
            
            style.Border.Top.Style = borderStyle;
            style.Border.Left.Style = borderStyle;
            style.Border.Right.Style = borderStyle;
            style.Border.Bottom.Style = borderStyle;
        }

        /// <summary>
        /// Applies column-specific formatting efficiently
        /// </summary>
        public void ApplyColumnFormatting(ExcelWorksheet worksheet, System.Collections.Generic.IEnumerable<int> dateColumns, string dateFormat, 
                                        System.Collections.Generic.IEnumerable<int> textColumns, int colCount)
        {
            // Apply date formatting to specified columns
            if (dateColumns != null)
            {
                foreach (int dateCol in dateColumns)
                {
                    if (dateCol > 0 && dateCol <= colCount)
                    {
                        worksheet.Column(dateCol).Style.Numberformat.Format = dateFormat;
                    }
                }
            }
            
            // Apply text formatting to specified columns
            if (textColumns != null)
            {
                foreach (int textCol in textColumns)
                {
                    if (textCol > 0 && textCol <= colCount)
                    {
                        worksheet.Column(textCol).Style.Numberformat.Format = "@";
                    }
                }
            }
        }

        /// <summary>
        /// Performs optimized AutoFit operation with configurable sample size
        /// </summary>
        public void PerformOptimizedAutoFit(ExcelWorksheet worksheet, int lastRow, int colCount, int sampleRows)
        {
            if (sampleRows <= 0) return;
            
            int sampleEndRow = Math.Min(lastRow, sampleRows);
            var sampleRange = worksheet.Cells[1, 1, sampleEndRow, colCount];
            
            sampleRange.AutoFitColumns();
        }

        /// <summary>
        /// Clears all cached objects and disposes pooled resources
        /// </summary>
        public void ClearCache()
        {
            _colorCache.Clear();
            _borderStyleCache.Clear();
            
            // Dispose all pooled packages
            while (_packagePool.TryDequeue(out var package))
            {
                package.Dispose();
            }
            
            lock (_poolLock)
            {
                _pooledPackagesCount = 0;
            }
        }

        /// <summary>
        /// Gets pool statistics for monitoring
        /// </summary>
        public (int PooledPackages, int CachedColors, int CachedBorderStyles) GetPoolStatistics()
        {
            lock (_poolLock)
            {
                return (_pooledPackagesCount, _colorCache.Count, _borderStyleCache.Count);
            }
        }
    }
}
