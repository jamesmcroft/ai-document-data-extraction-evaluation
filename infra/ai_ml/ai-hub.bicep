import { serverlessModelDeploymentInfo, serverlessModelDeploymentOutputInfo } from './ai-hub-model-serverless-endpoint.bicep'

@description('Name of the resource.')
param name string
@description('Location to deploy the resource. Defaults to the location of the resource group.')
param location string = resourceGroup().location
@description('Tags for the resource.')
param tags object = {}

@description('ID for the Storage Account associated with the AI Hub.')
param storageAccountId string
@description('ID for the Key Vault associated with the AI Hub.')
param keyVaultId string
@description('ID for the Application Insights associated with the AI Hub.')
param applicationInsightsId string
@description('ID for the Container Registry associated with the AI Hub.')
param containerRegistryId string
@description('ID for the Managed Identity associated with the AI Hub. Defaults to the system-assigned identity.')
param identityId string?
@description('Name for the AI Services resource to connect to.')
param aiServicesName string
@description('Serverless model deployments for the AI Hub.')
param serverlessModels serverlessModelDeploymentInfo[] = []

resource aiServices 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' existing = {
  name: aiServicesName
}

resource aiHub 'Microsoft.MachineLearningServices/workspaces@2023-10-01' = {
  name: name
  location: location
  tags: tags
  kind: 'Hub'
  identity: {
    type: identityId == null ? 'SystemAssigned' : 'UserAssigned'
    userAssignedIdentities: identityId == null
      ? null
      : {
          '${identityId}': {}
        }
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    friendlyName: name
    storageAccount: storageAccountId
    keyVault: keyVaultId
    applicationInsights: applicationInsightsId
    containerRegistry: containerRegistryId
    primaryUserAssignedIdentity: identityId
  }

  resource aiServicesConnection 'connections@2024-01-01-preview' = {
    name: '${name}-connection-AzureOpenAI'
    properties: {
      category: 'AzureOpenAI'
      target: aiServices.properties.endpoint
      authType: 'AAD'
      isSharedToAll: true
      metadata: {
        ApiType: 'Azure'
        ResourceId: aiServices.id
      }
    }
  }
}

module serverlessModelEndpoints 'ai-hub-model-serverless-endpoint.bicep' = [
  for serverlessModel in serverlessModels: {
    name: serverlessModel.name
    params: {
      name: serverlessModel.name
      aiHubName: aiHub.name
      model: serverlessModel.model
      keyVaultConfig: serverlessModel.keyVaultConfig
    }
  }
]

@description('The deployed AI Hub resource.')
output resource resource = aiHub
@description('ID for the deployed AI Hub resource.')
output id string = aiHub.id
@description('Name for the deployed AI Hub resource.')
output name string = aiHub.name
@description('Identity principal ID for the deployed AI Hub resource.')
output identityPrincipalId string? = identityId == null ? aiHub.identity.principalId : identityId
@description('Serverless model deployments for the AI Hub.')
output serverlessModelDeployments serverlessModelDeploymentOutputInfo[] = [
  for (item, index) in serverlessModels: {
    id: serverlessModelEndpoints[index].outputs.id
    name: serverlessModelEndpoints[index].outputs.name
    endpoint: serverlessModelEndpoints[index].outputs.endpoint
    primaryKeySecretName: serverlessModelEndpoints[index].outputs.primaryKeySecretName
    secondaryKeySecretName: serverlessModelEndpoints[index].outputs.secondaryKeySecretName
  }
]
