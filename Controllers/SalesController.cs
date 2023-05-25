using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pentia.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Globalization;

namespace Pentia.Controllers
{
    public class SalesController : Controller
    {
        private readonly ILogger<SalesController> _logger;

        public SalesController(ILogger<SalesController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://azurecandidatetestapi.azurewebsites.net/");
            httpClient.DefaultRequestHeaders.Add("ApiKey", "test1234");
            var version = "1.0"; // Replace with the desired version

            var salespeopleEndpointUrl = $"api/v{version}/SalesPersons";
            var orderlinesEndpointUrl = $"api/v{version}/Orderlines";

            var salespeopleResponse = await httpClient.GetAsync(salespeopleEndpointUrl);
            salespeopleResponse.EnsureSuccessStatusCode();

            var salespeopleJson = await salespeopleResponse.Content.ReadAsStringAsync();
            var salespeople = JsonConvert.DeserializeObject<List<SalesPerson>>(salespeopleJson);



            if (salespeople != null && salespeople.Count > 0)
            {
                Console.WriteLine($"API call successful. Received {salespeople.Count} salespeople.");

                // Retrieve order counts for all salespeople
                var orderCounts = await GetSalesPeopleOrderCounts(httpClient, orderlinesEndpointUrl);

                // Update salespeople with their respective order counts
                foreach (var salesperson in salespeople)
                {
                    if (orderCounts.TryGetValue(salesperson.Id, out int orderCount))
                    {
                        salesperson.OrderCount = orderCount;
                    }
                }

                return View(salespeople);
            }
            else
            {
                Console.WriteLine("API call successful, but no salespeople found.");
                return View(); // Return the view without passing any model data
            }
        }

        private async Task<Dictionary<int, int>> GetSalesPeopleOrderCounts(HttpClient httpClient, string orderlinesEndpointUrl)
        {
            var response = await httpClient.GetAsync(orderlinesEndpointUrl);
            response.EnsureSuccessStatusCode();

            var ordersJson = await response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<List<Order>>(ordersJson);

            var orderCounts = new Dictionary<int, int>();

            // Calculate order counts for each salesperson
            foreach (var order in orders)
            {
                if (orderCounts.ContainsKey(order.SalesPersonId))
                {
                    orderCounts[order.SalesPersonId]++;
                }
                else
                {
                    orderCounts[order.SalesPersonId] = 1;
                }
            }

            return orderCounts;
        }

        public async Task<IActionResult> Detail(int id)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://azurecandidatetestapi.azurewebsites.net/");
            httpClient.DefaultRequestHeaders.Add("ApiKey", "test1234");
            var version = "1.0"; // Replace with the desired version
            var endpointUrl = $"api/v{version}/SalesPersons";

            try
            {
                var response = await httpClient.GetAsync(endpointUrl);
                response.EnsureSuccessStatusCode();

                var salespersonListJson = await response.Content.ReadAsStringAsync();
                var salespersonList = JsonConvert.DeserializeObject<List<SalesPerson>>(salespersonListJson);

                var salesperson = salespersonList.FirstOrDefault(sp => sp.Id == id);

                if (salesperson != null)
                {
                    var orderCount = await GetSalesPersonOrderCount(id);
                    salesperson.OrderCount = orderCount;

                    // Populate the OrderList property
                    var orderList = await GetSalesPersonOrderList(id);
                    salesperson.OrderList = orderList;
                    Console.WriteLine("salesperson:" + salesperson.Name);

                    Console.WriteLine("address:" + salesperson.Address);
                    for (int i = 0; i < salesperson.OrderList.Count; i++)
                    {
                        Console.WriteLine("orlderList:" + salesperson.OrderList[i]);
                    }

                    return View("Detail", salesperson);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (HttpRequestException)
            {
                return NotFound();
            }
        }




        private async Task<List<int>> GetSalesPersonOrderList(int salespersonId)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://azurecandidatetestapi.azurewebsites.net/");
            httpClient.DefaultRequestHeaders.Add("ApiKey", "test1234");
            var version = "1.0"; // Replace with the desired version
            var endpointUrl = $"api/v{version}/Orderlines";

            var response = await httpClient.GetAsync(endpointUrl);
            response.EnsureSuccessStatusCode();

            var ordersJson = await response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<List<Order>>(ordersJson);

            // Filter orders by SalesPersonId and get the list of order IDs
            var orderList = orders.Where(order => order.SalesPersonId == salespersonId)
                                  .Select(order => order.Id)
                                  .ToList();

            return orderList;
        }

        private async Task<int> GetSalesPersonOrderCount(int salespersonId)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://azurecandidatetestapi.azurewebsites.net/");
            httpClient.DefaultRequestHeaders.Add("ApiKey", "test1234");
            var version = "1.0"; // Replace with the desired version
            var endpointUrl = $"api/v{version}/Orderlines";

            var response = await httpClient.GetAsync(endpointUrl);
            response.EnsureSuccessStatusCode();

            var ordersJson = await response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<List<Order>>(ordersJson);

            // Count the orders with matching SalesPersonId
            var orderCount = orders.Count(order => order.SalesPersonId == salespersonId);

            return orderCount;
        }

        public async Task<IActionResult> OrdersGraph(int id)
        {
            System.Console.WriteLine("in OrderGraphs. Id:" + id);
            var salesPersonOrderList = await GetSalesPersonOrderList(id);

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://azurecandidatetestapi.azurewebsites.net/");
            httpClient.DefaultRequestHeaders.Add("ApiKey", "test1234");
            var version = "1.0";
            var endpointUrl = $"api/v{version}/Orderlines";

            try
            {
                var response = await httpClient.GetAsync(endpointUrl);
                response.EnsureSuccessStatusCode();

                var ordersJson = await response.Content.ReadAsStringAsync();
                var orders = JsonConvert.DeserializeObject<List<Order>>(ordersJson);

                var orderCountsByMonth = new Dictionary<string, int>();

                foreach (var order in orders)
                {
                    if (order.SalesPersonId == id)
                    {
                        DateTime orderDateTime;
                        if (DateTime.TryParseExact(order.OrderDate, new[] { "dd-MM-yyyy HH:mm", "yyyy-MM-dd HH:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out orderDateTime))
                        {
                            var orderMonth = orderDateTime.ToString("MMMM yyyy");

                            if (orderCountsByMonth.ContainsKey(orderMonth))
                            {
                                orderCountsByMonth[orderMonth]++;
                            }
                            else
                            {
                                orderCountsByMonth[orderMonth] = 1;
                            }
                        }
                    }
                }

                var sortedOrderCounts = orderCountsByMonth.OrderBy(entry => DateTime.Parse(entry.Key)).ToList();
                var dates = sortedOrderCounts.Select(entry => entry.Key);
                var orderCounts = sortedOrderCounts.Select(entry => entry.Value);

                ViewBag.Dates = dates;
                ViewBag.OrderCounts = orderCounts;

                return View();
            }
            catch (HttpRequestException)
            {
                return NotFound();
            }
        }





    }
}
