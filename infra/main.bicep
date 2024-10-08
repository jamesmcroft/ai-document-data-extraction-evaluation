import { raiPolicyInfo } from './ai_ml/ai-services.bicep'

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

var abbrs = loadJsonContent('./abbreviations.json')
var roles = loadJsonContent('./roles.json')
var resourceToken = toLower(uniqueString(subscription().id, workloadName, location))

resource contributor 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  scope: resourceGroup
  name: roles.general.contributor
}

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.managementGovernance.resourceGroup}${workloadName}'
  location: location
  tags: union(tags, {})
}

module managedIdentity './security/managed-identity.bicep' = {
  name: '${abbrs.security.managedIdentity}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.security.managedIdentity}${resourceToken}'
    location: location
    tags: union(tags, {})
  }
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
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: contributor.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

resource storageAccountContributor 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  scope: resourceGroup
  name: roles.storage.storageAccountContributor
}

resource storageBlobDataContributor 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  scope: resourceGroup
  name: roles.storage.storageBlobDataContributor
}

resource storageFileDataPrivilegedContributor 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  scope: resourceGroup
  name: roles.storage.storageFileDataPrivilegedContributor
}

resource storageTableDataContributor 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  scope: resourceGroup
  name: roles.storage.storageTableDataContributor
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
        roleDefinitionId: storageAccountContributor.id
        principalType: 'User'
      }
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: storageAccountContributor.id
        principalType: 'ServicePrincipal'
      }
      {
        principalId: userPrincipalId
        roleDefinitionId: storageBlobDataContributor.id
        principalType: 'User'
      }
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: storageBlobDataContributor.id
        principalType: 'ServicePrincipal'
      }
      {
        principalId: userPrincipalId
        roleDefinitionId: storageFileDataPrivilegedContributor.id
        principalType: 'User'
      }
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: storageFileDataPrivilegedContributor.id
        principalType: 'ServicePrincipal'
      }
      {
        principalId: userPrincipalId
        roleDefinitionId: storageTableDataContributor.id
        principalType: 'User'
      }
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: storageTableDataContributor.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

resource keyVaultAdministrator 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
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
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: keyVaultAdministrator.id
        principalType: 'ServicePrincipal'
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
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: acrPush.id
        principalType: 'ServicePrincipal'
      }
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: acrPull.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

resource cognitiveServicesUser 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.ai.cognitiveServicesUser
}

@description('Location for Document Intelligence preview features (requires 2024-02-29-preview).')
var documentIntelligenceLocation = 'westeurope'
var documentIntelligenceResourceToken = toLower(uniqueString(
  subscription().id,
  workloadName,
  documentIntelligenceLocation
))

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
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: cognitiveServicesUser.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

resource cognitiveServicesContributor 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  scope: resourceGroup
  name: roles.ai.cognitiveServicesContributor
}

resource cognitiveServicesOpenAIContributor 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  scope: resourceGroup
  name: roles.ai.cognitiveServicesOpenAIContributor
}

var gpt4OmniMiniModelDeploymentName = 'gpt-4o-mini'
var gpt4OmniModelDeploymentName = 'gpt-4o'
var gpt4ModelDeploymentName = 'gpt-4'
var gpt35ModelDeploymentName = 'gpt-35-turbo'

module aiServices './ai_ml/ai-services.bicep' = {
  name: '${abbrs.ai.aiServices}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.ai.aiServices}${resourceToken}'
    location: location
    tags: union(tags, {})
    identityId: managedIdentity.outputs.id
    raiPolicies: [
      {
        name: workloadName
        mode: 'Blocking'
        prompt: {}
        completion: {}
      }
    ]
    deployments: [
      {
        name: gpt4OmniMiniModelDeploymentName
        model: {
          format: 'OpenAI'
          name: 'gpt-4o-mini'
          version: '2024-07-18'
        }
        sku: {
          name: 'GlobalStandard'
          capacity: 10
        }
        raiPolicyName: workloadName
        versionUpgradeOption: 'OnceCurrentVersionExpired'
      }
      {
        name: gpt4OmniModelDeploymentName
        model: {
          format: 'OpenAI'
          name: 'gpt-4o'
          version: '2024-05-13'
        }
        sku: {
          name: 'GlobalStandard'
          capacity: 10
        }
        raiPolicyName: workloadName
        versionUpgradeOption: 'OnceCurrentVersionExpired'
      }
      {
        name: gpt4ModelDeploymentName
        model: {
          format: 'OpenAI'
          name: 'gpt-4'
          version: 'turbo-2024-04-09'
        }
        sku: {
          name: 'GlobalStandard'
          capacity: 8
        }
        raiPolicyName: workloadName
        versionUpgradeOption: 'OnceCurrentVersionExpired'
      }
      {
        name: gpt35ModelDeploymentName
        model: {
          format: 'OpenAI'
          name: 'gpt-35-turbo'
          version: '0613'
        }
        sku: {
          name: 'Standard'
          capacity: 10
        }
        raiPolicyName: workloadName
        versionUpgradeOption: 'OnceCurrentVersionExpired'
      }
    ]
    roleAssignments: [
      {
        principalId: userPrincipalId
        roleDefinitionId: cognitiveServicesContributor.id
        principalType: 'User'
      }
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: cognitiveServicesContributor.id
        principalType: 'ServicePrincipal'
      }
      {
        principalId: userPrincipalId
        roleDefinitionId: cognitiveServicesOpenAIContributor.id
        principalType: 'User'
      }
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: cognitiveServicesOpenAIContributor.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

resource azureMLDataScientist 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  scope: resourceGroup
  name: roles.ai.azureMLDataScientist
}

