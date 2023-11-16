using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzDoWorkItemForm
{
    public class WorkItemFields
    {
        private readonly Dictionary<string, Field> _allFields;

        public Field Title { get; private set; }
        public Field Description { get; private set; }
        public Field AreaPath { get; private set; }
        public Field RequesterName { get; private set; }
        public Field StepsToReproduce { get; private set; }
        public Field ExpectedOutcome { get; private set; }
        public Field ActualOutcome { get; private set; }
        public Field AdditionalComments { get; private set; }
        public Field Severity { get; private set; }
        public Field Reproducibility { get; private set; }
        public Field SystemAreaOrService { get; private set; }
        public Field AffectedFeatureOrArea { get; private set; }
        public Field DeviceModelOrBrowser { get; private set; }
        public Field AppVersion { get; private set; }
        public Field OperatingSystem { get; private set; }
        public Field NetworkType { get; private set; }
        // Add more fields as needed...

        public WorkItemFields()
        {
            // Cache all field info
            _allFields = GetAllFieldsAsync().GetAwaiter().GetResult(); 

            // Attempt to locate all fiends as requested in docker variables
            Title = GetFieldDetails("TITLE");
            Description = GetFieldDetails("DESCRIPTION");
            AreaPath = GetFieldDetails("AREA_PATH");
            RequesterName = GetFieldDetails("REQUESTER_NAME");           
            StepsToReproduce = GetFieldDetails("STEPS_TO_REPRODUCE");
            ExpectedOutcome = GetFieldDetails("EXPECTED_OUTCOME");
            ActualOutcome = GetFieldDetails("ACTUAL_OUTCOME");
            AdditionalComments = GetFieldDetails("ADDITIONAL_COMMENTS");
//          Severity = GetFieldDetails("SEVERITY");
            Reproducibility = GetFieldDetails("REPRODUCIBILITY");
            SystemAreaOrService = GetFieldDetails("SYSTEM_AREA_OR_SERVICE");
            AffectedFeatureOrArea = GetFieldDetails("AFFECTED_FEATURE_OR_AREA");
            DeviceModelOrBrowser = GetFieldDetails("DEVICE_MODEL_OR_BROWSER");
            AppVersion = GetFieldDetails("APP_VERSION");
            OperatingSystem = GetFieldDetails("OPERATING_SYSTEM");
            NetworkType = GetFieldDetails("NETWORK_TYPE");
        }

        private async Task<Dictionary<string, Field>> GetAllFieldsAsync()
        {
            string pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
            string orgUrl = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG_URL");
            string project = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PROJECT");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{pat}")));

                var url = $"{orgUrl}/{project}/_apis/wit/fields?api-version=7.1";
                var response = await httpClient.GetStringAsync(url);

                var fieldsResponse = JsonSerializer.Deserialize<FieldsResponse>(response);

                var allFields = new Dictionary<string, Field>();

                foreach (var field in fieldsResponse?.value)
                {
                    // Check if it's a picklist field
                    if (field.isPicklist)
                    {
                        // Extract picklist ID from field reference name
                        var picklistId = field.picklistId;

                        // Fetch picklist options
                        var picklistOptionsUrl = $"{orgUrl}/_apis/work/processes/lists/{picklistId}?api-version=7.1";
                        var picklistOptionsResponse = await httpClient.GetStringAsync(picklistOptionsUrl);
                        var picklistOptions = JsonSerializer.Deserialize<PickListOptions>(picklistOptionsResponse);

                        // Attach picklist options to the field
                        field.Options = picklistOptions;
                    }

                    allFields[field.referenceName.ToLowerInvariant()] = field;
                }

                return allFields;
            }
        }

        private Field GetFieldDetails(string fieldName)
        {
            var resolvedFieldName = Environment.GetEnvironmentVariable(fieldName);

            // Check if there was a variable defined
            if (resolvedFieldName == null) return null;

            // Look up the requested field reference name
            var result = _allFields.TryGetValue(resolvedFieldName.ToLowerInvariant(), out var field) ? field : null;
            
            // If we found it, update the field from AzDo to add in the docker variable name for future reference
            if (result != null) result.docker_variable_name = fieldName;

            return result;
        }

        public class Field
        {
            public string docker_variable_name { get; set; }
            public string name { get; set; }
            public string nice_name
            {
                get
                {
                    return AddSpaceBeforeCapitalLetters(name ?? "");
                }
            }
            public string referenceName { get; set; }
            public string description { get; set; }
            public string type { get; set; }
            public string usage { get; set; }
            public bool isPicklist { get; set; }
            public PickListOptions Options { get; set; }
            public bool isPicklistSuggested { get; set; }
            public string picklistId { get; set; }
            public string url { get; set; }
        }

        public class FieldsResponse
        {
            public List<Field> value { get; set; }
        }

        public class PickListOptions
        {
            public List<string> items { get; set; }
        }

        private static string AddSpaceBeforeCapitalLetters(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            StringBuilder result = new StringBuilder();
            result.Append(input[0]); // Add the first character without a space

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    result.Append(' '); // Add a space before capital letter
                }

                result.Append(input[i]); // Add the current character
            }

            return result.ToString();
        }
    }
}
