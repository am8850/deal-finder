# Deal Finder

Azure function to find deals and email results



## Solution

### Technologies

- .NET Core 3.1 LTS
- Azure Functions

### Local Settings

> **Note:** The file local.settings.json is not stored in the repo and should be added to the DealFinderAzFuncs project when the project is restored.

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "SmtpHostName": "<HOST_NAME>_",
    "SmtpHostPot": 587,
    "SmtpUserEmailAddress": "<EMAIL_ADDRESS>",
    "SmtpPassword": "<PASSWORD>",
    "EmailsTo": "<name1|email1,name2|email2,etc.>",
    "DealFuncURI": "<URI FOR AZURE FUNCTION>",
    "EmailServiceURI": "<URI FOR AZURE FUNCTION>",
    "KeyWords": "laptop,drive,food processor,citizen",
    "TableStorateConnectionString": "<CONNECTION STRING>"
  }
}
```

### Dependencies

- Newtonsoft
- Mailkit
- HtmlAgilityPack
- Microsoft.Azure.Cosmos.Table

### Azure Functions

- CheckDealsHttpFunc
- CheckDealsTimerFunc
- SendEmailHttpFunc
- CleanupTimerFunc

### Libraries

- DF.Services

#### Html

- ```IProcessHtml```:
- ```ProcessEdealInfo```:
- ```ProcessTechBargains```:

#### Hash

```string Hash(string key)```: Create a has for a string

#### State

Logic: Using Azure Table storage, setting the partiotion key to 'DealFinder' and to RowKey to the has of the item processed.

- ```bool FindAsync(string hash)```: See if deal has already been reported
- ```void SaveAsync()```: If the deal is not found as processed, it is saved to the table