module aiHub './ai_ml/ai-hub.bicep' = {
  name: '${abbrs.ai.aiHub}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.ai.aiHub}${resourceToken}'
    friendlyName: 'Hub - Document Data Extraction Evaluation'
    descriptionInfo: 'Generated by the Document Data Extraction Evaluation sample project'
    location: location
    tags: union(tags, {})
    identityId: managedIdentity.outputs.id
    storageAccountId: storageAccount.outputs.id
    keyVaultId: keyVault.outputs.id
    applicationInsightsId: applicationInsights.outputs.id
    containerRegistryId: containerRegistry.outputs.id
    aiServicesName: aiServices.outputs.name
    roleAssignments: [
      {
        principalId: userPrincipalId
        roleDefinitionId: azureMLDataScientist.id
        principalType: 'User'
      }
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: azureMLDataScientist.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

var phi3MiniModelDeploymentName = 'phi-3-mini-128k-${resourceToken}'
var phi35MiniModelDeploymentName = 'phi-35-mini-${resourceToken}'

module aiHubProject './ai_ml/ai-hub-project.bicep' = {
  name: '${abbrs.ai.aiHubProject}${workloadName}'
  scope: resourceGroup
  params: {
    name: '${abbrs.ai.aiHubProject}${workloadName}'
    friendlyName: 'Project - Document Data Extraction Evaluation'
    descriptionInfo: 'Generated by the Document Data Extraction Evaluation sample project'
    location: location
    tags: union(tags, {})
    identityId: managedIdentity.outputs.id
    aiHubName: aiHub.outputs.name
    serverlessModels: [
      // {
      //   name: phi3MiniModelDeploymentName
      //   model: {
      //     name: 'Phi-3-mini-128k-instruct'
      //   }
      //   keyVaultConfig: {
      //     name: keyVault.outputs.name
      //     primaryKeySecretName: 'Phi-3-mini-128k-instruct-PrimaryKey'
      //     secondaryKeySecretName: 'Phi-3-mini-128k-instruct-SecondaryKey'
      //   }
      // }
      {
        name: phi35MiniModelDeploymentName
        model: {
          name: 'Phi-35-mini-instruct'
        }
        keyVaultConfig: {
          name: keyVault.outputs.name
          primaryKeySecretName: 'Phi-35-mini-instruct-PrimaryKey'
          secondaryKeySecretName: 'Phi-35-mini-instruct-SecondaryKey'
        }
      }
    ]
    roleAssignments: [
      {
        principalId: userPrincipalId
        roleDefinitionId: azureMLDataScientist.id
        principalType: 'User'
      }
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: azureMLDataScientist.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

output subscriptionInfo object = {
  id: subscription().subscriptionId
  tenantId: subscription().tenantId
}

output resourceGroupInfo object = {
  name: resourceGroup.name
  location: resourceGroup.location
  workloadName: workloadName
}

output storageAccountInfo object = {
  name: storageAccount.outputs.name
  location: location
}

output keyVaultInfo object = {
  name: keyVault.outputs.name
  location: location
}

output documentIntelligenceInfo object = {
  endpoint: documentIntelligence.outputs.endpoint
  location: documentIntelligenceLocation
}

output aiModelsInfo object = {
  gpt35Turbo: {
    endpoint: aiServices.outputs.openAIEndpoint
    deploymentName: gpt35ModelDeploymentName
  }
  gpt4Turbo: {
    endpoint: aiServices.outputs.openAIEndpoint
    deploymentName: gpt4ModelDeploymentName
  }
  gpt4Omni: {
    endpoint: aiServices.outputs.openAIEndpoint
    deploymentName: gpt4OmniModelDeploymentName
  }
  gpt4OmniMini: {
    endpoint: aiServices.outputs.openAIEndpoint
    deploymentName: gpt4OmniMiniModelDeploymentName
  }
  // phi3Mini: {
  //   endpoint: aiHubProject.outputs.serverlessModelDeployments[0].endpoint
  //   primaryKeySecretName: aiHubProject.outputs.serverlessModelDeployments[0].primaryKeySecretName
  //   secondaryKeySecretName: aiHubProject.outputs.serverlessModelDeployments[0].secondaryKeySecretName
  // }
  phi35Mini: {
    endpoint: aiHubProject.outputs.serverlessModelDeployments[0].endpoint
    primaryKeySecretName: aiHubProject.outputs.serverlessModelDeployments[0].primaryKeySecretName
    secondaryKeySecretName: aiHubProject.outputs.serverlessModelDeployments[0].secondaryKeySecretName
  }
}
