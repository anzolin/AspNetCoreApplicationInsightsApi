<!-- PROJECT SHIELDS -->
<!--
*** I'm using markdown "reference style" links for readability.
*** Reference links are enclosed in brackets [ ] instead of parentheses ( ).
*** See the bottom of this document for the declaration of the reference variables
*** for contributors-url, forks-url, etc. This is an optional, concise syntax you may use.
*** https://www.markdownguide.org/basic-syntax/#reference-style-links
-->
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]

<br />


# ASP.NET Core 2.0 and Application Insights Api
A simple sample project using ASP.NET Core 2.0 that consumes the Application Insights API and returns the exceptions.


What you need to do
-------------------

First, install the following applications:
- [.NET Core 2.0 SDK](https://www.microsoft.com/net/download/core)
- [Visual Studio Code](https://code.visualstudio.com/)
- [C# for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)

Considering that you know a little about .Net Core, open the 'Visual Studio Code' and create a new .NET Core web project.

```
dotnet new mvc
```

Open the file 'appsettings.json' and put yours Application Insights's configurations like the example below:

```json
  "ApplicationInsightsSettings": {
    "ApiUrl": "https://api.applicationinsights.io/beta/apps/{0}/{1}/{2}?{3}",
    "ApiKey": "",
    "AppId": ""
  }
```

Create a directory called 'Code' in the root folder of your project and in this folder create a class file called 'ApplicationInsightsSettings.cs' with the following content:

`ApplicationInsightsSettings.cs`
```csharp
namespace AspNetCoreApplicationInsightsApi.Code
{
    public class ApplicationInsightsSettings
    {
        public string ApiUrl { get; set; }

        public string ApiKey { get; set; }

        public string AppId { get; set; }
    }
}
```

Open the Startup.cs file and add the following code into the beginning of the ConfigureServices() method.

`Startup.cs`
```csharp
services.Configure<ApplicationInsightsSettings>(Configuration.GetSection("ApplicationInsightsSettings"));
```

Now, create other directory called 'Models' and two classes files with the following contents:

`ErrorViewModel.cs`
```csharp
namespace AspNetCoreApplicationInsightsApi.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
```

`ExceptionViewModel.cs`
```csharp
namespace AspNetCoreApplicationInsightsApi.Models
{
    public class ExceptionViewModel
    {
        public bool WasSearched { get; set; }

        public bool WasFound { get; set; }

        public string Description { get; set; }
    }
}
```

And, create another directory called 'Controller' and controller file with the following contents:

`HomeController.cs`
```csharp
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
```

And lastly, create a view file in the Views/Home folder with the following contents:

`Search.cshtml`
```cshtml
@model AspNetCoreApplicationInsightsApi.Models.ExceptionViewModel
@{

}
<div class="page-header">
	<h2>@ViewData["Title"] <small>@ViewData["SubTitle"]</small></h2>
</div>

<div class="row">
	<div class="col-md-12">
		<section>
			<form class="form-horizontal" asp-controller="Home" asp-action="Search" method="get">
				<div class="panel panel-default">
					<div class="panel-body" style="background-color: #f5f5f5;">
						<div class="input-group">
							<input type="text" class="form-control" name="idException" placeholder="You can search using the exception id field, example: '09cf8729-6e13-4f55-86df-9c7dba8a2773'" id="idException" value="@ViewData["Search"]" style="max-width: none;">
							<span class="input-group-btn">
								<button class="btn btn-primary" type="submit">Search</button>
							</span>
						</div>
					</div>
				</div>
			</form>
		</section>
	</div>
</div>

<div class="row">
	<div class="col-md-12">
		<section>
			@if (!Model.WasFound && Model.WasSearched)
			{
				<div class="alert alert-info" role="alert">No records found.</div>
			}
			else if (Model.WasSearched && Model.WasFound)
			{
				<div class="page-header">
					<h3>Exception data</h3>
				</div>
				<p id="errorBox"></p>

				<script>
					function output(inp) {
						document.getElementById("errorBox").innerHTML += '<pre><code>'+inp+'</code></pre>';
					}

					function syntaxHighlight(json) {
						json = json.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
						return json.replace(/("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)/g, function (match) {
							var cls = 'number';
							if (/^"/.test(match)) {
								if (/:$/.test(match)) {
									cls = 'key';
								} else {
									cls = 'string';
								}
							} else if (/true|false/.test(match)) {
								cls = 'boolean';
							} else if (/null/.test(match)) {
								cls = 'null';
							}
							return '<span class="' + cls + '">' + match + '</span>';
						});
					}

					var obj = @Html.Raw(@Model.Description);
					var str = JSON.stringify(obj, undefined, 4);

					output(syntaxHighlight(str));
				</script>
			}
		</section>
	</div>
</div>
```

Open the Visual Studio Code terminal and restore, build, and run your application.

```
dotnet restore
dotnet build
dotnet run
```

Open the address returned in the terminal in your favorite internet browser!


Nuget packages applied
----------------------

No nuget package applied.


License
-------

This example application is [MIT Licensed](https://github.com/anzolin/AspNetCoreApplicationInsightsApi/blob/master/LICENSE).


About the author
----------------

Hello everyone, my name is Diego Anzolin Ferreira. I'm a .NET developer from Brazil. I hope you will enjoy this simple example application as much as I enjoy developing it. If you have any problems, you can post a [GitHub issue](https://github.com/anzolin/AspNetCoreApplicationInsightsApi/issues). You can reach me out at diego@anzolin.com.br.


Donate
------
  
Want to help me keep creating open source projects, make a donation:

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/donate?business=DN2VPNW42RTXY&no_recurring=0&currency_code=BRL)

Thank you!



<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/anzolin/AspNetCoreApplicationInsightsApi.svg?style=for-the-badge
[contributors-url]: https://github.com/anzolin/AspNetCoreApplicationInsightsApi/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/anzolin/AspNetCoreApplicationInsightsApi.svg?style=for-the-badge
[forks-url]: https://github.com/anzolin/AspNetCoreApplicationInsightsApi/network/members
[stars-shield]: https://img.shields.io/github/stars/anzolin/AspNetCoreApplicationInsightsApi.svg?style=for-the-badge
[stars-url]: https://github.com/anzolin/AspNetCoreApplicationInsightsApi/stargazers
[issues-shield]: https://img.shields.io/github/issues/anzolin/AspNetCoreApplicationInsightsApi.svg?style=for-the-badge
[issues-url]: https://github.com/anzolin/AspNetCoreApplicationInsightsApi/issues
[license-shield]: https://img.shields.io/github/license/anzolin/AspNetCoreApplicationInsightsApi.svg?style=for-the-badge
[license-url]: https://github.com/anzolin/AspNetCoreApplicationInsightsApi/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://www.linkedin.com/in/diego-anzolin/
