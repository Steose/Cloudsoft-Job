targetScope = 'resourceGroup'

@description('Azure region for the Azure Container Registry.')
param location string = resourceGroup().location

@description('Short prefix used for resource names.')
param prefix string = 'cloudsoftjob'

@description('Azure Container Registry name. Must be globally unique, 5-50 lowercase alphanumeric characters.')
param acrName string = take(replace('${prefix}${uniqueString(resourceGroup().id)}', '-', ''), 50)

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

output acrName string = acr.name
output acrLoginServer string = acr.properties.loginServer
