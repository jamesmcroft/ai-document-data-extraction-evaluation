<#
.SYNOPSIS
    Deploys the infrastructure and applications required to run the solution.
.PARAMETER DeploymentName
	The name of the deployment.
.PARAMETER Location
    The location of the deployment.
.PARAMETER SkipInfrastructure
    Whether to skip the infrastructure deployment. Requires InfrastructureOutputs.json to exist in the infra directory.
.EXAMPLE
    .\Setup-Environment.ps1 -DeploymentName 'my-deployment' -Location 'swedencentral' -SkipInfrastructure $false
.NOTES
    Author: James Croft
#>

param
(
    [Parameter(Mandatory = $true)]
    [string]$DeploymentName,
    [Parameter(Mandatory = $true)]
    [string]$Location,
    [Parameter(Mandatory = $true)]
    [string]$SkipInfrastructure
)

function Set-ConfigurationFileVariable($configurationFile, $variableName, $variableValue) {
    if (-not (Test-Path $configurationFile)) {
        New-Item -Path $configurationFile -ItemType file
    }

    if (Select-String -Path $configurationFile -Pattern $variableName) {
        (Get-Content $configurationFile) | Foreach-Object {
            $_ -replace "$variableName = .*", "$variableName = $variableValue"
        } | Set-Content $configurationFile
    }
    else {
        Add-Content -Path $configurationFile -value "$variableName = $variableValue"
    }
}

Write-Host "Starting environment setup..."

if ($SkipInfrastructure -eq '$false' -or -not (Test-Path -Path './infra/InfrastructureOutputs.json')) {
    Write-Host "Deploying infrastructure..."
    $InfrastructureOutputs = (./infra/Deploy-Infrastructure.ps1 `
            -DeploymentName $DeploymentName `
            -Location $Location)
}
else {
    Write-Host "Skipping infrastructure deployment. Using existing outputs..."
    $InfrastructureOutputs = Get-Content -Path './infra/InfrastructureOutputs.json' -Raw | ConvertFrom-Json
}

$TenantId = $InfrastructureOutputs.subscriptionInfo.value.tenantId
$ResourceGroupName = $InfrastructureOutputs.resourceGroupInfo.value.name
$KeyVaultName = $InfrastructureOutputs.keyVaultInfo.value.name
$StorageAccountName = $InfrastructureOutputs.storageAccountInfo.value.name
$DocumentIntelligenceEndpoint = $InfrastructureOutputs.documentIntelligenceInfo.value.endpoint
$PrimaryAIServicesEndpoint = $InfrastructureOutputs.primaryAIServicesInfo.value.endpoint
$Gpt35ModelDeploymentName = $InfrastructureOutputs.primaryAIServicesInfo.value.gpt35ModelDeploymentName
$Gpt4ModelDeploymentName = $InfrastructureOutputs.primaryAIServicesInfo.value.gpt4ModelDeploymentName
$SecondaryAIServicesEndpoint = $InfrastructureOutputs.secondaryAIServicesInfo.value.endpoint
$Gpt4OmniModelDeploymentName = $InfrastructureOutputs.secondaryAIServicesInfo.value.gpt4OmniModelDeploymentName

Write-Host "Updating local settings..."

$ConfigurationFile = './config.env'

Set-ConfigurationFileVariable -configurationFile $ConfigurationFile -variableName 'AZURE_TENANT_ID' -variableValue $TenantId
Set-ConfigurationFileVariable -configurationFile $ConfigurationFile -variableName 'AZURE_RESOURCE_GROUP_NAME' -variableValue $ResourceGroupName
Set-ConfigurationFileVariable -configurationFile $ConfigurationFile -variableName 'AZURE_KEY_VAULT_NAME' -variableValue $KeyVaultName
Set-ConfigurationFileVariable -configurationFile $ConfigurationFile -variableName 'AZURE_STORAGE_ACCOUNT_NAME' -variableValue $StorageAccountName
Set-ConfigurationFileVariable -configurationFile $ConfigurationFile -variableName 'DOCUMENT_INTELLIGENCE_ENDPOINT' -variableValue $DocumentIntelligenceEndpoint
Set-ConfigurationFileVariable -configurationFile $ConfigurationFile -variableName 'PRIMARY_AI_SERVICES_ENDPOINT' -variableValue $PrimaryAIServicesEndpoint
Set-ConfigurationFileVariable -configurationFile $ConfigurationFile -variableName 'GPT35_MODEL_DEPLOYMENT_NAME' -variableValue $Gpt35ModelDeploymentName
Set-ConfigurationFileVariable -configurationFile $ConfigurationFile -variableName 'GPT4_MODEL_DEPLOYMENT_NAME' -variableValue $Gpt4ModelDeploymentName
Set-ConfigurationFileVariable -configurationFile $ConfigurationFile -variableName 'SECONDARY_AI_SERVICES_ENDPOINT' -variableValue $SecondaryAIServicesEndpoint
Set-ConfigurationFileVariable -configurationFile $ConfigurationFile -variableName 'GPT4_OMNI_MODEL_DEPLOYMENT_NAME' -variableValue $Gpt4OmniModelDeploymentName

Pop-Location

return $InfrastructureOutputs
