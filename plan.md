## EPPlus Migration Plan (Review Draft)

### 1. Goal & Scope
Replace ClosedXML with EPPlus using `LoadFromDataReader()` for faster, bulk Excel export while preserving the **exact existing workflow sequence**:
1. Execute stored procedure (single execution per parameter combination)
2. Validate row count via `SELECT COUNT(*) FROM <View>`
3. If valid ( >0 and <= Excel limit ), retrieve dynamic column headers (schema)
4. Write headers to row 1
5. Bulk load data rows (starting at row 2) directly from `SqlDataReader` using EPPlus `LoadFromDataReader(reader, printHeaders:false)`
6. Apply formatting (fonts, borders, number formats, text formats, autofit, wrap) **after** all data is loaded (one pass)
7. Name file using existing `FileNameHelper` logic
8. Save file & return result (logging + skip logic remains unchanged)

Out of scope (explicitly NOT changing now):
- Additional features (tables, pivots, charts, multi-sheet enhancements)
- Parallelization or streaming optimizations
- Removal of extra unused package references (unless strictly required)
- Changing logging structure or error handling semantics

### 2. Current ClosedXML Usage Inventory
File: `Features/Export/ExcelService.cs`
- `using ClosedXML.Excel;`
- `new XLWorkbook()` → workbook creation
- `workbook.Worksheets.Add("Export Data")` → sheet creation
- Header writing via per-cell `worksheet.Cell(1, col).Value` + style
- Data population: per-row loop `while(reader.Read())` & per-cell assignment
- Formatting helper `ApplyExcelFormatting()` using ranges and style operations
- `workbook.SaveAs(path)` → persistence

### 3. EPPlus Replacement Strategy
Library: EPPlus (NuGet: `EPPlus` latest stable)

Key substitutions:
| Concern | ClosedXML | EPPlus Equivalent |
|---------|-----------|-------------------|
| Workbook | `new XLWorkbook()` | `new ExcelPackage()` |
| Add sheet | `Workbooks.Add()` | `package.Workbook.Worksheets.Add("Export Data")` |
| Header cell | `worksheet.Cell(r,c)` | `worksheet.Cells[r, c]` |
| Bulk data load | Manual loop | `worksheet.Cells[startRow, startCol].LoadFromDataReader(reader, false)` |
| Save file | `SaveAs(path)` | `package.SaveAs(new FileInfo(path))` |
| Fonts / styles | `range.Style...` | `worksheet.Cells[range].Style...` |
| Number format | `range.Style.NumberFormat.Format = "dd-mmm-yy"` | same |
| Autofit | `worksheet.Columns().AdjustToContents()` | `worksheet.Cells[1,1,lastRow,lastCol].AutoFitColumns()` (optional range limit) |

### 4. Workflow Preservation Details
| Step | Existing Behavior | EPPlus Adaptation |
|------|-------------------|-------------------|
| SP Execution | Done before reading data rows | Unchanged |
| Row Count | COUNT(*) with separate command | Unchanged |
| Header Retrieval | Uses `reader.GetName(i)` without moving reader | Unchanged (no `Read()` before bulk load) |
| Data Population | Manual nested loops | Single `LoadFromDataReader` call (starting at A2) |
| Formatting Timing | After data (currently mostly after) | Consolidate all styling post data load |
| Row Limit Skip | Before export | Unchanged |
| File Naming | `FileNameHelper.GenerateExportFileName` | Unchanged |
| Logging | LoggingHelper steps | Same step names; update wording only if required (minimize changes) |

### 5. Header + Bulk Load Sequence (EPPlus)
1. Create package & worksheet
2. For each field index `i`: write header: `worksheet.Cells[1, i+1].Value = reader.GetName(i)`
3. Call: `worksheet.Cells[2,1].LoadFromDataReader(reader, false)` (reader not advanced yet)
   - NOTE: `printHeaders:false` because we already wrote headers.
4. Capture `lastRow = 1 + recordCount`

### 6. Formatting Plan (Single Pass Post-Load)
Apply operations in this order:
1. Set base font for full used range (`worksheet.Cells[1,1,lastRow,lastCol]`)
2. Header styling (bold, background color, borders) row 1
3. Data borders (inside + outline) across data range (A1..)
4. Date columns: for each configured index `d`: `worksheet.Cells[2, d, lastRow, d].Style.Numberformat.Format = <dateFormat>`
5. Text columns: `Style.Numberformat.Format = "@"`
6. WrapText / alignment: apply to data range
7. Autofit (optional): if enabled, restrict to first N rows for performance (N configurable, default all) – maintain current semantics (auto-fit all) unless config changed.
8. Activate A1 (select cell): (EPPlus selection optional)

