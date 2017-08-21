using AspNetCoreApplicationInsightsApi.Code;
using AspNetCoreApplicationInsightsApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AspNetCoreApplicationInsightsApi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationInsightsSettings _applicationInsightsSettings;

        public HomeController(IOptions<ApplicationInsightsSettings> applicationInsightsSettings)
        {
            _applicationInsightsSettings = applicationInsightsSettings.Value;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Search));
        }

        public IActionResult Search(string idException)
        {
            var ex = new ExceptionViewModel()
            {
                WasSearched = false,
                WasFound = false,
                Description = string.Empty
            };

            if (!string.IsNullOrEmpty(idException))
            {
                var param = FormatParameters(idException);
                var exception = GetEventExceptions(param);

                if (string.IsNullOrEmpty(exception))
                {
                    ex = new ExceptionViewModel()
                    {
                        WasSearched = true,
                        WasFound = false,
                        Description = string.Empty
                    };
                }
                else
                {
                    ex = new ExceptionViewModel()
                    {
                        WasSearched = true,
                        WasFound = true,
                        Description = exception
                    };
                }

                ViewData["Search"] = idException;
            }

            ViewData["Title"] = "Application Exception Lookup";
            ViewData["SubTitle"] = "Search";

            return View(ex);
        }

        private string FormatParameters(string idException)
        {
            if (string.IsNullOrEmpty(idException))
                throw new ArgumentNullException("Name cannot be null or empty string", "idException");

            var timeSpanFilter = "P30D";

            idException = idException.Trim();

            return string.Format("timespan={0}&$top=1&$search={1}", timeSpanFilter, idException);
        }

        private string GetEventExceptions(string parameterString)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("x-api-key", _applicationInsightsSettings.ApiKey);

            var requestUri = string.Format(_applicationInsightsSettings.ApiUrl, _applicationInsightsSettings.AppId, "events", "exceptions", parameterString);

            var response = client.GetAsync(requestUri).Result;

            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                return response.ReasonPhrase;
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
