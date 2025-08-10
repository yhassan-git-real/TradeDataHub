using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using TradeDataHub.Core.Cancellation;
using TradeDataHub.Core.Controllers;
using TradeDataHub.Core.Database;
using TradeDataHub.Core.Models;
using TradeDataHub.Core.Validation;
using TradeDataHub.Features.Common.ViewModels;
using TradeDataHub.Features.Export;
using TradeDataHub.Features.Export.Services;
using TradeDataHub.Features.Import;
using TradeDataHub.Features.Import.Services;
using TradeDataHub.Features.Monitoring.Services;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Container for managing all application services and their dependencies
    /// </summary>
    public class ServiceContainer
    {
        // Core Services
        public ExportExcelService ExcelService { get; private set; } = null!;
        public ImportExcelService ImportService { get; private set; } = null!;
        public ICancellationManager CancellationManager { get; private set; } = null!;
        public MonitoringService MonitoringService { get; private set; } = null!;

        // Validation Services
        public ExportObjectValidationService ExportObjectValidationService { get; private set; } = null!;
        public ImportObjectValidationService ImportObjectValidationService { get; private set; } = null!;
        public IParameterValidator ParameterValidator { get; private set; } = null!;
        public DatabaseObjectValidator DatabaseObjectValidator { get; private set; } = null!;
        public IValidationService ValidationService { get; private set; } = null!;
        public IResultProcessorService ResultProcessorService { get; private set; } = null!;

        // Controllers
        public IExportController ExportController { get; private set; } = null!;
        public IImportController ImportController { get; private set; } = null!;
        public IUIService UIService { get; private set; } = null!;
        public IMenuService MenuService { get; private set; } = null!;
        public IUIActionService UIActionService { get; private set; } = null!;
        public IViewStateService ViewStateService { get; private set; } = null!;

        // View Models
        public DbObjectSelectorViewModel ExportDbObjectViewModel { get; private set; } = null!;
        public DbObjectSelectorViewModel ImportDbObjectViewModel { get; private set; } = null!;

        /// <summary>
        /// Initialize all services with their dependencies
        /// </summary>
        public void InitializeServices(System.Windows.Window window)
        {
            try
            {
                // Initialize core services
                ExcelService = new ExportExcelService();
                ImportService = new ImportExcelService();
                CancellationManager = new CancellationManager();
                MonitoringService = MonitoringService.Instance;
                
                // Initialize validation services
                ExportObjectValidationService = new ExportObjectValidationService(ExcelService.ExportSettings);
                ImportObjectValidationService = new ImportObjectValidationService(ImportService.ImportSettings);
                ParameterValidator = new ParameterValidator();
                ValidationService = new ValidationService(ParameterValidator, ExportObjectValidationService, ImportObjectValidationService);
                ResultProcessorService = new ResultProcessorService(window.Dispatcher);
                
                // Initialize controllers
                ExportController = new ExportController(
                    ExcelService,
                    ValidationService,
                    ResultProcessorService,
                    MonitoringService,
                    window.Dispatcher);
                    
                ImportController = new ImportController(
                    ImportService,
                    ValidationService,
                    ResultProcessorService,
                    MonitoringService,
                    window.Dispatcher);
                
                // Initialize database object validator
                var dbSettings = LoadSharedDatabaseSettings();
                DatabaseObjectValidator = new DatabaseObjectValidator(dbSettings.ConnectionString);
                
                // Initialize services
                UIService = new UIService(DatabaseObjectValidator, MonitoringService);
                MenuService = new MenuService();
                UIActionService = new UIActionService(ExportController, ImportController, MonitoringService);
                ViewStateService = new ViewStateService(MonitoringService);
                
                // Initialize view models for database object selection
                ExportDbObjectViewModel = new DbObjectSelectorViewModel(
                    ExportObjectValidationService.GetAvailableViews(),
                    ExportObjectValidationService.GetAvailableStoredProcedures(),
                    ExportObjectValidationService.GetDefaultViewName(),
                    ExportObjectValidationService.GetDefaultStoredProcedureName());
                    
                ImportDbObjectViewModel = new DbObjectSelectorViewModel(
                    ImportObjectValidationService.GetAvailableViews(),
                    ImportObjectValidationService.GetAvailableStoredProcedures(),
                    ImportObjectValidationService.GetDefaultViewName(),
                    ImportObjectValidationService.GetDefaultStoredProcedureName());

                // Initialize services that need window reference
                MenuService.Initialize(window);
                UIActionService.Initialize(window);
                UIActionService.SetServiceContainer(this); // Pass service container reference
                ViewStateService.Initialize(window);

                MonitoringService.UpdateStatus(Features.Monitoring.Models.StatusType.Idle, "All services initialized successfully");
            }
            catch (Exception ex)
            {
                MonitoringService?.UpdateStatus(Features.Monitoring.Models.StatusType.Error, "Service initialization failed");
                throw new InvalidOperationException($"Failed to initialize services: {ex.Message}", ex);
            }
        }

        private SharedDatabaseSettings LoadSharedDatabaseSettings()
        {
            const string json = "Config/database.appsettings.json";
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(json, false);
            var cfg = builder.Build();
            var root = cfg.Get<SharedDatabaseSettingsRoot>() ?? throw new InvalidOperationException("Failed to bind SharedDatabaseSettingsRoot");
            return root.DatabaseConfig;
        }
    }
}
