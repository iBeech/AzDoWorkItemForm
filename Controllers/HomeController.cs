using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using AzDoWorkItemForm;
using Microsoft.Extensions.Caching.Memory;


public class HomeController : Controller
{
    private readonly IMemoryCache _cache;

    public HomeController(IMemoryCache memoryCache)
    {
        _cache = memoryCache;
    }

    public IActionResult Index()
    {
        var config = GetOrLoadConfiguration();
        ViewBag.BACKGROUND_COLOUR = config.BACKGROUND_COLOUR;
        ViewBag.PAGE_TITLE = config.PAGE_TITLE;
        ViewBag.FORMS = config.Forms;
        ViewBag.COMPANY_LOGO = config.COMPANY_LOGO;

        return View("Index", config.Forms);
    }
    public IActionResult Form(string name, string prefill, string areaPath)
    {
        var config = GetOrLoadConfiguration();
        ViewBag.BACKGROUND_COLOUR = config.BACKGROUND_COLOUR;
        ViewBag.PAGE_TITLE = config.PAGE_TITLE;
        ViewBag.FORMS = config.Forms;
        ViewBag.COMPANY_LOGO = config.COMPANY_LOGO;
        
        // Find the form requested
        var foundForm = config.Forms.Single(f => f.FORM_TITLE.ToLowerInvariant() == name.ToLowerInvariant());
        
        // 
        if (!string.IsNullOrEmpty(areaPath))
        {
            foundForm.DEFAULT_AREA_PATH = areaPath;
        }

        // Check if there are any prefilled field values in the query string
        if (!string.IsNullOrEmpty(prefill) && prefill.Contains(','))
        {
            var split = prefill.Split(',');
            var PRESET_FIELDNAME = split[0];
            var PRESET_FIELDVALUE = split[1];

            // Search for the field
            var fieldWithDefault = foundForm.FIELDS.SingleOrDefault(f => f.FieldName.Equals(PRESET_FIELDNAME, StringComparison.InvariantCultureIgnoreCase));

            // If we found it, set its default value
            if(fieldWithDefault != null) { fieldWithDefault.DefaultValue = PRESET_FIELDVALUE; }
        }
                
        return View("CreateWorkItemForm", foundForm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkItem(IFormCollection form)
    {
        var config = GetOrLoadConfiguration();
        ViewBag.OrganisationUrl = config.AZURE_DEVOPS_ORG_URL;
        ViewBag.BACKGROUND_COLOUR = config.BACKGROUND_COLOUR;
        ViewBag.PAGE_TITLE = config.PAGE_TITLE;
        ViewBag.FORMS = config.Forms;
        ViewBag.COMPANY_LOGO = config.COMPANY_LOGO;

        // Pull out the form we are working on so we can pick out some critical info for creating the ticket
        var originalForm = config.Forms.Single(f => f.FORM_TITLE.ToLowerInvariant() == form["FormTitle"].ToString().ToLowerInvariant());

        var azureDevOpsService = new AzureDevOpsService(config);

        // Create the work item
        var workItem = await azureDevOpsService.CreateWorkItemAsync(form, originalForm);

        config.CreatedWorkItem = workItem;
        config.SelectedForm = originalForm;

        // Render a view or redirect as needed
        return View("WorkItemCreated", config);
    }
    private Configuration GetOrLoadConfiguration()
    {
        // Try to get configuration from cache
        if (_cache.TryGetValue("Configuration", out Configuration cachedConfig))
        {
            return cachedConfig;
        }

        // If not in cache, load configuration and cache it
        Configuration newConfig = Configuration.LoadConfiguration();
        _cache.Set("Configuration", newConfig, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10), // Set cache expiration time
        });

        return newConfig;
    }
}