param storageAccountName string = 'storage${uniqueString(resourceGroup().id)}'
param hostingPlanName string = 'hostingPlan${uniqueString(resourceGroup().id)}'
param functionAppName string

param sqlServerHostname string = environment().suffixes.sqlServerHostname
param sqlServerName string = 'sql${uniqueString(resourceGroup().id)}'
param sqlAdminLogin string = 'innovationgameadmin'
@secure()
param sqlAdminPassword string

param location string = resourceGroup().location

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  resource blobService 'blobServices' = {
    name: 'default'
    resource container 'containers' = {
      name: '$web'
      properties: {
        publicAccess: 'Blob'
      }
    }
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}
resource isolatedFunction 'Microsoft.Web/sites@2022-03-01' = {
  location: location
  name: functionAppName
  kind: 'functionapp'
  properties: {

    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      connectionStrings: [
        {
          name: 'SqlConnectionString'
          value: 'Server=tcp:${sqlServerName}${sqlServerHostname},1433;Initial Catalog=innovationgame;Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
      ]
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }

        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listKeys(storageAccount.id, '2022-05-01').keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: 'innovationgame'
        }
      ]
      
    }
  }
  resource config 'config' = {
    name: 'web'
    properties: {
      netFrameworkVersion: 'v6.0'
    }
  }
}

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
  }
}
resource sqlDB 'Microsoft.Sql/servers/databases@2021-08-01-preview' = {
  name: '${sqlServer.name}/innovationgame'
  location: location
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'

  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
    family: 'Gen5'
  }
}

resource signalR 'Microsoft.SignalRService/SignalR@2022-08-01-preview' = {
  name: 'signalr${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    serverless: {
      connectionTimeoutInSeconds: 30
    }
  }
  sku: {
    name: 'Free_F1'
  }
}
