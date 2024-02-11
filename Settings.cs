using Microsoft.AspNetCore.Cors;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Organization.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzDoWorkItemForm
{
    public class Configuration
    {
        public string PAGE_TITLE { get; set; }
        public string COMPANY_LOGO { get; set; }
        public string BACKGROUND_COLOUR { get; set; }
        public string AZURE_DEVOPS_PAT { get; set; }
        public string AZURE_DEVOPS_ORG_URL { get; set; }
        public List<Form> Forms { get; set; }

        public WorkItem CreatedWorkItem { get; set; }
        public Form SelectedForm { get; set; }

        public static Configuration LoadConfiguration(bool matchFields = true)
        {
            string json;

            // Load JSON from configuration.json file
            using (StreamReader reader = new StreamReader("configuration.json"))
            {
                json = reader.ReadToEnd();
            }

            // Deserialize JSON into Configuration object
            Configuration configuration = JsonConvert.DeserializeObject<Configuration>(json);

            // Load additional form settings
            configuration.LoadFormSettings();

            if (matchFields) configuration.MatchFields();

            return configuration;
        }

        public void LoadFormSettings()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{AZURE_DEVOPS_PAT}")));

                foreach (Form form in Forms)
                {
                    form.AZURE_PROJECT_ID = GetProjectID(httpClient, form.AZURE_DEVOPS_PROJECT);
                    form.AZURE_PROCESS_ID = GetProjectProcessID(httpClient, form.AZURE_PROJECT_ID);
                    form.WORK_ITEM_TYPE_REFERENCE_NAME = GetWorkItemTypeReferenceName(httpClient, form.AZURE_PROCESS_ID, form.WORK_ITEM_TYPE);
                }
            }
        }

        public void MatchFields()
        {
            foreach (var currentForm in Forms)
            {
                GetAllFieldsAsync(currentForm);
            }

            Console.WriteLine(Forms.Count);
        }

        public void GetAllFieldsAsync(Form currentForm)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{AZURE_DEVOPS_PAT}")));

                var url = $"{AZURE_DEVOPS_ORG_URL}/{currentForm.AZURE_DEVOPS_PROJECT}/_apis/wit/fields?api-version=7.1";
                var response = httpClient.GetStringAsync(url).Result;

                var fieldsResponse = JsonConvert.DeserializeObject<FieldsResponse>(response);

                // Loop through all fields we found in Azure
                foreach (AzureField field in fieldsResponse?.value)
                {
                    // Search to see if this form has requested to use this field
                    var matchedField = currentForm.FIELDS.Find(f => f.FieldName.ToLowerInvariant() == field.referenceName.ToLowerInvariant());
                    
                    // If this field has not been requested in the form, skip any further operation on this field
                    if (matchedField == null) { continue; }

                    // Check if it's a picklist field
                    if (field.isPicklist)
                    {
                        // isPickList is true means there is a provided picklistId. 
                        // Attempt to get the pick list this way
                        field.Options = GetPickListFromId(httpClient, field);
                    } 
                    else
                    {
                        // Check if there are any allowedValues (no other values are allowed)
                        // If there are, we will emulate a picklist to ensure users can only pick these values
                        field.AllowedValues = GetAllowedValues(httpClient, currentForm.WORK_ITEM_TYPE_REFERENCE_NAME, field.referenceName, currentForm.AZURE_PROCESS_ID);
                    }

                    // Set the 
                    matchedField.AzDoField = field;
                }
            }

            var unmatchedFields = currentForm.FIELDS.Where(f => f.AzDoField == null).ToList();
            if (unmatchedFields.Any())
            {
                throw new Exception($"Unable to locate following fields for form {currentForm.FORM_TITLE}:{Environment.NewLine}{string.Join(Environment.NewLine, unmatchedFields.Select(f => f.FieldName))}");
            }
        }

        /// <summary>
        /// Gets the ID of the project requestd. Required for querying the API for more information about a given project
        /// </summary>
        private string GetProjectID(HttpClient client, string projectName)
        {
            string url = $"{AZURE_DEVOPS_ORG_URL}/_apis/projects/{projectName}";
            string result = string.Empty;

            HttpResponseMessage response = client.GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                string responseBody = response.Content.ReadAsStringAsync().Result;
                dynamic deseralised = JsonConvert.DeserializeObject(responseBody);
                result = deseralised.id;                
            }

            return result;
        }

        /// <summary>
        /// Gets the Process ID of a given project. Required for querying for information specific to some fields
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private string GetProjectProcessID(HttpClient client, string projectId)
        {
            string url = $"{AZURE_DEVOPS_ORG_URL}/_apis/projects/{projectId}/properties";
            string result = string.Empty;

            HttpResponseMessage response = client.GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                string responseBody = response.Content.ReadAsStringAsync().Result;

                var jsonObject = JObject.Parse(responseBody);

                // Parse out the process template id
                var processTemplateType = jsonObject["value"]
                    .FirstOrDefault(v => v["name"] != null &&
                                         v["name"].ToString() == "System.ProcessTemplateType");

                // Check that the value is not null
                if (processTemplateType != null)
                {
                    // Then return the ID
                    result = processTemplateType["value"].ToString();
                }
            }

            return result;
        }

        /// <summary>
        /// The reference name for a work item type is required in some API calls
        /// </summary>
        /// <param name="client"></param>
        /// <param name="processId"></param>
        /// <param name="workItemTypeName"></param>
        /// <returns></returns>
        private string GetWorkItemTypeReferenceName(HttpClient client, string processId, string workItemTypeName)
        {
            string url = $"{AZURE_DEVOPS_ORG_URL}/_apis/work/processdefinitions/{processId}/workitemtypes";
            string result = string.Empty;

            HttpResponseMessage response = client.GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {                
                string responseBody = response.Content.ReadAsStringAsync().Result;
                var jsonObject = JObject.Parse(responseBody);

                var workItemTypes = jsonObject["value"];
                var wit = workItemTypes.FirstOrDefault(wit => wit["name"].ToString().Equals(workItemTypeName, StringComparison.InvariantCultureIgnoreCase));

                // If we found a matching work item type, return its reference name
                if (wit != null)
                {
                    result = wit["id"].ToString();
                    
                }
            }

            return result;
        }

        private PickListOptions GetPickListFromId(HttpClient httpClient, AzureField field)
        {
            // Extract picklist ID from field reference name
            var picklistId = field.picklistId;

            // Fetch picklist options
            var picklistOptionsUrl = $"{AZURE_DEVOPS_ORG_URL}/_apis/work/processes/lists/{picklistId}?api-version=7.1";
            var picklistOptionsResponse = httpClient.GetStringAsync(picklistOptionsUrl).Result;
            var picklistOptions = JsonConvert.DeserializeObject<PickListOptions>(picklistOptionsResponse);

            // Attach picklist options to the field
            return picklistOptions;
        }

        private AllowedValues GetAllowedValues(HttpClient httpClient, string workItemReferenceName, string fieldReferenceName, string processId)
        {
            // Fetch allowed values
            var allowedValuesUrl = $"{AZURE_DEVOPS_ORG_URL}/_apis/work/processes/{processId}/workItemTypes/{workItemReferenceName}/fields/{fieldReferenceName}?%24expand=1";
            var allowedValuesResponse = httpClient.GetStringAsync(allowedValuesUrl).Result;
            var allowedValues = JsonConvert.DeserializeObject<AllowedValues>(allowedValuesResponse);

            // Attach picklist options to the field
            return allowedValues;
        }

        public class FieldsResponse
        {
            public List<AzureField> value { get; set; }
        }
    }

    public class Form
    {
        public string FORM_TITLE { get; set; }
        public string FORM_DESCRIPTION { get; set; }
        public string ExpandedDescription => FORM_DESCRIPTION.Replace("[DEFAULT_AREA_PATH]", DEFAULT_AREA_PATH);
        public string AZURE_DEVOPS_PROJECT { get; set; }
        public string WORK_ITEM_TYPE { get; set; }
        public string DEFAULT_AREA_PATH { get; set; }
        public string WORK_ITEM_SUBMITTED_MESSAGE { get; set; }
        public bool ENABLE_ATTACHMENTS { get; set; }
        public string AZURE_PROJECT_ID { get; set; }
        public string AZURE_PROCESS_ID { get; set; }
        public string WORK_ITEM_TYPE_REFERENCE_NAME { get; set; }
        public List<Field> FIELDS { get; set; }
        public bool NAVIGATION_DISABLED { get; set; } = false;
    }
    public class Field
    {
        public string Name { get; set; }
        public string FieldName { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public bool Hidden { get; set; } = false;
        public string Description { get; set; }
        public List<Tip> Tips { get; set; }
        public AzureField AzDoField { get; set; }
    }
    public class Tip
    {
        public string Option { get; set; }
        public string TipText { get; set; }
    }

    public class AzureField
    {
        public string docker_variable_name { get; set; }
        public string name { get; set; }
        public string referenceName { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string usage { get; set; }
        public bool isPicklist { get; set; }
        public PickListOptions Options { get; set; }
        public AllowedValues AllowedValues { get; set; }
        public bool AllowedValuesOnly => AllowedValues?.allowedValues?.Any() ?? false;

        public bool isPicklistSuggested { get; set; }
        public string picklistId { get; set; }
        public string url { get; set; }
        public Dictionary<string, string> Tips { get; set; }
        public WorkItem ResultingWorkItem { get; set; }
    }

    public class FieldsResponse
    {
        public List<AzureField> value { get; set; }
    }

    public class PickListOptions
    {
        public List<string> items { get; set; }
    }
    public class AllowedValues
    {
        public List<string> allowedValues{ get; set; }
    }
}