using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IReportService
    {
        Task<List<SalesSummaryReport>> GetSalesSummaryReportAsync(DateTime startDate, DateTime endDate, string groupBy);
        Task<List<InventoryReport>> GetInventoryReportAsync(int? categoryId, int? brandId);
        Task<List<PurchaseOrderReport>> GetPurchaseOrderReportAsync(DateTime? startDate, DateTime? endDate, int? supplierId, string? status);
    }

    public class ReportService : IReportService
    {
        private readonly ISalesOrderService _salesOrderService;
        private readonly IProductService _productService;
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;
        private readonly ISupplierService _supplierService;

        public ReportService(
            ISalesOrderService salesOrderService,
            IProductService productService,
            IPurchaseOrderService purchaseOrderService,
            ICategoryService categoryService,
            IBrandService brandService,
            ISupplierService supplierService)
        {
            _salesOrderService = salesOrderService;
            _productService = productService;
            _purchaseOrderService = purchaseOrderService;
            _categoryService = categoryService;
            _brandService = brandService;
            _supplierService = supplierService;
        }

        // Sales Summary Report - Grouped by Day/Week/Month
        public async Task<List<SalesSummaryReport>> GetSalesSummaryReportAsync(DateTime startDate, DateTime endDate, string groupBy)
        {
            var salesOrders = await _salesOrderService.GetAllSalesOrdersAsync();
            
            // Filter by date range
            var filteredSales = salesOrders
                .Where(s => s.sales_date >= startDate && s.sales_date <= endDate)
                .ToList();

            var report = new List<SalesSummaryReport>();

            if (groupBy == "Day")
            {
                var grouped = filteredSales
                    .GroupBy(s => s.sales_date.Date)
                    .Select(g => new SalesSummaryReport
                    {
                        Period = g.Key.ToString("MM/dd/yyyy"),
                        TransactionCount = g.Count(),
                        Subtotal = g.Sum(s => s.subtotal),
                        TaxAmount = g.Sum(s => s.tax_amount),
                        TotalAmount = g.Sum(s => s.total_amount)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();

                report = grouped;
            }
            else if (groupBy == "Week")
            {
                var grouped = filteredSales
                    .GroupBy(s => GetWeekOfYear(s.sales_date))
                    .Select(g => new SalesSummaryReport
                    {
                        Period = $"Week {g.Key} ({g.Min(s => s.sales_date):MM/dd/yyyy} - {g.Max(s => s.sales_date):MM/dd/yyyy})",
                        TransactionCount = g.Count(),
                        Subtotal = g.Sum(s => s.subtotal),
                        TaxAmount = g.Sum(s => s.tax_amount),
                        TotalAmount = g.Sum(s => s.total_amount)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();

                report = grouped;
            }
            else if (groupBy == "Month")
            {
                var grouped = filteredSales
                    .GroupBy(s => new { s.sales_date.Year, s.sales_date.Month })
                    .Select(g => new SalesSummaryReport
                    {
                        Period = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                        TransactionCount = g.Count(),
                        Subtotal = g.Sum(s => s.subtotal),
                        TaxAmount = g.Sum(s => s.tax_amount),
                        TotalAmount = g.Sum(s => s.total_amount)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();

                report = grouped;
            }

            return report;
        }

        // Inventory Report
        public async Task<List<InventoryReport>> GetInventoryReportAsync(int? categoryId, int? brandId)
        {
            var products = await _productService.GetProductsAsync();
            var categories = await _categoryService.GetCategoriesAsync();
            var brands = await _brandService.GetBrandsAsync();

            var filteredProducts = products.AsEnumerable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                filteredProducts = filteredProducts.Where(p => p.category_id == categoryId.Value);
            }

            if (brandId.HasValue && brandId.Value > 0)
            {
                filteredProducts = filteredProducts.Where(p => p.brand_id == brandId.Value);
            }

            var report = filteredProducts
                .Select(p => new InventoryReport
                {
                    ProductId = p.product_id,
                    ProductName = p.product_name,
                    SKU = p.product_sku,
                    Category = categories.FirstOrDefault(c => c.category_id == p.category_id)?.category_name ?? "N/A",
                    Brand = brands.FirstOrDefault(b => b.brand_id == p.brand_id)?.brand_name ?? "N/A",
                    Quantity = p.quantity ?? 0,
                    CostPrice = p.cost_price ?? 0,
                    SellPrice = p.sell_price,
                    StockValue = (p.quantity ?? 0) * (p.cost_price ?? 0),
                    Status = p.status == true ? "Active" : "Inactive"
                })
                .OrderBy(r => r.ProductName)
                .ToList();

            return report;
        }

        // Purchase Order Report
        public async Task<List<PurchaseOrderReport>> GetPurchaseOrderReportAsync(DateTime? startDate, DateTime? endDate, int? supplierId, string? status)
        {
            var purchaseOrders = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
            var suppliers = await _supplierService.GetSuppliersAsync();

            var filteredPOs = purchaseOrders.AsEnumerable();

            if (startDate.HasValue)
            {
                filteredPOs = filteredPOs.Where(po => po.order_date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                filteredPOs = filteredPOs.Where(po => po.order_date <= endDate.Value);
            }

            if (supplierId.HasValue && supplierId.Value > 0)
            {
                filteredPOs = filteredPOs.Where(po => po.supplier_id == supplierId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                filteredPOs = filteredPOs.Where(po => po.status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            var report = new List<PurchaseOrderReport>();

            foreach (var po in filteredPOs)
            {
                var items = await _purchaseOrderService.GetPurchaseOrderItemsAsync(po.po_id);
                var supplier = suppliers.FirstOrDefault(s => s.supplier_id == po.supplier_id);

                report.Add(new PurchaseOrderReport
                {
                    PurchaseOrderId = po.po_id,
                    PurchaseOrderNumber = po.po_number,
                    OrderDate = po.order_date,
                    SupplierName = supplier?.supplier_name ?? "N/A",
                    Status = po.status,
                    TotalAmount = items.Sum(i => i.quantity_ordered * i.unit_cost),
                    ItemCount = items.Count,
                    ExpectedDeliveryDate = po.expected_date
                });
            }

            return report.OrderByDescending(r => r.OrderDate).ToList();
        }

        // Helper method to get week number
        private int GetWeekOfYear(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var calendar = culture.Calendar;
            return calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
        }
    }

    // Report Models
    public class SalesSummaryReport
    {
        public string Period { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class InventoryReport
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellPrice { get; set; }
        public decimal StockValue { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PurchaseOrderReport
    {
        public int PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
    }
}

