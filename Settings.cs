using Microsoft.AspNetCore.Cors;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            if (matchFields) configuration.MatchFields();

            return configuration;
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
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{AZURE_DEVOPS_PAT}")));

                var url = $"{AZURE_DEVOPS_ORG_URL}/{currentForm.AZURE_DEVOPS_PROJECT}/_apis/wit/fields?api-version=7.1";
                var response = httpClient.GetStringAsync(url).Result;

                var fieldsResponse = JsonConvert.DeserializeObject<FieldsResponse>(response);

                foreach (var field in fieldsResponse?.value)
                {
                    // Check if it's a picklist field
                    if (field.isPicklist)
                    {
                        // Extract picklist ID from field reference name
                        var picklistId = field.picklistId;

                        // Fetch picklist options
                        var picklistOptionsUrl = $"{AZURE_DEVOPS_ORG_URL}/_apis/work/processes/lists/{picklistId}?api-version=7.1";
                        var picklistOptionsResponse = httpClient.GetStringAsync(picklistOptionsUrl).Result;
                        var picklistOptions = JsonConvert.DeserializeObject<PickListOptions>(picklistOptionsResponse);

                        // Attach picklist options to the field
                        field.Options = picklistOptions;
                    }

                    var matchedField = currentForm.FIELDS.Find(f => f.FieldName.ToLowerInvariant() == field.referenceName.ToLowerInvariant());
                    if (matchedField != null) { matchedField.AzDoField = field; }
                }
            }

            var unmatchedFields = currentForm.FIELDS.Where(f => f.AzDoField == null).ToList();
            if (unmatchedFields.Any())
            {
                throw new Exception($"Unable to locate following fields for form {currentForm.FORM_TITLE}:{Environment.NewLine}{string.Join(Environment.NewLine, unmatchedFields.Select(f => f.FieldName))}");
            }

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
        public string AZURE_DEVOPS_PROJECT { get; set; }
        public string WORK_ITEM_TYPE { get; set; }
        public string DEFAULT_AREA_PATH { get; set; }
        public string WORK_ITEM_SUBMITTED_MESSAGE { get; set; }
        public bool ENABLE_ATTACHMENTS { get; set; }
        public List<Field> FIELDS { get; set; }
    }
    public class Field
    {
        public string Name { get; set; }
        public string FieldName { get; set; }
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
}