targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the workload which is used to generate a short unique hash used in all resources.')
param workloadName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('Name of the resource group. If empty, a unique name will be generated.')
param resourceGroupName string = ''

@description('Tags for all resources.')
param tags object = {
  WorkloadName: workloadName
  Environment: 'Dev'
}

@description('Principal ID of the user that will be granted permission to access services.')
param userPrincipalId string
@description('Primary location for the Document Intelligence service. Default is westeurope for latest preview support.')
param documentIntelligenceLocation string = 'westeurope'
@description('Primary location for the OpenAI GPT-4 omni deployment. Default is West US 3')
param secondaryAIServicesLocation string = 'westus3'

var abbrs = loadJsonContent('./abbreviations.json')
var roles = loadJsonContent('./roles.json')
var resourceToken = toLower(uniqueString(subscription().id, workloadName, location))
var documentIntelligenceResourceToken = toLower(uniqueString(
  subscription().id,
  workloadName,
  documentIntelligenceLocation
))
var secondaryAIServicesResourceToken = toLower(uniqueString(
  subscription().id,
  workloadName,
  secondaryAIServicesLocation
))

resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.managementGovernance.resourceGroup}${workloadName}'
  location: location
  tags: union(tags, {})
}

resource contributor 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.general.contributor
}

module resouceGroupRoleAssignment './security/resource-group-role-assignment.bicep' = {
  name: '${resourceGroup.name}-role-assignment'
  scope: resourceGroup
  params: {
    roleAssignments: [
      {
        principalId: userPrincipalId
        roleDefinitionId: contributor.id
        principalType: 'User'
      }
    ]
  }
}

resource cognitiveServicesUser 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.ai.cognitiveServicesUser
}

module documentIntelligence './ai_ml/document-intelligence.bicep' = {
  name: '${abbrs.ai.documentIntelligence}${documentIntelligenceResourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.ai.documentIntelligence}${documentIntelligenceResourceToken}'
    location: documentIntelligenceLocation
    tags: union(tags, {})
    roleAssignments: [
      {
        principalId: userPrincipalId
        roleDefinitionId: cognitiveServicesUser.id
        principalType: 'User'
      }
    ]
  }
}

resource storageBlobDataContributor 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.storage.storageBlobDataContributor
}

module storageAccount './storage/storage-account.bicep' = {
  name: '${abbrs.storage.storageAccount}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.storage.storageAccount}${resourceToken}'
    location: location
    tags: union(tags, {})
    sku: {
      name: 'Standard_LRS'
    }
    roleAssignments: [
      {
        principalId: userPrincipalId
        roleDefinitionId: storageBlobDataContributor.id
        principalType: 'User'
      }
      {
        principalId: documentIntelligence.outputs.identityPrincipalId
        roleDefinitionId: storageBlobDataContributor.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

resource keyVaultAdministrator 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.security.keyVaultAdministrator
}

module keyVault './security/key-vault.bicep' = {
  name: '${abbrs.security.keyVault}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.security.keyVault}${resourceToken}'
    location: location
    tags: union(tags, {})
    roleAssignments: [
      {
        principalId: userPrincipalId
        roleDefinitionId: keyVaultAdministrator.id
        principalType: 'User'
      }
    ]
  }
}

module logAnalyticsWorkspace './management_governance/log-analytics-workspace.bicep' = {
  name: '${abbrs.managementGovernance.logAnalyticsWorkspace}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.managementGovernance.logAnalyticsWorkspace}${resourceToken}'
    location: location
    tags: union(tags, {})
  }
}

module applicationInsights './management_governance/application-insights.bicep' = {
  name: '${abbrs.managementGovernance.applicationInsights}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.managementGovernance.applicationInsights}${resourceToken}'
    location: location
    tags: union(tags, {})
    logAnalyticsWorkspaceName: logAnalyticsWorkspace.outputs.name
  }
}

resource acrPush 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.containers.acrPush
}

resource acrPull 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.containers.acrPull
}

module containerRegistry './containers/container-registry.bicep' = {
  name: '${abbrs.containers.containerRegistry}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.containers.containerRegistry}${resourceToken}'
    location: location
    tags: union(tags, {})
    sku: {
      name: 'Basic'
    }
    adminUserEnabled: true
    roleAssignments: [
      {
        principalId: userPrincipalId
        roleDefinitionId: acrPush.id
        principalType: 'User'
      }
      {
        principalId: userPrincipalId
        roleDefinitionId: acrPull.id
        principalType: 'User'
      }
    ]
  }
}

resource cognitiveServicesOpenAIContributor 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.ai.cognitiveServicesOpenAIContributor
}

var gpt4OmniModelDeploymentName = 'gpt-4o'
var gpt4ModelDeploymentName = 'gpt-4'
var gpt35ModelDeploymentName = 'gpt-35-turbo'

