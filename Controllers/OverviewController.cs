using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pentia.Models;
using Newtonsoft.Json;
using System.Globalization;

namespace Pentia.Controllers;

public class OverviewController : Controller
{
    private readonly ILogger<OverviewController> _logger;

    public OverviewController(ILogger<OverviewController> logger)
    {
        _logger = logger;
    }
    public async Task<IActionResult> Index()
    {
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
