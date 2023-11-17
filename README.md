# AzDoWorkItemForm

This project allows you to create dynamically generated form, based on the fields you want the end user to submit. 

Match one or more field names against a title in the docker environment variables. When the form loads, it queries Azure DevOps for the fields specified, if found it generates an appropraite HTML input. Single line text / Multi line text / multi select option.

![image](https://github.com/iBeech/AzDoWorkItemForm/assets/72153/719e495f-f2fe-43a8-abda-94d3f12029e4)

# Deploy with Docker-Compose

```yaml
 companyname-form:
      image: ibeech/azdoworkitemform
      environment:
        - AZURE_DEVOPS_PAT=
        - AZURE_DEVOPS_ORG_URL=
        - AZURE_DEVOPS_PROJECT=Beech Family
        - DEFAULT_AREA_PATH=
        - COMPANY_LOGO=https://www.strunkmedia.com/wp-content/uploads/2018/05/bigstock-222496366.jpg
        - PAGE_TITLE=Tom Beech - Support
        - PAGE_BACKGROUND_COLOUR=#FBCB0F
        - WORK_ITEM_TYPE=Customer Bug Report
        - FORM_TITLE=Family Reporting Form
        - FORM_DESCRIPTION=Your queries will be recorded on the chase BAU Board where you can track the progress of your query
        - WORKITEM_SUBMITTED_MESSAGE=Support Request Submitted Successfully!

        - ENABLE_ATTACHMENTS=true
        - FIELD_1=Title,System.Title
        - FIELD_2=Description,System.Description
        - FIELD_3=Requester Name,Custom.RequesterName
        - FIELD_4=Describe the Bug,System.Description
        - FIELD_5=Steps to Reproduce,Custom.StepstoReproduce
        - FIELD_6=Expected Outcome,Custom.ExpectedOutcome
        - FIELD_7=Actual Outcome,Custom.ActualOutcome
        - FIELD_8=Additional Comments,Custom.AdditionalComments      
        - FIELD_9=Reproducibility,Custom.Reproducibility
        - FIELD_10=System Area or Service,Custom.SystemAreaorService
        - FIELD_11=Affected Feature or Area,Custom.AffectedfeatureorArea
        - FIELD_12=Device Model or Browser,Custom.DeviceModelorBrowser
        - FIELD_13=App Version or Browser,Custom.AppVersion
        - FIELD_14=Operating System,Custom.OperatingSystem
        - FIELD_15=Network Type,Custom.NetworkType
      ports:
        - "8080:80"
```
## Required Variables

| Variable | Description |
| --- | --- |
| AZURE_DEVOPS_PAT | Personal Access Token from Azure DevOps. Needs to have Work Items read and write permissions |
| AZURE_DEVOPS_ORG_URL | The full url of your devops instance e.g. https://dev.azure.com/companyname |
| AZURE_DEVOPS_PROJECT | The team project tickets will be created in |
| DEFAULT_AREA_PATH | The area path which tickets will be created under |
| WORK_ITEM_TYPE | The name of the work item you want creating. e.g. User Story |

## Field Values
Fields are comma separated into 3 values. e.g. 
FIELD_1=Title,System.Title,Give your field a title
| Value | Description |
| --- | --- |
| 1st Value | The main header on the form for this field |
| 2nd Value | The field name in Azure DevOps. This is where the value the user enters will be saved to |
| 3rd Value (Optional) | Field Description. If not specified, will check if there is a description in Azure DevOps and use that. If 'hide' is specified, will ignore the description in Azure DevOps |
