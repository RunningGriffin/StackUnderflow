#!/usr/bin/env bash
# One-time setup: creates the Azure App Service for ClassMate, points it at Neon,
# and wires GitHub Actions to deploy to it via OIDC (no stored passwords).
#
# Prerequisites: `az login` and `gh auth login` done, admin access to the GitHub repo.
# Usage:  bash scripts/azure-setup.sh
# Override any default with env vars, e.g.  APP_NAME=classmate-gw LOCATION=westeurope bash scripts/azure-setup.sh
set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:-classmate-rg}"
LOCATION="${LOCATION:-eastus}"
PLAN_NAME="${PLAN_NAME:-classmate-plan}"
PLAN_SKU="${PLAN_SKU:-F1}"            # F1 = free. Use B1 (~US$13/mo) for always-on and no daily CPU quota.
APP_NAME="${APP_NAME:-classmate-$(whoami | tr -cd 'a-z0-9' | head -c 10)}"  # must be globally unique
IDENTITY_NAME="${IDENTITY_NAME:-classmate-github-deployer}"
GITHUB_REPO="${GITHUB_REPO:-$(gh repo view --json nameWithOwner -q .nameWithOwner)}"

if [[ -z "${NEON_CONNECTION_STRING:-}" ]]; then
  echo "Paste your Neon connection string in .NET format"
  echo "(Host=...;Database=...;Username=...;Password=...;SSL Mode=Require) - input is hidden:"
  read -rs NEON_CONNECTION_STRING
  echo
fi
if [[ "$NEON_CONNECTION_STRING" == postgres* ]]; then
  echo "That looks like a postgres:// URL. Npgsql needs the key=value (.NET) format - pick '.NET' in Neon's Connect dialog." >&2
  exit 1
fi

SUBSCRIPTION_ID="$(az account show --query id -o tsv)"
TENANT_ID="$(az account show --query tenantId -o tsv)"

echo "==> Resource group $RESOURCE_GROUP ($LOCATION)"
az group create -n "$RESOURCE_GROUP" -l "$LOCATION" -o none

echo "==> App Service plan $PLAN_NAME ($PLAN_SKU, Linux)"
az appservice plan create -g "$RESOURCE_GROUP" -n "$PLAN_NAME" --is-linux --sku "$PLAN_SKU" -o none

echo "==> Web app $APP_NAME (.NET 10)"
az webapp create -g "$RESOURCE_GROUP" -p "$PLAN_NAME" -n "$APP_NAME" --runtime "DOTNETCORE:10.0" -o none

echo "==> Config: WebSockets (needed by Blazor Server), HTTPS only, Neon connection string"
az webapp config set -g "$RESOURCE_GROUP" -n "$APP_NAME" --web-sockets-enabled true -o none
az webapp update -g "$RESOURCE_GROUP" -n "$APP_NAME" --https-only true -o none
az webapp config appsettings set -g "$RESOURCE_GROUP" -n "$APP_NAME" -o none --settings \
  "ConnectionStrings__DefaultConnection=$NEON_CONNECTION_STRING" \
  "ASPNETCORE_ENVIRONMENT=Production"

echo "==> Managed identity for GitHub Actions"
az identity create -g "$RESOURCE_GROUP" -n "$IDENTITY_NAME" -o none
CLIENT_ID="$(az identity show -g "$RESOURCE_GROUP" -n "$IDENTITY_NAME" --query clientId -o tsv)"
PRINCIPAL_ID="$(az identity show -g "$RESOURCE_GROUP" -n "$IDENTITY_NAME" --query principalId -o tsv)"

az identity federated-credential create -g "$RESOURCE_GROUP" --identity-name "$IDENTITY_NAME" \
  -n github-production \
  --issuer "https://token.actions.githubusercontent.com" \
  --subject "repo:${GITHUB_REPO}:environment:production" \
  --audiences "api://AzureADTokenExchange" -o none

WEBAPP_ID="$(az webapp show -g "$RESOURCE_GROUP" -n "$APP_NAME" --query id -o tsv)"
az role assignment create --assignee-object-id "$PRINCIPAL_ID" --assignee-principal-type ServicePrincipal \
  --role "Website Contributor" --scope "$WEBAPP_ID" -o none

echo "==> GitHub secrets/variables on $GITHUB_REPO"
gh api -X PUT "repos/${GITHUB_REPO}/environments/production" >/dev/null
gh secret set AZURE_CLIENT_ID --repo "$GITHUB_REPO" --body "$CLIENT_ID"
gh secret set AZURE_TENANT_ID --repo "$GITHUB_REPO" --body "$TENANT_ID"
gh secret set AZURE_SUBSCRIPTION_ID --repo "$GITHUB_REPO" --body "$SUBSCRIPTION_ID"
gh variable set AZURE_WEBAPP_NAME --repo "$GITHUB_REPO" --body "$APP_NAME"

echo
echo "Done. Your app will live at: https://${APP_NAME}.azurewebsites.net"
echo "Deploy: push to main, or run:  gh workflow run deploy-azure.yml --repo $GITHUB_REPO"
