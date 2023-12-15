using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace AzDoWorkItemForm
{
    public class AzureDevOpsService
    {
        private readonly Configuration _config;

        public AzureDevOpsService()
        {
            _config = Configuration.LoadConfiguration(false);
        }

        public AzureDevOpsService(Configuration config)
        {
            _config = config;
        }

        public async Task<WorkItem> CreateWorkItemAsync(IFormCollection form, Form originalForm)
        {
            VssConnection connection = new VssConnection(new Uri(_config.AZURE_DEVOPS_ORG_URL), new VssBasicCredential(string.Empty, _config.AZURE_DEVOPS_PAT));

            WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            JsonPatchDocument patchDocument = new JsonPatchDocument();

            foreach (var item in form)
            {
                if (item.Key == "attachments[]" || item.Key == "__RequestVerificationToken" || item.Key == "FormTitle")
                {
                    // We will upload attachments after the work item is created
                    continue;
                }

                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{item.Key}",
                    Value = System.Net.WebUtility.HtmlDecode(item.Value)
                });
            }

            // Add the default area path, as defined in the docker variable
            patchDocument.Add(new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = "/fields/System.AreaPath",
                Value = originalForm.DEFAULT_AREA_PATH
            });

            // Create the work item
            var workItem = await witClient.CreateWorkItemAsync(patchDocument, originalForm.AZURE_DEVOPS_PROJECT, originalForm.WORK_ITEM_TYPE);

            var attachments = (List<IFormFile>)form.Files;
            if (attachments == null) return workItem;
            
            await UploadAttachmentAsync(workItem, originalForm, attachments);

            return workItem;
        }

        private async Task UploadAttachmentAsync(WorkItem workItem, Form originalForm, List<IFormFile> attachments)
        {
            // Upload attachments if available
            if (attachments != null && attachments.Count > 0)
            {
                foreach (var attachment in attachments)
                {
                    // Upload attachment and get the URL
                    var attachmentUrl = await UploadAttachmentAsync(workItem.Id.Value, originalForm, attachment);

                    // Add the attachment URL to the work item description or a custom field
                    workItem.Fields["Custom.Attachment"] = attachmentUrl;

                    // Update the work item with the attachment information
                    await UpdateWorkItemAsync(workItem.Id.Value, new JsonPatchDocument
                {
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/relations/-",
                        Value = new
                        {
                            rel = "AttachedFile",
                            url = attachmentUrl,
                            attributes = new { comment = "Uploaded from Support Form" }
                        }
                    }
                });
                }
            }
        }

        private async Task<string?> UploadAttachmentAsync(int workItemId, Form originalForm, IFormFile attachment)
        {
            using (var client = new HttpClient())
            {
                // Set up the request
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_config.AZURE_DEVOPS_PAT}")));

                // Get the stream from the attachment
                using (var stream = attachment.OpenReadStream())
                {
                    // Create a content with the attachment stream
                    var content = new StreamContent(stream);
                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "file",
                        FileName = attachment.FileName
                    };

                    // Escape and upload the attachement to Azure DevOps
                    var escapedFileName = Uri.EscapeDataString(attachment.FileName);
                    var response = await client.PostAsync($"{_config.AZURE_DEVOPS_ORG_URL}/{originalForm.AZURE_DEVOPS_PROJECT}/_apis/wit/attachments?fileName={escapedFileName}&api-version=7.1", content);

                    // Check for a successful response
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Parse the JSON response
                    var responseObject = JsonConvert.DeserializeObject<JObject>(responseContent);

                    // Extract the attachment URL
                    return responseObject["url"]?.ToString();
                }
            }
        }

        private async Task UpdateWorkItemAsync(int workItemId, JsonPatchDocument patchDocument)
        {
            using (var connection = new VssConnection(new Uri(_config.AZURE_DEVOPS_ORG_URL), new VssBasicCredential(string.Empty, _config.AZURE_DEVOPS_PAT)))
            {
                var workItemTrackingHttpClient = connection.GetClient<WorkItemTrackingHttpClient>();
                await workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, workItemId, bypassRules: false);
            }
        }
    }
}