using PMS.Common.Filters;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp;
using PMS.Classes;
using static PMS.Classes.OAuthHelper;

namespace PMS.Controllers
{
    public class ZohoController : BaseController
    {
        private static string zohoClientId = ConfigurationManager.AppSettings["zohoClientId"];
        private static string zohoClientSecret = ConfigurationManager.AppSettings["zohoClientSecret"];
        private static string zohoRedirectUrl = ConfigurationManager.AppSettings["zohoRedirectUrl"];
        private static string zohoOrganizationId = ConfigurationManager.AppSettings["zohoOrganizationId"];

        private readonly UnitOfWork<PMSEntities> uow;
        private readonly OAuthHelper oAuthHelper;

        public ZohoController(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
            oAuthHelper = new OAuthHelper(uow);
        }
        // GET: Zoho
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ProcessAuthorization()
        {
            var authorizeUrl = OAuthHelper.GenerateAuthorizationUrl(zohoClientId, zohoRedirectUrl);
            return Redirect(authorizeUrl);
        }
        public ActionResult ProcessZohoCallback(string code)
        {
            try
            {
                if (!string.IsNullOrEmpty(code))
                {
                    // Exchange authorization code for access token
                    OAuthHelper.ZohoTokenReponse tokenResponse = OAuthHelper.GetAccessToken(zohoClientId, zohoClientSecret, zohoRedirectUrl, code);

                    if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                    {
                        var clientIntegration = uow.GenericRepository<ClientIntegration>().Table.FirstOrDefault();

                        if (clientIntegration == null || clientIntegration.Client_Name != "ZohoBooks")
                        {
                            // Insert a new record
                            clientIntegration = new ClientIntegration
                            {
                                Client_Name = "ZohoBooks",
                                Access_Token = tokenResponse.AccessToken,
                                Access_Token_Expiry = DateTime.Now.AddSeconds(tokenResponse.AccessTokenExpiresIn).ToString(),
                                Refresh_Token = tokenResponse.RefreshToken,
                                Refresh_Token_Expiry = DateTime.Now.AddDays(30).ToString() // Set refresh token expiry to 30 days
                            };
                            uow.GenericRepository<ClientIntegration>().Insert(clientIntegration);
                            uow.SaveChanges();
                        }

                        // Redirect to the TriggerZohoCall action directly
                        return RedirectToAction("TriggerZohoCall", "Zoho");
                    }
                }

                // Authorization failed, redirect to an error page or handle accordingly
                return RedirectToAction("Error", "Home");
            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Home");
            }
        }
        public async Task<ActionResult> TriggerZohoCall()
        {
            var clientIntegration = uow.GenericRepository<ClientIntegration>().Table.FirstOrDefault(x => x.Client_Name == "ZohoBooks");

            if (clientIntegration != null && !string.IsNullOrEmpty(clientIntegration.Access_Token))
            {
                DateTime date = clientIntegration.Last_Journal_Entry.HasValue ? clientIntegration.Last_Journal_Entry.Value : DateTime.Today.AddDays(-1);

                if (DateTime.Parse(clientIntegration.Access_Token_Expiry) <= DateTime.Now)
                {
                    var refreshTokenResult = await oAuthHelper.CheckAndRefreshAccessToken(clientIntegration);

                    if (!refreshTokenResult.Success)
                    {
                        return RedirectToAction("ProcessAuthorization", "Zoho");
                    }
                }
                //List<ZohoOperationResult> result = new List<ZohoOperationResult>();

                var createCustomerResult = await CreateCustomer(clientIntegration.Access_Token, zohoOrganizationId);
                //var createChartOfAccountResult = await CreateChartOfAccounts(clientIntegration.Access_Token, zohoOrganizationId);
                //var createTaxResult = await CreateTaxes(clientIntegration.Access_Token, zohoOrganizationId);

                //var createJournalEntryResult = await CreateJournalEntry(clientIntegration.Access_Token, zohoOrganizationId, date);
                var createInvociceEntryResult = await CreateInvoices(clientIntegration.Access_Token, zohoOrganizationId, date);


                if (createInvociceEntryResult.Success && createCustomerResult.Success)
                {
                    TempData["MessageType"] = "success";
                    TempData["ShowMessage"] = "Synced successfully.";
                }


                //if (!createCustomerResult.Success /*&& !createJournalEntryResult.Success*/)
                //{
                //    TempData["MessageType"] = "error";
                //    TempData["ShowMessage"] = "No records found for both customers and journal entries.";
                //}

                //else if (createCustomerResult.Success && !createJournalEntryResult.Success)
                //{
                //    TempData["MessageType"] = "success";
                //    TempData["ShowMessage"] = "Customers synced successfully, but journal entries field due to " + createJournalEntryResult.ErrorMessage;
                //}
                //else if (createJournalEntryResult.Success && !createCustomerResult.Success)
                //{
                //    TempData["MessageType"] = "success";
                //    TempData["ShowMessage"] = "Journal entries synced successfully, but customers synced failed due to " + createCustomerResult.ErrorMessage;
                //}
                else
                {
                    //clientIntegration.Last_Journal_Entry = DateTime.Now;
                    //uow.GenericRepository<ClientIntegration>().Update(clientIntegration);
                    //uow.SaveChanges();
                    TempData["MessageType"] = "success";
                    TempData["ShowMessage"] = "Data is synced to Zoho.";
                }

                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("ProcessAuthorization", "Zoho");
        }
        public async Task<OperationResult> CreateCustomer(string accessToken, string organizationId)
        {
            OperationResult result = new OperationResult();

            try
            {
                var people = uow.GenericRepository<Person>()
                    .Table
                    .Where(/*p => DbFunctions.TruncateTime(p.CreatedDate) == DateTime.Today &&*/ p => p.ZohoCustomerId == null && p.IsEnable == true)
                    .ToList();

                if (!people.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = "No records found to sync data.";
                    return result;
                }

                List<ZohoCustomer> customers = new List<ZohoCustomer>();

                foreach (var person in people)
                {
                    ZohoCustomer customer = new ZohoCustomer()
                    {
                        contact_name = person.Code,
                        contact_persons = new List<ZohoContactPerson>()
                    };
                    customer.contact_persons.Add(new ZohoContactPerson()
                    {
                        first_name = person.FullName,
                        email = person.Email,
                        phone = person.Phone,
                        salutation = person.Title,
                        company_name = "The Myriad"
                    });
                    customers.Add(customer);
                }

                using (HttpClient httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"https://books.zoho.com/api/v3/contacts?organization_id={organizationId}");
                    request.Headers.Add("Authorization", $"Zoho-oauthtoken {accessToken}");

                    var json = JsonConvert.SerializeObject(customers);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    request.Content = content;
                    var response = await httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        result.Success = true;

                        var responseJson = JObject.Parse(responseContent);
                        var contacts = responseJson["contacts"];

                        foreach (var contact in contacts)
                        {
                            var contactName = contact["contact_name"].Value<string>();
                            var contactId = contact["contact_id"].Value<string>();

                            // Find the corresponding person by matching the contact_name with person.Code
                            var person = people.FirstOrDefault(p => p.Code == contactName);
                            if (person != null)
                            {
                                person.ZohoCustomerId = contactId;
                                uow.GenericRepository<Person>().Update(person);
                            }
                        }
                        uow.SaveChanges();
                    }
                    else
                    {
                        result.Success = false;

                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            try
                            {
                                var errorResponse = JObject.Parse(responseContent);
                                result.ErrorMessage = errorResponse.Value<string>("message");
                            }
                            catch
                            {
                                result.ErrorMessage = "Failed to create Zoho customer. Unknown error occurred.";
                            }
                        }
                        else
                        {
                            result.ErrorMessage = "Failed to create Zoho customer. Unknown error occurred.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to create Zoho customer. Error: " + ex.Message;
            }

            return result;
        }
        public async Task<OperationResult> CreateJournalEntry(string accessToken, string organizationId, DateTime date)
        {
            string responseContent = string.Empty;
            OperationResult result = new OperationResult();
            try
            {
                var oauthHelper = new OAuthHelper(uow);
                var ledgerData = oauthHelper.GetLedgerData(uow, date);

                if (!ledgerData.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = "No records found to sync data.";
                    return result;
                }

                var zohoAccounts = GetZohoChartOfAccounts(accessToken, organizationId);

                ZohoJournal journalEntry = new ZohoJournal()
                {
                    journal_date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                    reference_number = "TheMyriad" + Guid.NewGuid().ToString("N").Substring(0, 5),
                    line_items = new List<ZohoJournalLineItem>()
                };

                int itemOrder = 1;
                foreach (var item in ledgerData)
                {
                    string invoiceCode = item.Ledger.Code;
                    double amount = item.Ledger.DebitAmount != null ? Convert.ToDouble(item.Ledger.DebitAmount) : Convert.ToDouble(item.Ledger.CreditAmount);
                    string debitOrCredit = item.Ledger.DebitAmount != null ? "debit" : "credit";
                    if (debitOrCredit == "debit")
                    {
                        item.ChartOfAccount.Name = item.ChartOfAccount.Name.ToUpper();
                    }
                    else
                    {
                        item.PaymentType.PayementName = item.PaymentType.PayementName.ToUpper();
                    }

                    string accountCode = debitOrCredit == "debit" ? item.ChartOfAccount.Code.ToUpper() : item.PaymentType.Code.ToUpper();

                    if (zohoAccounts.TryGetValue(accountCode, out var accountId))
                    {
                        ZohoJournalLineItem lineItem = new ZohoJournalLineItem()
                        {
                            account_id = accountId,
                            debit_or_credit = debitOrCredit,
                            amount = amount,
                            description = item.Ledger.Code,
                            notes = item.Ledger.Remarks,
                            item_order = itemOrder
                        };

                        journalEntry.line_items.Add(lineItem);
                        itemOrder++;
                    }
                }

                using (HttpClient httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"https://books.zoho.com/api/v3/journals?organization_id={organizationId}");
                    request.Headers.Add("Authorization", $"Zoho-oauthtoken {accessToken}");

                    var json = JsonConvert.SerializeObject(journalEntry);
                    var content = new StringContent(json, null, "application/json");
                    request.Content = content;

                    var response = await httpClient.SendAsync(request);
                    responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        result.Success = true;
                    }
                    else
                    {
                        result.Success = false;

                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            try
                            {
                                var errorResponse = JObject.Parse(responseContent);
                                result.ErrorMessage = errorResponse.Value<string>("message");
                            }
                            catch
                            {
                                result.ErrorMessage = "Failed to send journal entries. Unknown error occurred.";
                            }
                        }
                        else
                        {
                            result.ErrorMessage = "Failed to send journal entries. Unknown error occurred.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to send journal entries. Error: " + ex.Message;
            }
            return result;
        }
        public async Task<OperationResult> CreateChartOfAccounts(string accessToken, string organizationId)
        {
            OperationResult result = new OperationResult();

            try
            {
                var chartOfAccounts = uow.GenericRepository<ChartOfAccount>()
                    .Table
                    .Where(c => c.ZohoAccountId == null && c.Status == true)
                    .ToList();

                if (!chartOfAccounts.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = "No records found to sync data.";
                    return result;
                }

                using (HttpClient httpClient = new HttpClient())
                {
                    foreach (var chartOfAccount in chartOfAccounts)
                    {
                        ZohoChartOfAccount zohoAccount = new ZohoChartOfAccount()
                        {
                            account_code = chartOfAccount.Code,
                            account_name = chartOfAccount.Name,
                            account_type = chartOfAccount.AccountType.ZohoAccountType
                        };

                        var request = new HttpRequestMessage(HttpMethod.Post, $"https://books.zoho.com/api/v3/chartofaccounts?organization_id={organizationId}");
                        request.Headers.Add("Authorization", $"Zoho-oauthtoken {accessToken}");
                        var json = JsonConvert.SerializeObject(zohoAccount);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        request.Content = content;

                        var response = await httpClient.SendAsync(request);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var zohoResponse = JsonConvert.DeserializeObject<ZohoChartOfAccountResponse>(responseContent);
                            if (zohoResponse != null && zohoResponse.chart_of_account != null)
                            {
                                chartOfAccount.ZohoAccountId = zohoResponse.chart_of_account.account_id;
                                uow.GenericRepository<ChartOfAccount>().Update(chartOfAccount);
                            }

                            result.Success = true;
                            result.MaxDate = chartOfAccounts.Select(x => x.CreatedDate).Max();
                        }
                        else
                        {
                            result.Success = false;

                            if (!string.IsNullOrEmpty(responseContent))
                            {
                                try
                                {
                                    var errorResponse = JObject.Parse(responseContent);
                                    result.ErrorMessage = errorResponse.Value<string>("message");
                                }
                                catch
                                {
                                    result.ErrorMessage = "Failed to create Zoho chart of accounts. Unknown error occurred.";
                                }
                            }
                            else
                            {
                                result.ErrorMessage = "Failed to create Zoho chart of accounts. Unknown error occurred.";
                            }
                        }
                    }

                    uow.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to create Zoho chart of accounts. Error: " + ex.Message;
            }

            return result;
        }
        public async Task<OperationResult> CreateTaxes(string accessToken, string organizationId)
        {
            OperationResult result = new OperationResult();

            try
            {
                var taxes = uow.GenericRepository<Tax>()
                    .Table
                    .Where(t => t.ZohoTaxId == null /*&& DbFunctions.TruncateTime(t.CreatedDate) == DateTime.Today*/ && t.IsActive == true && t.IsEnable == true)
                    .ToList();

                if (!taxes.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = "No records found to sync data.";
                    return result;
                }

                using (HttpClient httpClient = new HttpClient())
                {
                    foreach (var tax in taxes)
                    {
                        ZohoTax zohoTax = new ZohoTax()
                        {
                            tax_name = tax.TaxName,
                            tax_percentage = tax.TaxPercentage
                        };

                        var request = new HttpRequestMessage(HttpMethod.Post, $"https://books.zoho.com/api/v3/settings/taxes?organization_id={organizationId}");
                        request.Headers.Add("Authorization", $"Zoho-oauthtoken {accessToken}");
                        var json = JsonConvert.SerializeObject(zohoTax);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        request.Content = content;

                        var response = await httpClient.SendAsync(request);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var zohoResponse = JsonConvert.DeserializeObject<ZohoTaxResponse>(responseContent);
                            if (zohoResponse != null && zohoResponse.tax != null)
                            {
                                tax.ZohoTaxId = zohoResponse.tax.tax_id;
                                uow.GenericRepository<Tax>().Update(tax);
                            }

                            result.Success = true;
                            result.MaxDate = taxes.Select(x => x.CreatedDate).Max();
                        }
                        else
                        {
                            result.Success = false;

                            if (!string.IsNullOrEmpty(responseContent))
                            {
                                try
                                {
                                    var errorResponse = JObject.Parse(responseContent);
                                    result.ErrorMessage = errorResponse.Value<string>("message");
                                }
                                catch
                                {
                                    result.ErrorMessage = "Failed to create Zoho taxes. Unknown error occurred.";
                                }
                            }
                            else
                            {
                                result.ErrorMessage = "Failed to create Zoho taxes. Unknown error occurred.";
                            }
                        }
                    }

                    uow.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to create Zoho taxes. Error: " + ex.Message;
            }

            return result;
        }
        public async Task<OperationResult> CreateInvoices(string accessToken, string organizationId, DateTime date)
        {
            OperationResult result = new OperationResult();

            try
            {
                var oauthHelper = new OAuthHelper(uow);
                var invoicingData = oauthHelper.GetInvoicingData(uow, date);

                if (!invoicingData.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = "No records found to sync data.";
                    return result;
                }

                var invoices = new List<ZohoInvoice>();

                foreach (var data in invoicingData)
                {
                    string[] taxIds = data.Invoicing.TaxIds?.Split(',');
                    string firstTaxId = taxIds?.FirstOrDefault();

                    string zohoTaxId = null;
                    string taxName = null;
                    double taxPercentage = 0.0;
                    if (!string.IsNullOrEmpty(firstTaxId))
                    {
                        var tax = GetTaxByTaxId(firstTaxId);
                        zohoTaxId = tax?.ZohoTaxId;
                        taxName = tax?.TaxName;
                        taxPercentage = tax?.TaxPercentage != null ? Convert.ToDouble(tax.TaxPercentage) : 0.0;
                    }

                    ZohoInvoiceLineItem lineItem = new ZohoInvoiceLineItem()
                    {
                        Name = data.InvoicingDetail.ServiceName,
                        Description = data.InvoicingDetail.Description,
                        Rate = Convert.ToDouble(data.InvoicingDetail.Price),
                        Quantity = 1,
                        TaxPercentage = taxPercentage,
                        TaxId = zohoTaxId,
                        TaxName = taxName
                    };

                    ZohoInvoice invoice = new ZohoInvoice()
                    {
                        CustomerId = data.Invoicing.Person.ZohoCustomerId,
                        Date = DateTime.Today.ToString("yyyy-MM-dd"),
                        ReferenceNumber = data.Invoicing.Code,
                        LineItems = new List<ZohoInvoiceLineItem> { lineItem }
                    };

                    invoices.Add(invoice);
                }

                using (HttpClient httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"https://books.zoho.com/api/v3/invoices?organization_id={organizationId}");
                    request.Headers.Add("Authorization", $"Zoho-oauthtoken {accessToken}");

                    var json = JsonConvert.SerializeObject(invoices);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    request.Content = content;

                    var response = await httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = JObject.Parse(responseContent);
                        var invoicesArray = responseJson.GetValue("invoices") as JArray;

                        foreach (var invoiceItem in invoicesArray)
                        {
                            var invoiceId = invoiceItem.Value<string>("invoice_id");
                            var referenceNumber = invoiceItem.Value<string>("reference_number");

                            var matchingRecord = invoicingData.FirstOrDefault(data => data.Invoicing.Code == referenceNumber);
                            if (matchingRecord != null)
                            {
                                matchingRecord.Invoicing.ZohoInvoiceId = invoiceId;
                                uow.GenericRepository<Invoicing>().Update(matchingRecord.Invoicing);
                            }
                        }

                        result.Success = true;
                    }

                    //if (response.IsSuccessStatusCode)
                    //{
                    //    var responseJson = JObject.Parse(responseContent);
                    //    var invoicesArray = responseJson.GetValue("invoices") as JArray;
                    //    for (int i = 0; i < invoicesArray.Count; i++)
                    //    {
                    //        var invoiceItem = invoicesArray[i];
                    //        var invoiceId = invoiceItem.Value<string>("invoice_id");
                    //        invoicingData[i].Invoicing.ZohoInvoiceId = invoiceId;
                    //        uow.GenericRepository<Invoicing>().Update(invoicingData[i].Invoicing);
                    //    }

                    //    result.Success = true;

                    //}
                    else
                    {
                        result.Success = false;

                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            try
                            {
                                var errorResponse = JObject.Parse(responseContent);
                                result.ErrorMessage = errorResponse.Value<string>("message");
                            }
                            catch
                            {
                                result.ErrorMessage = "Failed to create Zoho customer. Unknown error occurred.";
                            }
                        }
                        else
                        {
                            result.ErrorMessage = "Failed to create Zoho customer. Unknown error occurred.";
                        }
                    }
                }
                uow.SaveChanges();

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to create Zoho customer. Error: " + ex.Message;
            }

            return result;
        }
        private Tax GetTaxByTaxId(string taxId)
        {
            int taxIdInt = int.Parse(taxId);
            var tax = uow.GenericRepository<Tax>().Table.FirstOrDefault(t => t.TaxId == taxIdInt);
            return tax;
        }

    }

}