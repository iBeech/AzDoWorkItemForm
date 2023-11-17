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

        public Field FIELD_1 { get; private set; }
        public Field FIELD_2{ get; private set; }
        public Field FIELD_3 { get; private set; }
        public Field FIELD_4 { get; private set; }
        public Field FIELD_5 { get; }
        public Field FIELD_6 { get; }
        public Field FIELD_7 { get; }
        public Field FIELD_8 { get; }
        public Field FIELD_9 { get; private set; }
        public Field FIELD_10 { get; private set; }
        public Field FIELD_11 { get; private set; }
        public Field FIELD_12 { get; private set; }
        public Field FIELD_13 { get; private set; }
        public Field FIELD_14 { get; private set; }
        public Field FIELD_15 { get; private set; }

        public WorkItemFields()
        {
            // Cache all field info
            _allFields = GetAllFieldsAsync().GetAwaiter().GetResult(); 

            // Attempt to locate all fiends as requested in docker variables
            FIELD_1 = GetFieldDetails("FIELD_1");
            FIELD_2 = GetFieldDetails("FIELD_2");
            FIELD_3 = GetFieldDetails("FIELD_3");
            FIELD_4 = GetFieldDetails("FIELD_4");           
            FIELD_5 = GetFieldDetails("FIELD_5");
            FIELD_6 = GetFieldDetails("FIELD_6");
            FIELD_7 = GetFieldDetails("FIELD_7");
            FIELD_8 = GetFieldDetails("FIELD_8");
            FIELD_9 = GetFieldDetails("FIELD_9");
            FIELD_10 = GetFieldDetails("FIELD_10");
            FIELD_11 = GetFieldDetails("FIELD_11");
            FIELD_12 = GetFieldDetails("FIELD_12");
            FIELD_13 = GetFieldDetails("FIELD_13");
            FIELD_14 = GetFieldDetails("FIELD_14");
            FIELD_15 = GetFieldDetails("FIELD_15");
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
            var resolvedFieldData = Environment.GetEnvironmentVariable(fieldName);

            // Check if there was a variable defined
            if (resolvedFieldData == null) return null;

            var fieldInfo = resolvedFieldData.Split(',');

            // Look up the requested field reference name
            var result = _allFields.TryGetValue(fieldInfo[1].Trim().ToLowerInvariant(), out var field) ? field : null;

            // If we found it, update the field from AzDo to add in the docker variable name for future reference
            if (result != null)
            {
                result.name = fieldInfo[0].Trim();
                result.docker_variable_name = fieldName;
            }

            return result;
        }

        public class Field
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
