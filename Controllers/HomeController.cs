using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using AzDoWorkItemForm;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var fields = new WorkItemFields();

        ViewData["Title"] = Environment.GetEnvironmentVariable("PAGE_TITLE");
        ViewData["FormTitle"] = Environment.GetEnvironmentVariable("FORM_TITLE");
        ViewData["FormDescription"] = Environment.GetEnvironmentVariable("FORM_DESCRIPTION");
        return View("CreateWorkItemForm", fields);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkItem(IFormCollection form)
    {
        // Read Azure DevOps connection details from environment variables
        string personalAccessToken = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
        string organizationUrl = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG_URL");
        string project = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PROJECT");
        string workItemType = Environment.GetEnvironmentVariable("WORK_ITEM_TYPE");

        ViewData["OrganisationUrl"] = organizationUrl;

        var azureDevOpsService = new AzureDevOpsService(personalAccessToken, organizationUrl, project);
        
        // Create the work item
        var workItem = await azureDevOpsService.CreateWorkItemAsync(form, workItemType);
        
        
        
        var fields = new WorkItemFields()
        {
            CreatedWorkItem = workItem
        };

        // Render a view or redirect as needed
        return View("WorkItemCreated", fields);
    }}
