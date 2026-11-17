#!/usr/bin/env bash
# One-time setup: creates the Azure App Service for ClassMate, points it at Neon,
# and wires GitHub Actions to deploy to it via OIDC (no stored passwords).
#
# Prerequisites: `az login` and `gh auth login` done, admin access to the GitHub repo.
# Usage:  bash scripts/azure-setup.sh
# Override any default with env vars, e.g.  APP_NAME=classmate-gw LOCATION=westeurope bash scripts/azure-setup.sh
set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:-classmate-rg}"
LOCATION="${LOCATION:-northcentralus}"  # Azure for Students only allows a few regions; this one is closest to Neon us-east-2
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
# Strip surrounding whitespace/quotes (common when copy-pasting).
NEON_CONNECTION_STRING="$(printf '%s' "$NEON_CONNECTION_STRING" | sed -E "s/^[[:space:]'\"]+//; s/[[:space:]'\"]+\$//")"
if [[ "$NEON_CONNECTION_STRING" == postgres* ]]; then
  # Npgsql needs key=value format, so convert a postgres:// URL automatically.
  NEON_CONNECTION_STRING="$(NEON_URL="$NEON_CONNECTION_STRING" python3 -I -c '
import os
from urllib.parse import urlparse, unquote, parse_qs
u = urlparse(os.environ["NEON_URL"].strip().strip("\x27\""))
ssl = {"verify-full": "VerifyFull", "verify-ca": "VerifyCA"}.get(parse_qs(u.query).get("sslmode", ["require"])[0], "Require")
db = unquote(u.path.lstrip("/"))
print(f"Host={u.hostname};Port={u.port or 5432};Database={db};"
      f"Username={unquote(u.username)};Password={unquote(u.password)};SSL Mode={ssl}")
')"
  echo "Converted postgres:// URL to .NET format."
fi
if [[ "$NEON_CONNECTION_STRING" == *"-pooler."* ]]; then
  echo "Note: this is a pooled (-pooler) connection. It works, but the direct one is safer for migrations." >&2
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

# GitHub may present the subject with immutable owner/repo IDs (owner@id/repo@id), so trust that form too.
OWNER_ID="$(gh api "repos/${GITHUB_REPO}" --jq .owner.id)"
REPO_ID="$(gh api "repos/${GITHUB_REPO}" --jq .id)"
az identity federated-credential create -g "$RESOURCE_GROUP" --identity-name "$IDENTITY_NAME" \
  -n github-production-immutable \
  --issuer "https://token.actions.githubusercontent.com" \
  --subject "repo:${GITHUB_REPO%%/*}@${OWNER_ID}/${GITHUB_REPO##*/}@${REPO_ID}:environment:production" \
  --audiences "api://AzureADTokenExchange" -o none

WEBAPP_ID="$(az webapp show -g "$RESOURCE_GROUP" -n "$APP_NAME" --query id -o tsv)"
az role assignment create --assignee-object-id "$PRINCIPAL_ID" --assignee-principal-type ServicePrincipal \
  --role "Website Contributor" --scope "$WEBAPP_ID" -o none

if [[ "$(gh api "repos/${GITHUB_REPO}" --jq .permissions.admin 2>/dev/null)" == "true" ]]; then
  echo "==> GitHub secrets/variables on $GITHUB_REPO"
  gh api -X PUT "repos/${GITHUB_REPO}/environments/production" >/dev/null
  gh secret set AZURE_CLIENT_ID --repo "$GITHUB_REPO" --body "$CLIENT_ID"
  gh secret set AZURE_TENANT_ID --repo "$GITHUB_REPO" --body "$TENANT_ID"
  gh secret set AZURE_SUBSCRIPTION_ID --repo "$GITHUB_REPO" --body "$SUBSCRIPTION_ID"
  gh variable set AZURE_WEBAPP_NAME --repo "$GITHUB_REPO" --body "$APP_NAME"
else
  cat <<EOF

==> You are not an admin of $GITHUB_REPO, so GitHub was NOT configured.
    Send the block below to the repo owner and ask them to run it (these are IDs, not passwords):

gh secret set AZURE_CLIENT_ID --repo $GITHUB_REPO --body "$CLIENT_ID"
gh secret set AZURE_TENANT_ID --repo $GITHUB_REPO --body "$TENANT_ID"
gh secret set AZURE_SUBSCRIPTION_ID --repo $GITHUB_REPO --body "$SUBSCRIPTION_ID"
gh variable set AZURE_WEBAPP_NAME --repo $GITHUB_REPO --body "$APP_NAME"

    (Or in the browser: repo Settings -> Secrets and variables -> Actions:
     3 "Repository secrets" + 1 "Variable" with the same names and values.)
EOF
fi

echo
echo "Done. Your app will live at: https://${APP_NAME}.azurewebsites.net"
echo "Deploy: push to main, or run:  gh workflow run deploy-azure.yml --repo $GITHUB_REPO"
