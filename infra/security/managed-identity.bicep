@description('Name of the resource.')
param name string
@description('Location to deploy the resource. Defaults to the location of the resource group.')
param location string = resourceGroup().location
@description('Tags for the resource.')
param tags object = {}

@export()
@description('Role assignment information for an identity.')
type roleAssignmentInfo = {
  @description('Role definition ID for the RBAC role to assign to the identity.')
  roleDefinitionId: string
  @description('Principal ID of the identity to assign to.')
  principalId: string
  @description('Type of the principal ID.')
  principalType: 'Device' | 'User' | 'Group' | 'ServicePrincipal' | 'ForeignGroup'
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' = {
  name: name
  location: location
  tags: tags
}

@description('The deployed Managed Identity resource.')
output resource resource = identity
@description('ID for the deployed Managed Identity resource.')
output id string = identity.id
@description('Name for the deployed Managed Identity resource.')
output name string = identity.name
@description('Principal ID for the deployed Managed Identity resource.')
output principalId string = identity.properties.principalId
@description('Client ID for the deployed Managed Identity resource.')
output clientId string = identity.properties.clientId
