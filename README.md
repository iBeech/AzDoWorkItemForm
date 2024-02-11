# AzDoWorkItemForm

This project allows you to create dynamically generated form, based on the fields you want the end user to submit. 

Match one or more field names against a title in the docker environment variables. When the form loads, it queries Azure DevOps for the fields specified, if found it generates an appropraite HTML input. Single line text / Multi line text / multi select option.

You can now override the default area path, or speicfy default options using the query string e.g.

/Home/Form?name=MyForm&prefill=System.Title,My Default Title Text
/Home/Form?name=MyForm&areaPath=Project\Area

### Home Page
![image](https://github.com/iBeech/AzDoWorkItemForm/assets/72153/acdc1208-b1c9-4cf1-8416-e35a618ed533)

### Example Form
![image](https://github.com/iBeech/AzDoWorkItemForm/assets/72153/81add587-99c6-4428-a271-955504ebdd0c)

### Tips
![image](https://github.com/iBeech/AzDoWorkItemForm/assets/72153/4ccd24e3-a572-4c5b-9b74-2a9e0186dca2)

### Extra Forms
![image](https://github.com/iBeech/AzDoWorkItemForm/assets/72153/bf88e3a5-8d26-46ab-bcdb-2e4deea41bd7)


# Deploy with Docker-Compose

```yaml
 companyname-form:
      image: ibeech/azdoworkitemform
      volumes:
        - ./path/to/your/configuration.json:/app/configuration.json        
      ports:
        - "8080:80"
```
# JSON Configuration File
You will need to provide a configuration.json file, which you pass into the docker container

# Application Configuration

## Example confoguration.json
```
{
  "PAGE_TITLE": "Tom Beech - Support",
  "COMPANY_LOGO": "https://www.strunkmedia.com/wp-content/uploads/2018/05/bigstock-222496366.jpg",
  "BACKGROUND_COLOUR": "#FBCB0F",
  "AZURE_DEVOPS_ORG_URL": "https://dev.azure.com/tombeech",
  "AZURE_DEVOPS_PAT": "",
  "Forms": [
    {
      "FORM_TITLE": "Support Request",
      "FORM_DESCRIPTION": "Your queries will be recorded on the chase BAU Board where you can track the progress of your query",
      "AZURE_DEVOPS_PROJECT": "Beech Family",
      "WORK_ITEM_TYPE": "Customer Bug Report",
      "DEFAULT_AREA_PATH": "Beech Family",
      "WORK_ITEM_SUBMITTED_MESSAGE": "Support Request Submitted Successfully!",
      "ENABLE_ATTACHMENTS": true,
      "FIELDS": [
        {
          "Name": "Title",
          "FieldName": "System.Title"
        },
        {
          "Name": "Description",
          "FieldName": "System.Description",
          "Description": "Give your issue a great description"
        },
        {
          "Name": "Requester Name",
          "FieldName": "Custom.StepsToReproduce"
        },
        {
          "Name": "Expected Outcome",
          "FieldName": "Custom.ExpectedOutcome"
        },
        {
          "Name": "Additional Comments",
          "FieldName": "Custom.AdditionalComments"
        },
        {
          "Name": "Reproducibility",
          "FieldName": "Custom.Reproducibility"
        },
        {
          "Name": "System Area or Service",
          "FieldName": "Custom.SystemAreaorService",
          "Tips": [
            {
              "Option": "Service 1",
              "TipText": "Have you tried reproducing in an incognito window? This will run in a clean environment"
            }
          ]
        },
        {
          "Name": "Affected Feature or Area",
          "FieldName": "Custom.AffectedfeatureorArea",
          "Tips": [
            {
              "Option": "Feature 1",
              "TipText": "Have you tried turning it on and turning it back on again? It's Roy's #1 tip."
            },
            {
              "Option": "Feature 2",
              "TipText": "Are you sure this is even a bug? The product is so stable."
            }
          ]
        },
        {
          "Name": "Device Model or Browser",
          "FieldName": "Custom.AppVersion"
        },
        {
          "Name": "Operating System",
          "FieldName": "Custom.OperatingSystem"
        },
        {
          "Name": "Network Type",
          "FieldName": "Custom.NetworkType",
          "Hidden": true
        }
      ]
    },
    {
      "FORM_TITLE": "Change Request",
      "FORM_DESCRIPTION": "A Second form, which is totally different",
      "PAGE_BACKGROUND_COLOR": "#FBCB0F",
      "AZURE_DEVOPS_PROJECT": "Beech Family",
      "WORK_ITEM_TYPE": "Customer Bug Report",
      "DEFAULT_AREA_PATH": "Beech Family",
      "WORK_ITEM_SUBMITTED_MESSAGE": "Support Request Submitted Successfully!",
      "ENABLE_ATTACHMENTS": true,
      "NAVIGATION_DISABLED": true,
      "FIELDS": [
        {
          "Name": "Title",
          "FieldName": "System.Title"
        },
        {
          "Name": "Description",
          "FieldName": "System.Description",
          "Description": "Give your issue a great description"
        },
        {
          "Name": "Requester Name",
          "FieldName": "Custom.StepsToReproduce"
        },
        {
          "Name": "System Area or Service",
          "FieldName": "Custom.SystemAreaorService",
          "Tips": [
            {
              "Option": "Area 1",
              "TipText": "Have you tried reproducing in an incognito window? This will run in a clean environment"
            }
          ]
        },
        {
          "Name": "Device Model or Browser",
          "FieldName": "Custom.AppVersion"
        },
        {
          "Name": "Operating System",
          "FieldName": "Custom.OperatingSystem"
        },
        {
          "Name": "Network Type",
          "FieldName": "Custom.NetworkType"
        }
      ]
    }
  ]
}
```

## JSON Schema

```
{
  "PAGE_TITLE": "",
  "COMPANY_LOGO": "",
  "BACKGROUND_COLOUR": "",
  "AZURE_DEVOPS_ORG_URL": "",
  "AZURE_DEVOPS_PAT": "",
  "Forms": [
    {
      "FORM_TITLE": "",
      "FORM_DESCRIPTION": "",
      "AZURE_DEVOPS_PROJECT": "",
      "WORK_ITEM_TYPE": "",
      "DEFAULT_AREA_PATH": "",
      "WORK_ITEM_SUBMITTED_MESSAGE": "",
      "ENABLE_ATTACHMENTS": true,
      "NAVIGATION_DISABLED": true",
      "FIELDS": [
        {
          "Name": "",
          "FieldName": ""
        },
        {
          "Name": "",
          "FieldName": "",
          "Tips": [
            {
              "Option": "",
              "TipText": ""
              "Hidden": 
            }
          ]
        }
      ]
    }
  ]
}
```

## General Settings

| Setting                  | Description                            |
| ------------------------ | -------------------------------------- |
| **PAGE_TITLE**           | *Your Page Title goes here*          |
| **COMPANY_LOGO**         | *URL to your Company Logo*           |
| **BACKGROUND_COLOUR**    | *Background color code*              |
| **AZURE_DEVOPS_ORG_URL** | *URL to your Azure DevOps organization* |
| **AZURE_DEVOPS_PAT**     | *Your Azure DevOps Personal Access Token* |

## Forms Configuration
You can configure 1 or more forms in your configuration.json. Doing so will allow users to pick which form they want to fill out and submit from the same web-site.

### Form

| Field                    | Description                            |
| ------------------------ | -------------------------------------- |
| **FORM_TITLE**           | *Your Form Title goes here*          |
| **FORM_DESCRIPTION**     | *Description of the form. You can use the variable [DEFAULT_AREA_PATH] here, which will expand to the actual area path set; Useful for when setting the area path in the query string (URL).*            |
| **AZURE_DEVOPS_PROJECT** | *Azure DevOps Project Name*          |
| **WORK_ITEM_TYPE**       | *Work Item Type in Azure DevOps to create*                  |
| **DEFAULT_AREA_PATH**    | *Default Area Path for Work Items*   |
| **WORK_ITEM_SUBMITTED_MESSAGE** | *Success Message upon submission* |
| **ENABLE_ATTACHMENTS**   | *If set to true will allow form submitters to submit files to their ticket*   |
| **NAVIGATION_DISABLED** | *When set to true, will disable the auto generated navigation to other forms. Useful when linking a customer to a form* |

#### Form Fields

| Field Name               | Azure DevOps Field Name                |
| ------------------------ | -------------------------------------- |
| **Name**                 | *Friendly name of the field for the form*      |
| **FieldName**           | *Azure DevOps Field Name e.g. System.Title*            |
| **Hidden**              | *When set to true, will hide this field from the form. Warning, you must set a default value by using the query string*

#### Tips for Specific Fields

| Option                  | TipText                               |
| ------------------------| --------------------------------------|
| **Option**         | *The drop down option you want to create a tip for*            |
| **TipText** | The tip you want to pop up for a user when they select this option from the drop down |

