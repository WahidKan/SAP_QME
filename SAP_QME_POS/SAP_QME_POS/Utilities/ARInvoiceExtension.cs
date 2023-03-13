using Microsoft.Extensions.Options;
using SAP_ARInvoice.Model.DTO;
using SAP_QME_POS.Connection;
using SAP_QME_POS.Model;
using SAP_QME_POS.Model.Setting;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_QME_POS.Utilities
{
    public class ARInvoiceExtension : IARInvoiceExtension
    {
        //private readonly SAP_Connection _connection;
        //public ARInvoiceExtension(IOptions<Setting> setting)
        //{
        //    _connection = new SAP_Connection(setting.Value);
        //}
        public List<Orders> InvoiceMapper(List<DataModelSP> data)
        {
            List<Orders> orders = new List<Orders>();
            List<DataModelSP> resp = data.Select(x => new { x.CusCode, x.BillNo }).Distinct().Select(x => data.FirstOrDefault(r => r.CusCode == x.CusCode && r.BillNo == x.BillNo)).Distinct().ToList();
            foreach (var item in resp)
            {
                var orderDetail = data.Where(x => x.BillNo == item.BillNo && x.CusCode == item.CusCode).Select(x => new OrderDetail
                {
                    ItemCode = x.ICode,
                    IName = x.IName,
                    Quantity = x.Qty,
                    BankDiscount = x.DisAmt,
                    TaxCode = x.TaxCode,
                    TaxAmount = x.TaxAmt,
                    BankCode = x.BankCode,
                    DisAmt = x.DisAmt,
                    WareHouse = x.BSec,
                    OthDisAmt= x.OthDisAmt,
                    Section = x.BranchId,
                    UnitPrice = double.Parse(x.IRate),
                    OrderCode = x.BillNo


                }).Distinct().ToList();

                orders.Add(new Orders()
                {
                    CustName = item.CusCode,
                    OrderCode = item.BillNo,
                    OrderDate = item.TDate,
                    TaxAmountSum = orderDetail.Sum(x => double.Parse(x.TaxAmount)),
                    BankDiscountSum = orderDetail.Sum(x => double.Parse(x.DisAmt)),
                    OtherDiscountSum = orderDetail.Sum(x => double.Parse(x.OthDisAmt)),
                    BankCode = item.BankCode,
                    //BankDiscount = item.DisAmt,
                    TaxCode = item.TaxCode,
                    //TaxAmount = item.TaxAmt,
                    OrderDetail = orderDetail
                });
            }

            return orders;
        }
        public async Task<bool> IsCustomerExist(string CustomerId, ISAP_Connection _connection)
        {
            bool output = false;
            SAPbobsCOM.Recordset recordSet = null;
            BusinessPartners businessPartners = null;
            recordSet = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
            businessPartners = _connection.GetCompany().GetBusinessObject(BoObjectTypes.oBusinessPartners);

            recordSet.DoQuery($"SELECT * FROM \"OCRD\" WHERE \"CardCode\"='{CustomerId}'");
            if (recordSet.RecordCount == 0)
            {
                businessPartners.CardCode = CustomerId;
                var response = businessPartners.Add();
                if (response.Equals(0))
                {
                    return true;
                }
                else
                {
                    return false;
                }
                //IDictionary<string, string> parameters = new Dictionary<string, string>();
                //parameters.Add("@TDate", "2022-12-24");

                //List<Customer> customer = await connection.ArInvoice_SP<Customer>("SAP20", parameters);
                //foreach (var item in customer)
                //{
                //    businessPartners.CardCode = item.CardCode;
                //    businessPartners.CardName = item.CustName;
                //    businessPartners.Phone1 = item.Phone;
                //    businessPartners.CardType = BoCardTypes.cCustomer;
                //    businessPartners.SubjectToWithholdingTax = (BoYesNoNoneEnum)BoYesNoEnum.tNO;
                //    var response = businessPartners.Add();
                //    if (response.Equals(0))
                //    {
                //        return true;

                //    }
                //    else
                //    {
                //        return false;
                //    }
                //}
            }
            else
            {
                output = true;
            }
            return output;
        }
        public async Task<bool> CheckItemExist(List<OrderDetail> orderDetail, ISAP_Connection _connection)
        {
            bool output = false;
            SAPbobsCOM.Items product = null;
            SAPbobsCOM.Recordset recordSet = null;
            recordSet = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
            product = _connection.GetCompany().GetBusinessObject(BoObjectTypes.oItems);

            foreach (var singleOrderDetail in orderDetail)
            {
                recordSet.DoQuery($"SELECT * FROM \"OITM\" WHERE \"ItemCode\"='{singleOrderDetail.ItemCode}'");
                if (recordSet.RecordCount == 0)
                {
                    product.ItemCode = singleOrderDetail.ItemCode;
                    //product.ItemName = singleOrderDetail.ItemDescription;

                    var resp = product.Add();
                    if (resp.Equals(0))
                    {
                        output = true;
                    }
                    else
                    {
                        output = false;
                    }
                    //IDictionary<string, string> parameters = new Dictionary<string, string>();
                    //parameters.Add("@ItemCode", singleOrderDetail.ItemCode);
                    //List<Item> items = await connection.ArInvoice_SP<Item>("GetItems", parameters);
                    //foreach (var item in items)
                    //{
                    //    product.ItemCode = item.ItemCode;
                    //    product.ItemName = item.ItemDescription;

                    //    var resp = product.Add();
                    //    if (resp.Equals(0))
                    //    {
                    //        output = true;
                    //    }
                    //    else
                    //    {
                    //        output = false;
                    //    }
                    //}
                }
                else
                {
                    output = true;
                }
            }
            return output;
        }
        public bool CheckIfInvoiceExist(string orderCode, ISAP_Connection _connection)
        {
            bool output = false;
            SAPbobsCOM.Recordset recordSet = null;
            recordSet = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                //Need to add Column Accordingly
                recordSet.DoQuery($"SELECT * FROM \"OINV\" WHERE \"NumAtCard\"='{orderCode}'");
                if (recordSet.RecordCount > 0)
                {
                    output = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception Occurred: {ex.Message}");
            }

            return output;
        }
    }
}
