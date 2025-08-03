# TradeDataHub Deployment Checklist

## Pre-Deployment Requirements

### System Requirements
- [ ] Windows 10/11 or Windows Server 2016+
- [ ] .NET 8.0 Runtime installed
- [ ] Microsoft Excel installed (Office 365, 2016, 2019, etc.)
- [ ] SQL Server access configured
- [ ] Administrator privileges available for setup

### Excel Versions Supported
- [ ] ✅ Office 365 (recommended)
- [ ] ✅ Excel 2019
- [ ] ✅ Excel 2016  
- [ ] ✅ Excel 2013
- [ ] ✅ Excel 2010
- [ ] ⚠️ Excel 2007 (limited support)
- [ ] ❌ Excel 2003 (not recommended)

## Deployment Steps

### 1. Copy Application Files
- [ ] Copy entire `TradeDataHub.02` folder to target machine
- [ ] Verify all files copied successfully
- [ ] Check folder permissions

### 2. Install Dependencies
- [ ] Install .NET 8.0 Runtime if not present
- [ ] Verify Excel installation
- [ ] Install Microsoft.Office.Interop.Excel (included in project)

### 3. Excel COM Interop Setup
- [ ] Navigate to `ExcelSetup` folder
- [ ] Run `SetupExcelCOM.ps1` as Administrator
- [ ] Verify setup completed successfully
- [ ] Run `VerifyExcelCOM.ps1` to confirm functionality

### 4. Configuration
- [ ] Update `appsettings.json` with correct database connection
- [ ] Configure export paths in settings
- [ ] Test database connectivity
- [ ] Verify Excel format templates are present

### 5. Testing
- [ ] Run `ExcelSetup\VerifyExcelCOM.ps1` to verify COM functionality
- [ ] Test small export (< 1000 rows)
- [ ] Test medium export (10K-50K rows)
- [ ] Verify export performance meets expectations

### 6. Performance Validation
- [ ] Test with actual data volumes
- [ ] Measure export times
- [ ] Verify memory usage is acceptable
- [ ] Check Excel file integrity

## Verification Commands

### Excel COM Status Check
```powershell
# Run as Administrator
cd ExcelSetup
.\VerifyExcelCOM.ps1
```
Expected output:
- ✅ Excel COM is working
- ✅ CopyFromRecordset method available

### Application Test
```cmd
cd TradeDataHub
dotnet run
```

## Troubleshooting

### Excel COM Issues
If Excel COM setup fails:
1. [ ] Verify Excel is installed and accessible
2. [ ] Run setup script as Administrator
3. [ ] Check Windows Event Logs for errors
4. [ ] Try manual registry entries (see README.md)
5. [ ] Restart machine and try again

### Performance Issues
If export performance is poor:
1. [ ] Verify COM Interop is working (run `ExcelSetup\VerifyExcelCOM.ps1`)
2. [ ] Check database connection performance
3. [ ] Monitor memory usage during export
4. [ ] Consider increasing SQL command timeout values

### Permission Issues
If access denied errors occur:
1. [ ] Verify user has necessary file system permissions
2. [ ] Check SQL Server connection permissions
3. [ ] Ensure Excel can be automated (not running in background)

## Expected Performance Metrics

### With COM Interop (Target Performance)
- **Small datasets (< 10K rows)**: 10-30 seconds
- **Medium datasets (10K-100K rows)**: 1-3 minutes  
- **Large datasets (100K-1M rows)**: 3-8 minutes
- **Very large datasets (> 1M rows)**: 8-15 minutes

### Fallback Performance (Optimized ClosedXML)
- **Small datasets (< 10K rows)**: 30-60 seconds
- **Medium datasets (10K-100K rows)**: 3-8 minutes
- **Large datasets (100K-1M rows)**: 15-30 minutes
- **Very large datasets (> 1M rows)**: 30-60 minutes

## Post-Deployment

### User Training
- [ ] Train users on new export functionality
- [ ] Document performance expectations
- [ ] Provide troubleshooting guide
- [ ] Set up support procedures

### Monitoring
- [ ] Monitor export performance regularly
- [ ] Check for Excel COM errors in logs
- [ ] Track user feedback and issues
- [ ] Plan for maintenance and updates

### Backup
- [ ] Backup working configuration
- [ ] Document custom settings
- [ ] Create restore procedures

## Support Information

### Documentation Locations
- Main setup guide: `ExcelSetup\README.md`
- Application documentation: `README.md`
- Change log: `CHANGELOG.md`

### Key Files for Support
- Excel setup script: `ExcelSetup\SetupExcelCOM.ps1`
- Verification script: `ExcelSetup\VerifyExcelCOM.ps1`
- Application logs: `TradeDataHub\Logs\`

### Common Issues Reference
1. **COM not working**: Run setup script as Administrator
2. **Slow performance**: Verify COM is enabled, check database performance
3. **File access errors**: Check permissions and paths
4. **Excel crashes**: Ensure no other Excel instances running

---

**Deployment Date**: _______________  
**Deployed By**: _______________  
**Tested By**: _______________  
**Approved By**: _______________

*For technical support, refer to the development team or system administrator.*
