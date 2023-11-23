using Microsoft.AspNetCore.Cors;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
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
        public readonly Dictionary<string, Field> AllFields;

        public bool EnableAttachments { get; private set; }
        public WorkItem CreatedWorkItem { get; set; }

        public WorkItemFields()
        {
            // Cache all field info from Azure DevOps
            AllFields = GetAllFieldsAsync().GetAwaiter().GetResult();

            // Go through each docker variable and try to match it with a field from Azure DevOps
            for (var i = 1; i < 15; i++)
            {
                var fieldName = $"FIELD_{i}";
                UpdateFieldDetails(fieldName);
            }

            // Filter out all fields that are not relevant
            // and update the key to be the docker_variable_name
            AllFields = AllFields
                   .Where(pair => !string.IsNullOrEmpty(pair.Value.docker_variable_name))
                   .OrderBy(pair => ExtractNumericValue(pair.Key))
                   .ToDictionary(pair => pair.Value.docker_variable_name, pair => pair.Value);

            // Capture if the user wants to allow uploading attachments
            EnableAttachments = bool.TryParse(Environment.GetEnvironmentVariable("ENABLE_ATTACHMENTS"), out var enableAttachments) && enableAttachments;
        }
        private int ExtractNumericValue(string key)
        {
            // Assuming the key is in the format "FIELD_X"
            string numericPart = key.Substring("FIELD_".Length);
            if (int.TryParse(numericPart, out int numericValue))
            {
                return numericValue;
            }
            else
            {
                return int.MaxValue; // Put keys without numeric values at the end
            }
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

        private void UpdateFieldDetails(string fieldName)
        {
            var resolvedFieldData = Environment.GetEnvironmentVariable(fieldName);

            // Check if there was a variable defined
            if (resolvedFieldData == null) return;

            var fieldInfo = resolvedFieldData.Split(',');

            // Look up the requested field reference name
            var result = AllFields.TryGetValue(fieldInfo[1].Trim().ToLowerInvariant(), out var field) ? field : null;

            // No field found in Azure DevOps which matches
            if (result == null)
            {
                Console.WriteLine($"Unable to find field {fieldName} in Azure DevOps");
            }

            // Update the field from AzDo to add in the docker variable name for future reference
            result.name = fieldInfo[0].Trim();
            result.docker_variable_name = fieldName;

            // If the configuration overrides the fields description, store it
            if (fieldInfo.Length > 2)
            {
                result.description = fieldInfo[2].Trim();

                // Check if the user configured the description to be hidden
                // Useful if the user does not want the description in Azure DevOps 
                // to be changed, but it doesnt sit right in the form, and they
                // don't want a description, as it may be self explanitory
                if (result.description.Equals("hide", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.description = string.Empty;
                }
            }

            // Read in any tips to be displayed post creation
            result.Tips = ReadFieldTips(fieldName);
        }

        private Dictionary<string, string> ReadFieldTips(string fieldName)
        {
            var fieldTipName = $"{fieldName}_TIPS";
            var fieldTips = Environment.GetEnvironmentVariable(fieldTipName);

            // If no tips were defined, return an empty dictionary
            if (string.IsNullOrEmpty(fieldTips)) return new Dictionary<string, string>();

            return ExtractTips(fieldTips);
        }

        public static Dictionary<string, string> ExtractTips(string fieldTips)
        {
            Dictionary<string, string> tips = new Dictionary<string, string>();

            // Split out each tip and tripm excess
            foreach (var tip in fieldTips.Split(",").Select(tip => tip.Trim()))
            {
                // Split the option name and the tip
                var tipSections = tip.Split(':').Select(tip => tip.Trim()).ToList();

                // Add this tip
                tips.Add(tipSections[0], tipSections[1]);
            }

            return tips;
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
        public Dictionary<string, string> Tips { get; set; }
        public WorkItem ResultingWorkItem { get; set; }
    }

    public class FieldsResponse
    {
        public List<Field> value { get; set; }
    }

    public class PickListOptions
    {
        public List<string> items { get; set; }
    }
}
