# Deploying InventoryHub to Azure

Two supported targets, both using the same Docker image:

- **Option A — Azure Container Apps** (simplest, serverless containers)
- **Option B — Azure Kubernetes Service, AKS** (full Kubernetes, uses the `k8s/` manifests)

The app reads its database settings from environment variables, so no code
change is needed for the cloud: set `DatabaseProvider=Npgsql` and the
`ConnectionStrings__PostgresConnection` variable to point at an
**Azure Database for PostgreSQL – Flexible Server** instance.

---

## Shared first steps: resource group, registry, image

```bash
# Variables
RG=inventoryhub-rg
LOCATION=eastus
ACR=inventoryhubacr$RANDOM        # must be globally unique, letters/numbers only
IMAGE=inventoryhub:1.0.0

# 1. Resource group
az group create --name $RG --location $LOCATION

# 2. Azure Container Registry
az acr create --resource-group $RG --name $ACR --sku Basic

# 3. Build the image IN ACR (no local Docker needed) and push it
az acr build --registry $ACR --image $IMAGE .

# Full image name used below:
FULL_IMAGE=$ACR.azurecr.io/$IMAGE
```

## Option A — Azure Container Apps

```bash
# 4. Container Apps environment
az containerapp env create \
  --name inventoryhub-env \
  --resource-group $RG \
  --location $LOCATION

# 5. Give the app access to pull from ACR, then create the app
az containerapp create \
  --name inventoryhub \
  --resource-group $RG \
  --environment inventoryhub-env \
  --image $FULL_IMAGE \
  --target-port 8080 \
  --ingress external \
  --registry-server $ACR.azurecr.io \
  --env-vars \
      DatabaseProvider=Npgsql \
      "ConnectionStrings__PostgresConnection=Host=<pg-host>.postgres.database.azure.com;Port=5432;Database=inventoryhub;Username=<user>;Password=<password>;SSL Mode=Require"

# 6. Print the public URL
az containerapp show --name inventoryhub --resource-group $RG \
  --query properties.configuration.ingress.fqdn -o tsv
```

Updating later: `az containerapp update --name inventoryhub --resource-group $RG --image <new-image>`
— this is exactly what the GitHub Actions workflow in
`.github/workflows/ci-cd.yml` automates.

## Option B — AKS

```bash
# 4. AKS cluster with ACR pull access
az aks create \
  --resource-group $RG \
  --name inventoryhub-aks \
  --node-count 2 \
  --attach-acr $ACR \
  --generate-ssh-keys

# 5. Connect kubectl
az aks get-credentials --resource-group $RG --name inventoryhub-aks

# 6. Point the deployment at your ACR image and apply the manifests
sed -i "s|image: inventoryhub:latest|image: $FULL_IMAGE|" k8s/deployment.yaml
kubectl create secret generic inventoryhub-secret \
  --from-literal=PostgresConnection="Host=<pg-host>;Port=5432;Database=inventoryhub;Username=<user>;Password=<password>;SSL Mode=Require"
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/ingress.yaml

# 7. Watch it come up
kubectl get pods,svc,ingress
```

An NGINX ingress controller is required for `k8s/ingress.yaml` on a fresh
cluster; on AKS it can be added with the web app routing add-on:
`az aks enable-addons --addons web_application_routing ...` or the Helm chart.

---

## GitHub Actions

`.github/workflows/ci-cd.yml` builds and tests on every push, and on `main`
it pushes the image to ACR and updates the Container App. Create the
service principal it logs in with:

```bash
az ad sp create-for-rbac --name inventoryhub-gha --role contributor \
  --scopes /subscriptions/<subscription-id>/resourceGroups/$RG --sdk-auth
```

Save the JSON output as the GitHub secret `AZURE_CREDENTIALS`, plus
`ACR_LOGIN_SERVER`, `AZURE_RESOURCE_GROUP` and `AZURE_CONTAINERAPP_NAME`.