### 7. Edge Case Handling
| Case | Handling |
|------|----------|
| Empty result (rowCount==0) | Skip (unchanged) |
| Row > limit | Skip (unchanged) |
| Null field values | EPPlus writes null → blank cells (acceptable) |
| Large numeric text columns (leading zeros) | Apply `@` format per TextColumns config |
| DateTime values | Reader returns DateTime; EPPlus stores native; apply number format |

### 8. Error Handling & Logging Adjustments
Keep existing try/catch boundaries. Only internal wording may change from "Excel Creation" steps; keep same step names to avoid log parsing regressions.

Additional log points (reuse existing calls):
- Before bulk load: `LogStep("Excel Load", "Bulk loading data via EPPlus", processId)` (optional)

### 9. Package & Licensing
Add package reference:
```xml
<PackageReference Include="EPPlus" Version="6.*" />
```
Remove:
```xml
<PackageReference Include="ClosedXML" ... />
```
License context setup (earliest startup):
```csharp
ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Personal / non-commercial use
```
Location: `App.OnStartup` after settings load.

### 10. Code Changes Summary (Planned)
File | Change Type | Summary
-----|-------------|--------
`TradeDataHub.csproj` | Update | Remove ClosedXML ref; add EPPlus ref
`ExcelService.cs` | Refactor | Remove per-cell writes & style logic; implement EPPlus-based bulk load + unified formatting; delete `WriteDataToWorksheet`, modify `WriteColumnHeaders`, replace `ApplyExcelFormatting` with EPPlus version
`App.xaml.cs` | Minor Add | Set EPPlus license context
`using` directives | Cleanup | Replace `using ClosedXML.Excel;` with `using OfficeOpenXml; using OfficeOpenXml.Style;`
`GetColumnLetter` | Retain | Might still be used for numeric->letter mapping (alternative: rely on EPPlus ranges)

### 11. Backward Compatibility / Regression Risk
Risk | Mitigation
-----|-----------
Numeric/text difference (leading zeros) | Preserve TextColumns format `@`
Date display mismatch | Apply configured date format
Autofit performance regression | Maintain current behavior first; optimize later if required
Log diff (unexpected content) | Keep same step names; only add one optional line for EPPlus bulk load
Workbook file size variance | Validate representative sample

### 12. Validation & Test Plan
Test Type | Objective | Method
----------|----------|-------
Functional small dataset | Headers & values identical | Compare sample vs pre-change baseline
Zero-row combination | Skip logic unchanged | Use filters returning no rows
Row limit skip | Validate ExcelRowLimit still triggers | Force threshold test
Text columns leading zeros | Preservation | Insert HS/IEC with leading zeros
Date column formatting | Format exact (dd-mmm-yy) | Inspect output
Performance | Confirm speedup vs old per-cell loop | Timing via logs
Memory footprint | Observe no abnormal growth | Monitor process working set

### 13. Rollback Plan
1. Re-add ClosedXML package
2. Restore prior `ExcelService.cs`
3. Remove EPPlus license context code

### 14. Implementation Order
1. Add EPPlus package & license context (build)
2. Refactor `ExcelService` to EPPlus
3. Remove ClosedXML reference
4. Run validation tests
5. Clean up usings & dead methods
6. Commit changes

### 15. Non-Goals Clarification
- No multi-sheet export
- No advanced Excel objects (tables, pivots, charts)
- No workflow restructuring
- No UI modifications

### 16. Open Points (If Any)
Topic | Decision Needed
------|-----------------
EPPlus Version Pin | Approve specific version (e.g., 6.x)
License Context Type | RESOLVED: NonCommercial (personal use)
Autofit Range Limiting | Proceed full-range first? (Yes/No)

### 17. Approval Checklist
- [ ] Scope accepted
- [ ] License context approved
- [ ] EPPlus version approved
- [ ] Formatting parity acceptable
- [ ] Logging changes (none/minimal) approved

---
Please review. On approval, implementation will follow this plan precisely.
