@description('Name of the resource.')
param name string
@description('Location to deploy the resource. Defaults to the location of the resource group.')
param location string = resourceGroup().location
@description('Tags for the resource.')
param tags object = {}

@description('Name for the AI Hub resource associated with the AI Hub project.')
param aiHubName string

resource aiHub 'Microsoft.MachineLearningServices/workspaces@2024-04-01-preview' existing = {
  name: aiHubName
}

resource aiHubProject 'Microsoft.MachineLearningServices/workspaces@2024-04-01-preview' = {
  name: name
  location: location
  tags: tags
  kind: 'Project'
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    friendlyName: name
    hubResourceId: aiHub.id
  }
}

@description('The deployed AI Hub project resource.')
output resource resource = aiHubProject
@description('ID for the deployed AI Hub project resource.')
output id string = aiHubProject.id
@description('Name for the deployed AI Hub project resource.')
output name string = aiHubProject.name
