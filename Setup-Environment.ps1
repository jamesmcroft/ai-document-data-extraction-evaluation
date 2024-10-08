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

if (-not $InfrastructureOutputs) {
    Write-Error "Infrastructure deployment outputs are not available. Exiting..."
    exit 1
}

$KeyVaultName = $InfrastructureOutputs.keyVaultInfo.value.name

## Update the Key Vault firewall rules with the current IP address
$CurrentIpAddress = (Invoke-RestMethod -Uri 'http://ipinfo.io/json' | Select-Object -ExpandProperty ip)
Write-Host "Updating Key Vault firewall rules with current IP address: $CurrentIpAddress..."
az keyvault network-rule add --name $KeyVaultName --ip-address $CurrentIpAddress

$DocumentIntelligenceEndpoint = $InfrastructureOutputs.documentIntelligenceInfo.value.endpoint
$Gpt35ModelEndpoint = $InfrastructureOutputs.aiModelsInfo.value.gpt35Turbo.endpoint
$Gpt35ModelDeploymentName = $InfrastructureOutputs.aiModelsInfo.value.gpt35Turbo.deploymentName
$Gpt4ModelEndpoint = $InfrastructureOutputs.aiModelsInfo.value.gpt4Turbo.endpoint
$Gpt4ModelDeploymentName = $InfrastructureOutputs.aiModelsInfo.value.gpt4Turbo.deploymentName
$Gpt4OmniModelEndpoint = $InfrastructureOutputs.aiModelsInfo.value.gpt4Omni.endpoint
$Gpt4OmniModelDeploymentName = $InfrastructureOutputs.aiModelsInfo.value.gpt4Omni.deploymentName
$Gpt4OmniMiniModelEndpoint = $InfrastructureOutputs.aiModelsInfo.value.gpt4OmniMini.endpoint
$Gpt4OmniMiniModelDeploymentName = $InfrastructureOutputs.aiModelsInfo.value.gpt4OmniMini.deploymentName

Write-Host "Updating test/EvaluationTests/appsettings.Test.json settings..."

$ConfigurationFile = './test/EvaluationTests/appsettings.Test.json'
$Configuration = Get-Content -Path $ConfigurationFile -Raw | ConvertFrom-Json
$Configuration.DocumentIntelligence.Endpoint = $DocumentIntelligenceEndpoint
$Configuration.GPT35Turbo.Endpoint = $Gpt35ModelEndpoint
$Configuration.GPT35Turbo.DeploymentName = $Gpt35ModelDeploymentName
$Configuration.GPT4Turbo.Endpoint = $Gpt4ModelEndpoint
$Configuration.GPT4Turbo.DeploymentName = $Gpt4ModelDeploymentName
$Configuration.GPT4Omni.Endpoint = $Gpt4OmniModelEndpoint
$Configuration.GPT4Omni.DeploymentName = $Gpt4OmniModelDeploymentName
$Configuration.GPT4OmniMini.Endpoint = $Gpt4OmniMiniModelEndpoint
$Configuration.GPT4OmniMini.DeploymentName = $Gpt4OmniMiniModelDeploymentName

if ($InfrastructureOutputs.aiModelsInfo.value.phi3Mini) {
    $Phi3MiniModelEndpoint = $InfrastructureOutputs.aiModelsInfo.value.phi3Mini.endpoint
    $Phi3MiniModelDeploymentPrimaryKeySecretName = $InfrastructureOutputs.aiModelsInfo.value.phi3Mini.primaryKeySecretName
    $Phi3MiniModelDeploymentPrimaryKey = (az keyvault secret show --vault-name $KeyVaultName --name $Phi3MiniModelDeploymentPrimaryKeySecretName --query value -o tsv)

    $Configuration.Phi3Mini128kInstruct.Endpoint = $Phi3MiniModelEndpoint
    $Configuration.Phi3Mini128kInstruct.ApiKey = $Phi3MiniModelDeploymentPrimaryKey
}

if ($InfrastructureOutputs.aiModelsInfo.value.phi35Mini) {
    $Phi35MiniModelEndpoint = $InfrastructureOutputs.aiModelsInfo.value.phi35Mini.endpoint
    $Phi35MiniModelDeploymentPrimaryKeySecretName = $InfrastructureOutputs.aiModelsInfo.value.phi35Mini.primaryKeySecretName
    $Phi35MiniModelDeploymentPrimaryKey = (az keyvault secret show --vault-name $KeyVaultName --name $Phi35MiniModelDeploymentPrimaryKeySecretName --query value -o tsv)

    $Configuration.Phi35MiniInstruct.Endpoint = $Phi35MiniModelEndpoint
    $Configuration.Phi35MiniInstruct.ApiKey = $Phi35MiniModelDeploymentPrimaryKey
}

$Configuration | ConvertTo-Json -Depth 100 | Set-Content -Path $ConfigurationFile

Pop-Location

return $InfrastructureOutputs
