targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short prefix used for resource names.')
param prefix string = 'cloudsoftjob'

@description('Azure Container Registry name. Must be globally unique, 5-50 lowercase alphanumeric characters.')
param acrName string = take(replace('${prefix}${uniqueString(resourceGroup().id)}', '-', ''), 50)

@description('Container Apps managed environment name.')
param containerAppsEnvironmentName string = '${prefix}-cae'

@description('Container App name.')
param containerAppName string = '${prefix}-web'

@description('User-assigned managed identity name used by the Container App to pull from ACR.')
param managedIdentityName string = '${prefix}-aca-pull'

@description('Log Analytics workspace name.')
param logAnalyticsWorkspaceName string = '${prefix}-logs'

@description('Container image repository name inside ACR.')
param imageRepository string = 'cloudsoft-job'

@description('Container image tag to deploy.')
param imageTag string = 'latest'

@description('Container port exposed by the ASP.NET Core app.')
param containerPort int = 8080

@description('Minimum number of app replicas.')
param minReplicas int = 0

@description('Maximum number of app replicas.')
param maxReplicas int = 2

@description('CPU cores per replica.')
param cpu string = '0.5'

@description('Memory per replica.')
param memory string = '1Gi'

@secure()
@description('Optional MongoDB connection string. Leave empty to use in-memory storage.')
param mongoDbConnectionString string = ''

@description('MongoDB database name.')
param mongoDbDatabaseName string = 'Cloudsoft'

@description('MongoDB job postings collection name.')
param mongoDbJobPostingsCollectionName string = 'jobPostings'

@description('MongoDB employers collection name.')
param mongoDbEmployersCollectionName string = 'employers'

@description('Enable MongoDB repositories when a connection string is supplied.')
param useMongoDb bool = false

@description('Enable Azure Key Vault configuration loading inside the app.')
param useAzureKeyVault bool = false

@description('Key Vault URI used by the app when Azure Key Vault is enabled.')
param keyVaultUri string = ''

@description('Enable Azure Blob image URLs inside the web app.')
param useAzureStorage bool = false

@description('Azure Blob container URL used by the app when Azure Storage is enabled.')
param azureBlobContainerUrl string = ''

@description('Azure Storage account name for public image assets. Must be globally unique, 3-24 lowercase letters and numbers.')
param storageAccountName string = take(replace('${prefix}st${uniqueString(resourceGroup().id)}', '-', ''), 24)

@description('Blob container name for public image assets.')
param imageBlobContainerName string = 'images'

var acrPullRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d'
)

var generatedAzureBlobContainerUrl = 'https://${storage.name}.blob.${az.environment().suffixes.storage}/${imageBlobContainerName}'
var effectiveAzureBlobContainerUrl = !empty(azureBlobContainerUrl) ? azureBlobContainerUrl : (useAzureStorage ? generatedAzureBlobContainerUrl : '')

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource pullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
}

resource acrPullAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, pullIdentity.id, acrPullRoleDefinitionId)
  scope: acr
  properties: {
    principalId: pullIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleDefinitionId
  }
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = if (useAzureStorage) {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: true
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = if (useAzureStorage) {
  parent: storage
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

resource imageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = if (useAzureStorage) {
  parent: blobService
  name: imageBlobContainerName
  properties: {
    publicAccess: 'Blob'
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${pullIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: containerPort
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: pullIdentity.id
        }
      ]
      secrets: empty(mongoDbConnectionString) ? [] : [
        {
          name: 'mongodb-connection-string'
          value: mongoDbConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'cloudsoft-web'
          image: '${acr.properties.loginServer}/${imageRepository}:${imageTag}'
          env: concat([
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:${containerPort}'
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'FeatureFlags__UseMongoDb'
              value: string(useMongoDb)
            }
            {
              name: 'FeatureFlags__UseAzureKeyVault'
              value: string(useAzureKeyVault)
            }
            {
              name: 'FeatureFlags__UseAzureStorage'
              value: string(useAzureStorage)
            }
            {
              name: 'MongoDb__DatabaseName'
              value: mongoDbDatabaseName
            }
            {
              name: 'MongoDb__JobPostingsCollectionName'
              value: mongoDbJobPostingsCollectionName
            }
            {
              name: 'MongoDb__EmployersCollectionName'
              value: mongoDbEmployersCollectionName
            }
            {
              name: 'KeyVault__VaultUri'
              value: keyVaultUri
            }
            {
              name: 'AzureBlob__ContainerUrl'
              value: effectiveAzureBlobContainerUrl
            }
          ], empty(mongoDbConnectionString) ? [] : [
            {
              name: 'MongoDb__ConnectionString'
              secretRef: 'mongodb-connection-string'
            }
          ])
          resources: {
            cpu: json(cpu)
            memory: memory
          }
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
  dependsOn: [
    acrPullAssignment
  ]
}

output acrName string = acr.name
output acrLoginServer string = acr.properties.loginServer
output containerAppName string = containerApp.name
output containerAppFqdn string = containerApp.properties.configuration.ingress.fqdn
output containerAppUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output managedEnvironmentName string = environment.name
output imageStorageAccountName string = useAzureStorage ? storage.name : ''
output imageBlobContainerName string = useAzureStorage ? imageContainer.name : ''
output imageBlobContainerUrl string = effectiveAzureBlobContainerUrl
