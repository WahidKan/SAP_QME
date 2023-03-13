using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System;
using SAPbobsCOM;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using SAP_ARInvoice.Model;
using Microsoft.Extensions.Logging;
using SAP_ARInvoice.Model.DTO;
using SAP_QME_POS.Connection;
using SAP_QME_POS.Model;
using SAP_QME_POS.Model.Setting;
using SAP_QME_POS.Service;
using Microsoft.Extensions.Hosting;
using SAP_QME_POS.Utilities;

namespace SAP_ARInvoice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ARInvoiceController : Controller
    {
        private readonly ILogger _logger;
        private readonly ISAP_Connection _connection;
        private readonly DIService _BackServices;
        private readonly IARInvoiceExtension _invoiceExtension;
        private readonly IDataContext _dataContext;
        public ARInvoiceController(IOptions<Setting> setting, ILogger<HomeController> logger,
            DIService hostedService, IARInvoiceExtension invoiceExtension)
        {
            _connection = new SAP_Connection(setting.Value);
            _logger = logger;
            _BackServices = hostedService;
            _invoiceExtension = invoiceExtension;
            _dataContext = new DataContext(setting.Value);
        }

        [HttpGet("db1")]
        public async Task<string> ArInvoice1()
        {
            _ = new List<Orders>();
            if (_connection.Connect() == 0)
            {
                Documents invoice = null;
                IDictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("@TDate", "2023-03-01");
                parameters.Add("@DTp", "SIV");
                List<Orders> invoices;
                var TotalPostedInvoices = 0;
                double LineWiseOthDiscount = 0;
                double LineWiseTaxSum = 0;
                double ItemLineTotalSum = 0;

                double LineWiseTaxAmtSum = 0;
                double LineWiseotherDiscountSum = 0;
                double HeaderDiscount;

                double DiscountPercent;
                double result2 = 0;
                invoices = _invoiceExtension.InvoiceMapper(await _dataContext.ArInvoice_SP<DataModelSP>("SAP20", parameters));
                if (invoices != null)
                {
                    await _BackServices.StopAsync(new System.Threading.CancellationToken());
                }
                else
                {
                    await _BackServices.StartAsync(new System.Threading.CancellationToken());
                    return "SAP B1 Background service | No data found";
                }
                foreach (var singleInvoice in invoices)
                {
                    if (singleInvoice.CustName == "101000703" && singleInvoice.OrderCode == "000002")
                    {
                        HeaderDiscount = 0;
                        DiscountPercent = 0;
                        LineWiseTaxAmtSum = singleInvoice.TaxAmountSum;
                        LineWiseotherDiscountSum = singleInvoice.OtherDiscountSum;

                        var userResponse = await _invoiceExtension.IsCustomerExist(singleInvoice.CustName, _connection);
                        if (!userResponse)
                        {
                            _logger.LogError("Unable to Create New User");
                            return "SAP B1 Background service";
                        }

                        //var arMemo = CheckIfInvoiceExist(singleInvoice.OrderCode);
                        //if (arMemo)
                        //{
                        //    _logger.LogError("AR Invoice Already Exist");
                        //    continue;
                        //}

                        var productResponse = await _invoiceExtension.CheckItemExist(singleInvoice.OrderDetail, _connection);
                        if (!productResponse)
                        {
                            _logger.LogError("Unable to Create New Item");
                            return "SAP B1 Background service";
                        }

                        invoice = _connection.GetCompany().GetBusinessObject(BoObjectTypes.oInvoices);
                        invoice.CardCode = singleInvoice.CustName;
                        invoice.DocDueDate = DateTime.Now;
                        invoice.DocDate = DateTime.Parse(singleInvoice.OrderDate);
                        invoice.NumAtCard = singleInvoice.OrderCode;
                        invoice.Comments = "Comment Added Through DI-Api";
                        invoice.UserFields.Fields.Item("U_PBN").Value = singleInvoice.OrderCode;

                        ItemLineTotalSum = 0;
                        result2 = 0;
                        foreach (var item in singleInvoice.OrderDetail)
                        {
                            ItemLineTotalSum += (item.UnitPrice * double.Parse(item.Quantity));
                        }

                        foreach (var OrderItem in singleInvoice.OrderDetail)
                        {
                            if (OrderItem.ItemCode == "S614")
                            {
                                invoice.Lines.ItemCode = OrderItem.ItemCode;
                                invoice.Lines.ItemDescription = OrderItem.IName;//
                                invoice.Lines.WarehouseCode = OrderItem.WareHouse;
                                invoice.Lines.Quantity = double.Parse(OrderItem.Quantity);
                                invoice.Lines.UnitPrice = OrderItem.UnitPrice;
                                invoice.Lines.CostingCode = OrderItem.Section;
                                invoice.Lines.Add();
                            }
                            ///////////////////////////

                            //////////////////////////
                            //if (OrderItem.ItemCode == "S614")
                            //{
                            LineWiseTaxSum = 0;
                            LineWiseOthDiscount = 0;

                        }

                        #region Batch wise Item
                        //SAPbobsCOM.Items product = null;
                        //SAPbobsCOM.Recordset recordSet = null;
                        //SAPbobsCOM.Recordset recordSetOBTN = null;
                        //recordSet = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
                        //recordSetOBTN = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
                        //product = _connection.GetCompany().GetBusinessObject(BoObjectTypes.oItems);

                        //recordSet.DoQuery($"select T1.\"U_ItemCode\",T1.\"U_Qty\",T1.\"U_Whs\" from \"@BOMH\" T0 " +
                        //    $"INNER JOIN \"@BOMR\" T1 ON T0.\"DocEntry\"=T1.\"DocEntry\" " +
                        //    $"WHERE T0.\"U_ItemCode\"='{OrderItem.ItemCode}' AND NOT T1.\"U_ItemCode\" IS NULL AND T0.\"U_Whs\"='{OrderItem.WareHouse}'"); //AND T0.\"U_Section\"='{OrderItem.Section}'

                        //var BOMTotal = recordSetRecordCount;
                        //var BOMCurrentCount = 0;
                        //if (recordSet.RecordCount != 0)
                        //{
                        //    while (BOMTotal > BOMCurrentCount)
                        //    {
                        //        var itemCode = recordSet.Fields.Item(0).Value.ToString();
                        //        var IngredientQuantity = double.Parse(recordSet.Fields.Item(1).Value.ToString()) * double.Parse(OrderItem.Quantity);
                        //        double Qty = double.Parse(recordSet.Fields.Item(1).Value.ToString());
                        //        var whs = recordSet.Fields.Item(2).Value.ToString();

                        //        invoice.Lines.ItemCode = itemCode;
                        //        invoice.Lines.WarehouseCode = whs;

                        //        //invoice.Lines.BinAllocations.BinAbsEntry = 1;
                        //        //invoice.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = 0;
                        //        //invoice.Lines.BinAllocations.Quantity = 60;
                        //        //invoice.Lines.BinAllocations.Add();
                        //        invoice.Lines.Quantity = double.Parse($"{IngredientQuantity}");

                        //        recordSetOBTN.DoQuery($"select T0.\"ItemCode\",T1.\"Quantity\", T0.\"DistNumber\" from \"OBTN\" T0 " +
                        //            $"INNER JOIN \"OBTQ\" T1 on T0.\"ItemCode\" = T1.\"ItemCode\" and T0.\"SysNumber\" = T1.\"SysNumber\" " +
                        //            $"INNER JOIN \"OITM\" T2 on T0.\"ItemCode\" = T2.\"ItemCode\" where T1.\"Quantity\" > 0 and T0.\"ItemCode\" = '{itemCode}' and T1.\"WhsCode\"='{whs}' order by T0.\"ExpDate\"");

                        //        var TotalCount = recordSetOBTN.RecordCount;
                        //        var CurrentCount = 0;

                        //        while (TotalCount > CurrentCount)
                        //        {
                        //            if (IngredientQuantity > 0)
                        //            {
                        //                var ExpDate = recordSetOBTN.Fields.Item(0).Value.ToString();
                        //                var AvailableQuantity = recordSetOBTN.Fields.Item(1).Value.ToString();
                        //                var BatchNumber = recordSetOBTN.Fields.Item(2).Value.ToString();
                        //                if (double.Parse(AvailableQuantity) > 0)
                        //                {
                        //                    invoice.Lines.BatchNumbers.BatchNumber = BatchNumber;
                        //                    invoice.Lines.BatchNumbers.ItemCode = itemCode;
                        //                    //invoice.Lines.BatchNumbers.ExpiryDate = ExpDate;

                        //                    if (double.Parse(AvailableQuantity) >= IngredientQuantity)
                        //                    {
                        //                        invoice.Lines.BatchNumbers.Quantity = IngredientQuantity;
                        //                        IngredientQuantity = 0;
                        //                    }
                        //                    else
                        //                    {
                        //                        invoice.Lines.BatchNumbers.Quantity = double.Parse(AvailableQuantity);
                        //                        IngredientQuantity = IngredientQuantity - double.Parse(AvailableQuantity);
                        //                    }
                        //                    invoice.Lines.BatchNumbers.Add();
                        //                };
                        //            }
                        //            CurrentCount += 1;
                        //            recordSetOBTN.MoveNext();
                        //        }

                        //        //if (!IngredientQuantity.Equals(0))
                        //        //{
                        //        //    _logger.LogError($"Not Enough Data in Given Batch= " + OrderItem.ItemCode);
                        //        //    //continue;
                        //        //    return "SAP B1 Background service";
                        //        //}

                        //        invoice.Lines.Add();
                        //        BOMCurrentCount += 1;
                        //        recordSet.MoveNext();
                        //    }
                        //}

                        //else
                        //{
                        //    _logger.LogError($"No BOM found angainst given Item No= " + OrderItem.ItemCode);
                        //    //return "SAP B1 Background service";
                        //    continue;
                        //}

                        #endregion
                        //}

                        #region Expenses
                        if (singleInvoice.CustName == "101000703" && singleInvoice.OrderCode == "000002")
                        {
                            LineWiseTaxSum += singleInvoice.TaxAmountSum;
                            LineWiseOthDiscount += singleInvoice.OtherDiscountSum;

                            var result = LineWiseOthDiscount - ItemLineTotalSum;

                            switch (singleInvoice.TaxCode)
                            {
                                case "16":
                                    invoice.Expenses.TaxCode = "14";
                                    invoice.Expenses.ExpenseCode = 14;
                                    //invoice.Lines.Expenses.TaxCode = "14";
                                    break;
                                case "5":
                                    invoice.Expenses.TaxCode = "12";
                                    invoice.Expenses.ExpenseCode = 12;
                                    break;
                                case "15":
                                    invoice.Expenses.TaxCode = "13";
                                    invoice.Expenses.ExpenseCode = 13;
                                    break;
                                case "0":
                                    invoice.Expenses.TaxCode = "11";
                                    invoice.Expenses.ExpenseCode = 11;
                                    break;
                            }

                            if (result > 0)
                            {
                                invoice.Expenses.LineTotal = 0;
                                //LineWiseTaxAmtSum -= LineWiseTaxSum;
                                //HeaderDiscount += (otherDiscountSum - LineWiseTaxSum);
                            }
                            else
                            {

                                //invoice.Expenses.LineTotal = singleInvoice.TaxAmountSum;
                                HeaderDiscount += LineWiseOthDiscount;
                            }
                            if (ItemLineTotalSum > LineWiseotherDiscountSum)
                            {
                                
                                invoice.Expenses.LineTotal = singleInvoice.TaxAmountSum;
                                invoice.Expenses.Add();
                            }
                            else
                            {
                                invoice.Lines.Expenses.LineTotal = 0;
                                invoice.Lines.Expenses.Add();
                            }

                            if (LineWiseotherDiscountSum > 0)
                            {
                                if (ItemLineTotalSum > LineWiseotherDiscountSum)
                                {
                                    result2 = (LineWiseotherDiscountSum / ItemLineTotalSum) * 100; //+ LineWiseTaxSum;
                                    DiscountPercent = result2;
                                }
                                else
                                {
                                    var ab = LineWiseotherDiscountSum - LineWiseTaxAmtSum;
                                    DiscountPercent = (ab / ItemLineTotalSum) * 100;
                                }
                            }

                            if (singleInvoice.BankCode != null && singleInvoice.BankCode != "0")
                            {
                                invoice.Expenses.ExpenseCode = int.Parse(singleInvoice.BankCode);
                                if (LineWiseotherDiscountSum > 0)
                                {
                                    if (ItemLineTotalSum < LineWiseotherDiscountSum)
                                    {
                                        //LineWiseTaxSum -= LineWiseTaxSum;
                                   
                                        invoice.Expenses.LineTotal = singleInvoice.BankDiscountSum; //- LineWiseTaxSum;
                                    }
                                    //else
                                    //{
                                    //    invoice.Expenses.LineTotal = -singleInvoice.BankDiscountSum;
                                    //}
                                }
                                else
                                {
                                    invoice.Expenses.LineTotal = -singleInvoice.BankDiscountSum;
                                }

                                invoice.Expenses.TaxCode = singleInvoice.BankCode;
                                invoice.Expenses.Add();
                            }
                            else
                            {
                                _logger.LogError($"No Bank Code found angainst given BillNo= " + singleInvoice.OrderCode);
                            }
                        }
                        #endregion
                        invoice.DiscountPercent = DiscountPercent;
                        if (singleInvoice.CustName == "101000703" && singleInvoice.OrderCode == "000002")
                        {
                            if (invoice.Add() == 0)
                            {
                                _logger.LogInformation($"Record added successfully for Invoice No= " + singleInvoice.OrderCode);
                                TotalPostedInvoices += 1;
                            }
                            else
                            {
                                var errCode = _connection.GetCompany().GetLastErrorCode();
                                var response = _connection.GetCompany().GetLastErrorDescription();
                                _logger.LogError($"{errCode}:{response}:{singleInvoice.OrderCode}");
                            }
                        }
                    }
                }

                _connection.GetCompany().Disconnect();
                await _BackServices.StartAsync(new System.Threading.CancellationToken());
                return $"{TotalPostedInvoices} Invoices posted successfully!";
            }
            else
            {
                _logger.LogError(_connection.GetErrorCode() + ": " + _connection.GetErrorMessage());
            }

            return "SAP B1 Background service";
        }

        [HttpGet("db2")]
        public async Task<string> ArInvoice2()
        {
            _ = new List<Orders>();
            if (_connection.Connect2() == 0)
            {
                Documents invoice = null;
                IDictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("@TDate", "2023-03-01");
                parameters.Add("@DTp", "SIV");
                List<Orders> invoices;
                var TotalPostedInvoices = 0;
                invoices = _invoiceExtension.InvoiceMapper(await _dataContext.ArInvoice_SP<DataModelSP>("SAP20B", parameters));
                if (invoices != null)
                {
                    await _BackServices.StopAsync(new System.Threading.CancellationToken());
                }
                else
                {
                    await _BackServices.StartAsync(new System.Threading.CancellationToken());
                    return "SAP B1 Background service | No data found";
                }

                foreach (var singleInvoice in invoices)
                {
                    var userResponse = await _invoiceExtension.IsCustomerExist(singleInvoice.CustName, _connection);

                    if (!userResponse)
                    {
                        _logger.LogError("Unable to Create New User");
                        return "SAP B1 Background service";
                    }

                    //var productResponse = await _invoiceExtension.CheckItemExist(singleInvoice.OrderDetail);
                    //if (!productResponse)
                    //{
                    //    _logger.LogError("Unable to Create New Item");
                    //    return "SAP B1 Background service";
                    //}

                    invoice = _connection.GetCompany().GetBusinessObject(BoObjectTypes.oInvoices);

                    invoice.CardCode = singleInvoice.CustName;
                    invoice.DocDueDate = DateTime.Now;
                    invoice.DocDate = DateTime.Parse(singleInvoice.OrderDate);
                    invoice.NumAtCard = singleInvoice.OrderCode;
                    invoice.Comments = "Comment Added Through DI-Api";
                    invoice.UserFields.Fields.Item("U_PBN").Value = singleInvoice.OrderCode;

                    foreach (var OrderItem in singleInvoice.OrderDetail)
                    {
                        invoice.Lines.ItemCode = OrderItem.ItemCode;
                        invoice.Lines.ItemDescription = OrderItem.IName;//
                        invoice.Lines.WarehouseCode = OrderItem.WareHouse;
                        invoice.Lines.Quantity = double.Parse(OrderItem.Quantity);
                        invoice.Lines.UnitPrice = OrderItem.UnitPrice;
                        invoice.Lines.CostingCode = OrderItem.Section;
                        invoice.Lines.Add();

                        #region Batch wise Item

                        #endregion

                    }
                    #region Expenses

                    if (singleInvoice.TaxCode != null)
                    {
                        switch (singleInvoice.TaxCode)
                        {
                            case "16":
                                invoice.Expenses.TaxCode = "14";
                                invoice.Expenses.ExpenseCode = 14;
                                break;
                            case "5":
                                invoice.Expenses.TaxCode = "12";
                                invoice.Expenses.ExpenseCode = 12;
                                break;
                            case "15":
                                invoice.Expenses.TaxCode = "13";
                                invoice.Expenses.ExpenseCode = 13;
                                break;
                            case "0":
                                invoice.Expenses.TaxCode = "11";
                                invoice.Expenses.ExpenseCode = 11;
                                break;
                        }
                        invoice.Lines.Add();
                        invoice.Expenses.LineTotal = singleInvoice.TaxAmountSum;
                        invoice.Expenses.Add();
                    }

                    else
                    {
                        _logger.LogError($"No Tax Code found angainst given BillNo= " + singleInvoice.OrderCode);
                    }

                    if (singleInvoice.BankCode != null && singleInvoice.BankCode != "0")
                    {
                        invoice.Expenses.ExpenseCode = int.Parse(singleInvoice.BankCode);
                        invoice.Expenses.LineTotal = -singleInvoice.BankDiscountSum;
                        invoice.Expenses.TaxCode = singleInvoice.BankCode;
                        invoice.Expenses.Add();
                    }

                    else
                    {
                        _logger.LogError($"No Bank Code found angainst given BillNo= " + singleInvoice.OrderCode);
                    }
                    #endregion

                    if (invoice.Add() == 0)
                    {
                        _logger.LogInformation($"Record added successfully for Invoice No= " + singleInvoice.OrderCode);
                        TotalPostedInvoices += 1;
                    }
                    else
                    {
                        var errCode = _connection.GetCompany().GetLastErrorCode();
                        var response = _connection.GetCompany().GetLastErrorDescription();
                        _logger.LogError($"{errCode}:{response}:{singleInvoice.OrderCode}");
                    }
                }
                _connection.GetCompany().Disconnect();
                await _BackServices.StartAsync(new System.Threading.CancellationToken());
                return $"{TotalPostedInvoices} Invoices posted successfully!";
            }
            else
            {
                _logger.LogError(_connection.GetErrorCode() + ": " + _connection.GetErrorMessage());
            }

            return "SAP B1 Background service";
        }
    }
}

//#region Expenses
//SAPbobsCOM.Recordset expenseRecordSet = null;
//expenseRecordSet = connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
//expenseRecordSet.DoQuery($"SELECT T0.\"ExpnsCode\" FROM OEXD T0 WHERE Lower(\"ExpnsName\") = Lower('{OrderItem.TaxCode}') ");
//if (expenseRecordSet.RecordCount != 0)
//{
//    var expenseCode = expenseRecordSet.Fields.Item(0).Value;
//    invoice.Lines.Expenses.ExpenseCode = expenseCode;
//    invoice.Lines.Expenses.LineTotal = double.Parse(OrderItem.TaxAmount);

//    invoice.Lines.Expenses.Add();
//}
//#endregion