module primaryAIServices './ai_ml/ai-services.bicep' = {
  name: '${abbrs.ai.aiServices}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.ai.aiServices}${resourceToken}'
    location: location
    tags: union(tags, {})
    deployments: [
      {
        name: gpt4ModelDeploymentName
        model: {
          format: 'OpenAI'
          name: 'gpt-4'
          version: 'turbo-2024-04-09'
        }
        sku: {
          name: 'Standard'
          capacity: 30
        }
      }
      {
        name: gpt35ModelDeploymentName
        model: {
          format: 'OpenAI'
          name: 'gpt-35-turbo'
          version: '1106'
        }
        sku: {
          name: 'Standard'
          capacity: 30
        }
      }
    ]
    roleAssignments: [
      {
        principalId: userPrincipalId
        roleDefinitionId: cognitiveServicesOpenAIContributor.id
        principalType: 'User'
      }
    ]
  }
}

module secondaryAIServices './ai_ml/ai-services.bicep' = {
  name: '${abbrs.ai.aiServices}${secondaryAIServicesResourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.ai.aiServices}${secondaryAIServicesResourceToken}'
    location: secondaryAIServicesLocation
    tags: union(tags, {})
    deployments: [
      {
        name: gpt4OmniModelDeploymentName
        model: {
          format: 'OpenAI'
          name: 'gpt-4o'
          version: '2024-05-13'
        }
        sku: {
          name: 'Standard'
          capacity: 20
        }
      }
    ]
    roleAssignments: [
      {
        principalId: userPrincipalId
        roleDefinitionId: cognitiveServicesOpenAIContributor.id
        principalType: 'User'
      }
    ]
  }
}

module aiHub './ai_ml/ai-hub.bicep' = {
  name: '${abbrs.ai.aiHub}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.ai.aiHub}${resourceToken}'
    location: location
    tags: union(tags, {})
    storageAccountId: storageAccount.outputs.id
    keyVaultId: keyVault.outputs.id
    applicationInsightsId: applicationInsights.outputs.id
    containerRegistryId: containerRegistry.outputs.id
    aiServicesName: primaryAIServices.outputs.name
  }
}

module aiHubProject './ai_ml/ai-hub-project.bicep' = {
  name: '${abbrs.ai.aiHubProject}${workloadName}'
  scope: resourceGroup
  params: {
    name: '${abbrs.ai.aiHubProject}${workloadName}'
    location: location
    tags: union(tags, {})
    aiHubName: aiHub.outputs.name
  }
}

output subscriptionInfo object = {
  id: subscription().subscriptionId
  tenantId: subscription().tenantId
}

output resourceGroupInfo object = {
  id: resourceGroup.id
  name: resourceGroup.name
  location: resourceGroup.location
  workloadName: workloadName
}

output storageAccountInfo object = {
  id: storageAccount.outputs.id
  name: storageAccount.outputs.name
}

output keyVaultInfo object = {
  id: keyVault.outputs.id
  name: keyVault.outputs.name
  uri: keyVault.outputs.uri
}

output logAnalyticsWorkspaceInfo object = {
  id: logAnalyticsWorkspace.outputs.id
  name: logAnalyticsWorkspace.outputs.name
  customerId: logAnalyticsWorkspace.outputs.customerId
}

output applicationInsightsInfo object = {
  id: applicationInsights.outputs.id
  name: applicationInsights.outputs.name
}

output containerRegistryInfo object = {
  id: containerRegistry.outputs.id
  name: containerRegistry.outputs.name
  loginServer: containerRegistry.outputs.loginServer
}

output primaryAIServicesInfo object = {
  id: primaryAIServices.outputs.id
  name: primaryAIServices.outputs.name
  endpoint: primaryAIServices.outputs.endpoint
  host: primaryAIServices.outputs.host
  gpt4ModelDeploymentName: gpt4ModelDeploymentName
  gpt35ModelDeploymentName: gpt35ModelDeploymentName
}

output secondaryAIServicesInfo object = {
  id: secondaryAIServices.outputs.id
  name: secondaryAIServices.outputs.name
  endpoint: secondaryAIServices.outputs.endpoint
  host: secondaryAIServices.outputs.host
  gpt4OmniModelDeploymentName: gpt4OmniModelDeploymentName
}

output documentIntelligenceInfo object = {
  id: documentIntelligence.outputs.id
  name: documentIntelligence.outputs.name
  endpoint: documentIntelligence.outputs.endpoint
  host: documentIntelligence.outputs.host
  identityPrincipalId: documentIntelligence.outputs.identityPrincipalId
}

output aiHubInfo object = {
  id: aiHub.outputs.id
  name: aiHub.outputs.name
  identityPrincipalId: aiHub.outputs.identityPrincipalId
}
