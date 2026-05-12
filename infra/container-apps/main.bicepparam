using './main.bicep'

param location = 'northeurope'
param prefix = 'cloudsoftjob'
param imageRepository = 'cloudsoft-job'
param imageTag = 'latest'
param useMongoDb = false
param useAzureKeyVault = false
param useAzureStorage = true
param imageBlobContainerName = 'images'
