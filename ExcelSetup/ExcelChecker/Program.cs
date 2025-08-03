using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ExcelChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== TradeDataHub Excel COM Checker ===");
            Console.WriteLine("Checking Excel COM Interop functionality...");
            Console.WriteLine();

            try
            {
                // Test 1: Check Excel Installation
                Console.Write("1. Checking Excel Installation... ");
                if (CheckExcelInstallation())
                {
                    Console.WriteLine("[OK]");
                }
                else
                {
                    Console.WriteLine("[FAIL] - Excel not found");
                    return;
                }

                // Test 2: Check COM Registry
                Console.Write("2. Checking COM Registry... ");
                if (CheckCOMRegistry())
                {
                    Console.WriteLine("[OK]");
                }
                else
                {
                    Console.WriteLine("[FAIL] - COM not registered");
                    Console.WriteLine("   Run SetupExcelCOM.ps1 as Administrator");
                    return;
                }

                // Test 3: Test COM Object Creation
                Console.Write("3. Testing COM Object Creation... ");
                string excelVersion = TestCOMCreation();
                if (!string.IsNullOrEmpty(excelVersion))
                {
                    Console.WriteLine("[OK]");
                    Console.WriteLine($"   Excel Version: {excelVersion}");
                    Console.WriteLine();
                    Console.WriteLine("*** SUCCESS: Excel COM ready for TradeDataHub! ***");
                    Console.WriteLine("TradeDataHub can now create Excel files using COM Interop.");
                }
                else
                {
                    Console.WriteLine("[FAIL] - Cannot create COM object");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static bool CheckExcelInstallation()
        {
            try
            {
                // Check Office Click-to-Run
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");
                if (key != null)
                {
                    var version = key.GetValue("VersionToReport");
                    if (version != null)
                    {
                        Console.Write($"(Office {version}) ");
                        return true;
                    }
                }

                // Check traditional installations
                string[] versions = { "16.0", "15.0", "14.0", "12.0" };
                foreach (string version in versions)
                {
                    using var versionKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Office\{version}\Excel\InstallRoot");
                    if (versionKey != null)
                    {
                        Console.Write($"(Excel {version}) ");
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        static bool CheckCOMRegistry()
        {
            try
            {
                // Check Excel.Application ProgID
                using var progKey = Registry.ClassesRoot.OpenSubKey("Excel.Application");
                if (progKey == null) return false;

                // Check CLSID
                using var clsidKey = Registry.ClassesRoot.OpenSubKey(@"CLSID\{00024500-0000-0000-C000-000000000046}");
                if (clsidKey == null) return false;

                // Check LocalServer32
                using var serverKey = clsidKey.OpenSubKey("LocalServer32");
                if (serverKey == null) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        static string TestCOMCreation()
        {
            object? excel = null;
            try
            {
                Type? excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null) return "";

                excel = Activator.CreateInstance(excelType);
                if (excel == null) return "";

                var version = excel.GetType().InvokeMember("Version",
                    System.Reflection.BindingFlags.GetProperty, null, excel, null);

                // Cleanup
                excel.GetType().InvokeMember("Quit",
                    System.Reflection.BindingFlags.InvokeMethod, null, excel, null);

                return version?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
            finally
            {
                if (excel != null)
                {
                    try
                    {
                        Marshal.ReleaseComObject(excel);
                    }
                    catch { }
                }
            }
        }
    }
}
